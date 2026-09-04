using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Agent.Harness;

namespace QS3D.Code.Cli
{
    public sealed class Qs3dCliApplication
    {
        private readonly Dictionary<string, HarnessExecutionSnapshot> _sessions =
            new Dictionary<string, HarnessExecutionSnapshot>(StringComparer.OrdinalIgnoreCase);
        private readonly ConsoleTraceRenderer _traceRenderer = new ConsoleTraceRenderer();

        public int Run(string[] args, TextWriter output, string workingDirectory)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (string.IsNullOrWhiteSpace(workingDirectory))
                throw new ArgumentException("Working directory is required.", nameof(workingDirectory));

            if (args.Length == 0)
            {
                WriteUsage(output);
                return 2;
            }

            try
            {
                var command = args[0];
                if (string.Equals(command, "route", StringComparison.OrdinalIgnoreCase))
                    return RunRoute(args, output, workingDirectory);
                if (string.Equals(command, "run", StringComparison.OrdinalIgnoreCase))
                    return RunDryRun(args, output, workingDirectory);
                if (string.Equals(command, "trace", StringComparison.OrdinalIgnoreCase))
                    return RunTrace(args, output);

                output.WriteLine("error: unsupported command");
                WriteUsage(output);
                return 2;
            }
            catch (InvalidDataException ex)
            {
                output.WriteLine("error: " + ex.Message);
                return 1;
            }
            catch (DirectoryNotFoundException ex)
            {
                output.WriteLine("error: " + ex.Message);
                return 1;
            }
            catch (InvalidOperationException ex)
            {
                output.WriteLine("error: " + ex.Message);
                return 1;
            }
        }

        private int RunRoute(string[] args, TextWriter output, string workingDirectory)
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                output.WriteLine("error: route requires exactly one prompt argument");
                return 2;
            }

            var root = RepositorySkillLoader.FindRepositoryRoot(workingDirectory);
            var snapshot = CreateSnapshotForPrompt(root, args[1]);
            RenderRoute(snapshot, output);
            return 0;
        }

        private int RunDryRun(string[] args, TextWriter output, string workingDirectory)
        {
            var root = RepositorySkillLoader.FindRepositoryRoot(workingDirectory);
            HarnessExecutionSnapshot snapshot;

            if (args.Length == 3
                && !string.IsNullOrWhiteSpace(args[1])
                && string.Equals(args[2], "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                snapshot = CreateSnapshotForPrompt(root, args[1]);
            }
            else if (args.Length == 4
                && string.Equals(args[1], "--skill", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(args[2])
                && string.Equals(args[3], "--dry-run", StringComparison.OrdinalIgnoreCase))
            {
                snapshot = CreateSnapshotForSkill(root, args[2]);
            }
            else
            {
                output.WriteLine("error: Child 2 supports only `run <prompt> --dry-run` or `run --skill <id> --dry-run`");
                return 2;
            }

            _sessions[snapshot.Trace[0].SessionId] = snapshot;
            output.WriteLine("session: " + snapshot.Trace[0].SessionId);
            RenderRoute(snapshot, output);
            _traceRenderer.Render(snapshot, output);
            return 0;
        }

        private int RunTrace(string[] args, TextWriter output)
        {
            if (args.Length != 2 || string.IsNullOrWhiteSpace(args[1]))
            {
                output.WriteLine("error: trace requires a session id");
                return 2;
            }

            if (!_sessions.TryGetValue(args[1], out var snapshot))
            {
                output.WriteLine("error: trace session is not available in this process; use the trace emitted by `run --dry-run`");
                return 2;
            }

            _traceRenderer.Render(snapshot, output);
            return 0;
        }

        private static HarnessExecutionSnapshot CreateSnapshotForPrompt(string root, string prompt)
        {
            var loader = new RepositorySkillLoader(root);
            var descriptors = loader.LoadForPrompt(prompt);
            return CreateEngine(descriptors).CreateInitialSnapshot(prompt, SessionId(prompt, descriptors));
        }

        private static HarnessExecutionSnapshot CreateSnapshotForSkill(string root, string id)
        {
            var loader = new RepositorySkillLoader(root);
            var descriptors = loader.LoadForSkill(id);
            var requested = descriptors.FirstOrDefault(skill => string.Equals(skill.Id, id, StringComparison.OrdinalIgnoreCase));
            if (requested == null)
                throw new InvalidDataException("Requested skill was not loaded: " + id + ".");

            var prompt = PromptForDomains(requested.Triggers);
            return CreateEngine(descriptors).CreateInitialSnapshot(prompt, SessionId("skill:" + id, descriptors));
        }

        private static HarnessEngine CreateEngine(IReadOnlyList<SkillDescriptor> descriptors)
        {
            var catalog = new SkillCatalog(descriptors);
            return new HarnessEngine(new TaskRouter(), new SkillRouter(catalog), new HarnessPolicy());
        }

        private static void RenderRoute(HarnessExecutionSnapshot snapshot, TextWriter output)
        {
            output.WriteLine("domains:");
            foreach (var domain in snapshot.Intent.Domains)
                output.WriteLine("- " + domain);

            output.WriteLine("skills:");
            foreach (var skill in snapshot.Skills)
                output.WriteLine("- " + skill.Id + "@" + skill.Version);

            output.WriteLine("docs:");
            foreach (var document in OrderedDistinct(snapshot.Skills.SelectMany(skill => skill.RequiredDocuments)))
                output.WriteLine("- " + document);

            output.WriteLine("tool-classes:");
            foreach (var toolClass in OrderedDistinct(snapshot.Skills.SelectMany(skill => skill.ToolClasses)))
                output.WriteLine("- " + toolClass);

            output.WriteLine("permissions:");
            var policy = new HarnessPolicy();
            foreach (var permission in RelevantPermissions(snapshot.Intent))
                output.WriteLine("- " + permission + "=" + policy.Resolve(permission).ToString().ToUpperInvariant());
        }

        private static IEnumerable<HarnessPermission> RelevantPermissions(TaskIntent intent)
        {
            yield return HarnessPermission.ReadRepository;
            yield return HarnessPermission.RunFocusedTests;

            if (intent.Domains.Contains(TaskDomain.Source))
                yield return HarnessPermission.EditTaskBranch;
            if (intent.Domains.Contains(TaskDomain.GithubCarrier))
            {
                yield return HarnessPermission.CreateUpdateCarrier;
                yield return HarnessPermission.MergeSameTaskPr;
            }
            if (intent.Domains.Contains(TaskDomain.CadInspect))
                yield return HarnessPermission.CadInspect;
            if (intent.Domains.Contains(TaskDomain.CadMutate))
                yield return HarnessPermission.CadMutate;
        }

        private static IEnumerable<string> OrderedDistinct(IEnumerable<string> values)
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (seen.Add(value))
                    yield return value;
            }
        }

        private static string PromptForDomains(IReadOnlyList<TaskDomain> domains)
        {
            var parts = new List<string>();
            foreach (var domain in domains)
            {
                switch (domain)
                {
                    case TaskDomain.Source: parts.Add("source code"); break;
                    case TaskDomain.ContinuousIntegration: parts.Add(" CI "); break;
                    case TaskDomain.GithubCarrier: parts.Add("github pull request"); break;
                    case TaskDomain.McpTransport: parts.Add("mcp transport"); break;
                    case TaskDomain.PersistenceDurability: parts.Add("save durability"); break;
                    case TaskDomain.BricsCadHost: parts.Add("bricscad"); break;
                    case TaskDomain.ReleasePackage: parts.Add("release package"); break;
                    case TaskDomain.CadInspect: parts.Add("cad inspect"); break;
                    case TaskDomain.CadMutate: parts.Add("cad mutate"); break;
                }
            }

            return string.Join(" ", parts);
        }

        private static string SessionId(string seed, IReadOnlyList<SkillDescriptor> descriptors)
        {
            var material = seed + "|" + string.Join(",", descriptors.Select(skill => skill.Id + "@" + skill.Version));
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(material));
            return "cli-" + Convert.ToHexString(hash.AsSpan(0, 8)).ToLowerInvariant();
        }

        private static void WriteUsage(TextWriter output)
        {
            output.WriteLine("usage:");
            output.WriteLine("  qs3d route \"<prompt>\"");
            output.WriteLine("  qs3d run \"<prompt>\" --dry-run");
            output.WriteLine("  qs3d run --skill <id> --dry-run");
            output.WriteLine("  qs3d trace <session-id>");
        }
    }
}
