using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using QS3D.Code.Cli;

namespace QS3D.Code.Cli.SmokeTests
{
    internal static class Program
    {
        private static int _passed;

        private static int Main()
        {
            try
            {
                Run("repository root discovery", RepositoryRootDiscovery);
                Run("progressive skill loading", ProgressiveSkillLoading);
                Run("manifest traversal rejected", ManifestTraversalRejected);
                Run("unknown manifest key rejected", UnknownManifestKeyRejected);
                Run("duplicate manifest id rejected", DuplicateManifestIdRejected);
                Run("dependency cycle rejected", DependencyCycleRejected);
                Run("deterministic route output", DeterministicRouteOutput);
                Run("dry-run performs no mutation", DryRunPerformsNoMutation);
                Run("environment dump option is unavailable", EnvironmentDumpUnavailable);
                Console.WriteLine($"PASS: {_passed} QS3D.Code.Cli smoke checks");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex);
                return 1;
            }
        }

        private static void Run(string name, Action test)
        {
            test();
            _passed++;
            Console.WriteLine("PASS: " + name);
        }

        private static void RepositoryRootDiscovery()
        {
            using var repo = new TempRepo();
            var nested = Path.Combine(repo.Root, "a", "b", "c");
            Directory.CreateDirectory(nested);
            var found = RepositorySkillLoader.FindRepositoryRoot(nested);
            Equal(Path.GetFullPath(repo.Root), found, "nested root discovery");
        }

        private static void ProgressiveSkillLoading()
        {
            using var repo = new TempRepo();
            repo.WriteCoreSkillSet();
            repo.WriteSkill("release-local-only", "id: release-local-only\nversion: 1\nunknown-key: must-not-be-read\n");

            var loader = new RepositorySkillLoader(repo.Root);
            var skills = loader.LoadForPrompt("fix MCP save durability and CI");
            var ids = skills.Select(skill => skill.Id).ToArray();
            SequenceEqual(
                new[] { "repository-lifecycle", "tdd-source", "ci-remediation", "mcp-transport", "persistence-durability" },
                ids,
                "progressive route ids");
        }

        private static void ManifestTraversalRejected()
        {
            using var repo = new TempRepo();
            repo.WriteSkill("repository-lifecycle", TempRepo.Manifest("repository-lifecycle", "Source", "", "AGENTS.md"));
            repo.WriteSkill("tdd-source", TempRepo.Manifest("tdd-source", "Source", "repository-lifecycle", "../outside.md"));
            Throws<InvalidDataException>(() => new RepositorySkillLoader(repo.Root).LoadForSkill("tdd-source"), "path traversal");
        }

        private static void UnknownManifestKeyRejected()
        {
            using var repo = new TempRepo();
            repo.WriteSkill("repository-lifecycle", "id: repository-lifecycle\nversion: 1\ntriggers:\n  - Source\nprerequisites: []\nrequired-docs: []\ntool-classes: []\nvalidation: []\nmystery: nope\n");
            Throws<InvalidDataException>(() => new RepositorySkillLoader(repo.Root).LoadForSkill("repository-lifecycle"), "unknown key");
        }

        private static void DuplicateManifestIdRejected()
        {
            using var repo = new TempRepo();
            repo.WriteSkill("repository-lifecycle", TempRepo.Manifest("duplicate", "Source", "", "AGENTS.md"));
            repo.WriteSkill("tdd-source", TempRepo.Manifest("duplicate", "Source", "", "CI_POLICY.md"));
            Throws<InvalidDataException>(() => new RepositorySkillLoader(repo.Root).LoadAllForValidation(), "duplicate id");
        }

        private static void DependencyCycleRejected()
        {
            using var repo = new TempRepo();
            repo.WriteSkill("repository-lifecycle", TempRepo.Manifest("repository-lifecycle", "Source", "tdd-source", "AGENTS.md"));
            repo.WriteSkill("tdd-source", TempRepo.Manifest("tdd-source", "Source", "repository-lifecycle", "CI_POLICY.md"));
            Throws<InvalidDataException>(() => new RepositorySkillLoader(repo.Root).LoadAllForValidation(), "dependency cycle");
        }

        private static void DeterministicRouteOutput()
        {
            using var repo = new TempRepo();
            repo.WriteCoreSkillSet();
            var app = new Qs3dCliApplication();
            var first = new StringWriter();
            var second = new StringWriter();
            Equal(0, app.Run(new[] { "route", "fix MCP save durability and CI" }, first, repo.Root), "first route exit");
            Equal(0, app.Run(new[] { "route", "fix MCP save durability and CI" }, second, repo.Root), "second route exit");
            Equal(first.ToString(), second.ToString(), "route output determinism");
            Contains(first.ToString(), "McpTransport", "route domain output");
            Contains(first.ToString(), "persistence-durability", "route skill output");
            Contains(first.ToString(), "docs:", "route canonical docs output");
            Contains(first.ToString(), "permissions:", "route permission classes output");
        }

        private static void DryRunPerformsNoMutation()
        {
            using var repo = new TempRepo();
            repo.WriteCoreSkillSet();
            var before = repo.FileSnapshot();
            var output = new StringWriter();
            var app = new Qs3dCliApplication();
            Equal(0, app.Run(new[] { "run", "fix MCP save durability and CI", "--dry-run" }, output, repo.Root), "dry-run exit");
            var after = repo.FileSnapshot();
            Equal(before, after, "dry-run repository snapshot");
            Contains(output.ToString(), "trace:", "dry-run trace output");
            Contains(output.ToString(), "session.ready", "dry-run ready trace");
        }

        private static void EnvironmentDumpUnavailable()
        {
            using var repo = new TempRepo();
            repo.WriteCoreSkillSet();
            const string secret = "QS3D_SMOKE_SECRET_VALUE_42";
            Environment.SetEnvironmentVariable("QS3D_TEST_SECRET", secret);
            try
            {
                var output = new StringWriter();
                var app = new Qs3dCliApplication();
                var code = app.Run(new[] { "--dump-env" }, output, repo.Root);
                True(code != 0, "dump-env must be rejected");
                True(output.ToString().IndexOf(secret, StringComparison.Ordinal) < 0, "environment secret must not be rendered");
            }
            finally
            {
                Environment.SetEnvironmentVariable("QS3D_TEST_SECRET", null);
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + $": expected '{expected}', got '{actual}'.");
        }

        private static void SequenceEqual(IEnumerable<string> expected, IEnumerable<string> actual, string label)
        {
            var left = expected.ToArray();
            var right = actual.ToArray();
            if (!left.SequenceEqual(right, StringComparer.Ordinal))
                throw new InvalidOperationException(label + $": expected [{string.Join(",", left)}], got [{string.Join(",", right)}].");
        }

        private static void Contains(string text, string token, string label)
        {
            if (text.IndexOf(token, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(label + $": missing '{token}'. Output: {text}");
        }

        private static void True(bool value, string label)
        {
            if (!value)
                throw new InvalidOperationException(label);
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException(label + $": expected {typeof(TException).Name}.");
        }

        private sealed class TempRepo : IDisposable
        {
            public TempRepo()
            {
                Root = Path.Combine(Path.GetTempPath(), "qs3d-cli-smoke-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(Root);
                Directory.CreateDirectory(Path.Combine(Root, ".git"));
                Directory.CreateDirectory(Path.Combine(Root, ".agent", "skills"));
            }

            public string Root { get; }

            public void WriteCoreSkillSet()
            {
                WriteSkill("repository-lifecycle", Manifest("repository-lifecycle", "Source,ContinuousIntegration,GithubCarrier,McpTransport,PersistenceDurability,BricsCadHost,ReleasePackage,CadInspect,CadMutate", "", "AGENTS.md,docs/AGENT-RESERVATION-V2.md,docs/AGENT-WORK-REGISTRATION.md"));
                WriteSkill("tdd-source", Manifest("tdd-source", "Source", "repository-lifecycle", ""));
                WriteSkill("ci-remediation", Manifest("ci-remediation", "ContinuousIntegration", "repository-lifecycle,tdd-source", "CI_POLICY.md"));
                WriteSkill("github-lifecycle", Manifest("github-lifecycle", "GithubCarrier", "repository-lifecycle", "docs/MAIN-WRITE-AUTHORIZATION.md"));
                WriteSkill("mcp-transport", Manifest("mcp-transport", "McpTransport", "repository-lifecycle,tdd-source", "docs/MCP-CANONICAL-RUNBOOK.md"));
                WriteSkill("persistence-durability", Manifest("persistence-durability", "PersistenceDurability", "mcp-transport", ""));
                WriteSkill("bricscad-host", Manifest("bricscad-host", "BricsCadHost", "repository-lifecycle", "docs/REMOTE-AGENT-SCOPE.md"));
                WriteSkill("cad-safety", Manifest("cad-safety", "CadInspect,CadMutate", "bricscad-host", ""));
                WriteSkill("release-local-only", Manifest("release-local-only", "ReleasePackage", "repository-lifecycle", "docs/REMOTE-AGENT-SCOPE.md"));
            }

            public void WriteSkill(string directoryId, string content)
            {
                var directory = Path.Combine(Root, ".agent", "skills", directoryId);
                Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory, "skill.yaml"), content, new UTF8Encoding(false));
            }

            public string FileSnapshot()
            {
                var builder = new StringBuilder();
                foreach (var path in Directory.GetFiles(Root, "*", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal))
                {
                    builder.Append(Path.GetRelativePath(Root, path).Replace('\\', '/'));
                    builder.Append('\n');
                    builder.Append(File.ReadAllText(path));
                    builder.Append("\n---\n");
                }
                return builder.ToString();
            }

            public static string Manifest(string id, string triggers, string prerequisites, string docs)
            {
                return "id: " + id + "\n"
                    + "version: 1\n"
                    + List("triggers", triggers)
                    + List("prerequisites", prerequisites)
                    + List("required-docs", docs)
                    + "tool-classes: []\n"
                    + "validation: []\n";
            }

            private static string List(string key, string csv)
            {
                var values = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0).ToArray();
                if (values.Length == 0)
                    return key + ": []\n";
                return key + ":\n" + string.Join("", values.Select(value => "  - " + value + "\n"));
            }

            public void Dispose()
            {
                try
                {
                    Directory.Delete(Root, true);
                }
                catch
                {
                }
            }
        }
    }
}
