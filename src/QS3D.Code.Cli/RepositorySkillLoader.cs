using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using QS3D.Core.Agent.Harness;

namespace QS3D.Code.Cli
{
    public sealed class RepositorySkillLoader
    {
        private const int MaxManifestBytes = 32 * 1024;
        private const int MaxListItems = 64;

        private static readonly string[] AllowedKeys =
        {
            "id",
            "version",
            "triggers",
            "prerequisites",
            "required-docs",
            "tool-classes",
            "validation"
        };

        private static readonly IReadOnlyDictionary<TaskDomain, string> RootSkills =
            new Dictionary<TaskDomain, string>
            {
                [TaskDomain.Source] = "tdd-source",
                [TaskDomain.ContinuousIntegration] = "ci-remediation",
                [TaskDomain.GithubCarrier] = "github-lifecycle",
                [TaskDomain.McpTransport] = "mcp-transport",
                [TaskDomain.PersistenceDurability] = "persistence-durability",
                [TaskDomain.BricsCadHost] = "bricscad-host",
                [TaskDomain.ReleasePackage] = "release-local-only",
                [TaskDomain.CadInspect] = "cad-safety",
                [TaskDomain.CadMutate] = "cad-safety"
            };

        private readonly string _repositoryRoot;
        private readonly string _skillRoot;

        public RepositorySkillLoader(string repositoryRoot)
        {
            if (string.IsNullOrWhiteSpace(repositoryRoot))
                throw new ArgumentException("Repository root is required.", nameof(repositoryRoot));

            _repositoryRoot = Path.GetFullPath(repositoryRoot);
            _skillRoot = Path.Combine(_repositoryRoot, ".agent", "skills");
            if (!Directory.Exists(_skillRoot))
                throw new DirectoryNotFoundException("Repository skill directory is missing: " + _skillRoot);
        }

        public string RepositoryRoot => _repositoryRoot;

        public static string FindRepositoryRoot(string startDirectory)
        {
            if (string.IsNullOrWhiteSpace(startDirectory))
                throw new ArgumentException("Start directory is required.", nameof(startDirectory));

            var current = new DirectoryInfo(Path.GetFullPath(startDirectory));
            if (!current.Exists)
                throw new DirectoryNotFoundException("Start directory does not exist: " + current.FullName);

            while (current != null)
            {
                var gitMarker = Path.Combine(current.FullName, ".git");
                var skillMarker = Path.Combine(current.FullName, ".agent", "skills");
                if ((Directory.Exists(gitMarker) || File.Exists(gitMarker)) && Directory.Exists(skillMarker))
                    return current.FullName;
                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate a QS3D repository root from the supplied working directory.");
        }

        public IReadOnlyList<SkillDescriptor> LoadForPrompt(string prompt)
        {
            var intent = new TaskRouter().Classify(prompt);
            var rootIds = new List<string>();
            foreach (var domain in Enum.GetValues<TaskDomain>())
            {
                if (!intent.Domains.Contains(domain))
                    continue;
                if (!RootSkills.TryGetValue(domain, out var id))
                    continue;
                if (!rootIds.Contains(id, StringComparer.OrdinalIgnoreCase))
                    rootIds.Add(id);
            }

            return LoadClosure(rootIds);
        }

        public IReadOnlyList<SkillDescriptor> LoadForSkill(string id)
        {
            ValidateSkillId(id, "requested skill id");
            return LoadClosure(new[] { id });
        }

        public IReadOnlyList<SkillDescriptor> LoadAllForValidation()
        {
            var descriptors = new List<SkillDescriptor>();
            var directories = Directory.GetDirectories(_skillRoot)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            foreach (var directory in directories)
            {
                var manifest = Path.Combine(directory, "skill.yaml");
                if (!File.Exists(manifest))
                    throw new InvalidDataException("Skill directory is missing skill.yaml: " + Path.GetFileName(directory));

                var descriptor = ParseManifest(manifest);
                var directoryId = Path.GetFileName(directory);
                if (!string.Equals(directoryId, descriptor.Id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Manifest id must match its skill directory: " + directoryId + ".");
                descriptors.Add(descriptor);
            }

            ValidateGraph(descriptors);
            return descriptors.ToArray();
        }

        private IReadOnlyList<SkillDescriptor> LoadClosure(IEnumerable<string> rootIds)
        {
            var ordered = new List<SkillDescriptor>();
            var byId = new Dictionary<string, SkillDescriptor>(StringComparer.OrdinalIgnoreCase);
            var state = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);

            foreach (var id in rootIds)
                Visit(id, byId, state, ordered);

            return ordered.ToArray();
        }

        private void Visit(
            string id,
            IDictionary<string, SkillDescriptor> byId,
            IDictionary<string, VisitState> state,
            IList<SkillDescriptor> ordered)
        {
            ValidateSkillId(id, "skill dependency id");
            if (state.TryGetValue(id, out var current))
            {
                if (current == VisitState.Visiting)
                    throw new InvalidDataException("Skill dependency cycle detected at '" + id + "'.");
                if (current == VisitState.Visited)
                    return;
            }

            state[id] = VisitState.Visiting;
            if (!byId.TryGetValue(id, out var descriptor))
            {
                var path = Path.Combine(_skillRoot, id, "skill.yaml");
                EnsureWithinRoot(path);
                if (!File.Exists(path))
                    throw new InvalidDataException("Required repository skill manifest is missing: " + id + ".");
                descriptor = ParseManifest(path);
                if (!string.Equals(descriptor.Id, id, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Manifest id must match requested skill directory: " + id + ".");
                if (byId.ContainsKey(descriptor.Id))
                    throw new InvalidDataException("Duplicate skill id: " + descriptor.Id + ".");
                byId.Add(descriptor.Id, descriptor);
            }

            foreach (var prerequisite in descriptor.Prerequisites)
                Visit(prerequisite, byId, state, ordered);

            state[id] = VisitState.Visited;
            ordered.Add(descriptor);
        }

        private SkillDescriptor ParseManifest(string path)
        {
            var info = new FileInfo(path);
            if (!info.Exists)
                throw new InvalidDataException("Skill manifest is missing: " + path);
            if (info.Length <= 0 || info.Length > MaxManifestBytes)
                throw new InvalidDataException("Skill manifest size is outside the bounded schema limit: " + info.Name);

            var scalars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var lists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string? activeList = null;

            foreach (var raw in File.ReadAllLines(path))
            {
                if (raw.IndexOf('\t') >= 0)
                    throw new InvalidDataException("Tabs are not allowed in repository skill manifests.");

                var trimmed = raw.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (raw.StartsWith("  - ", StringComparison.Ordinal))
                {
                    if (activeList == null)
                        throw new InvalidDataException("List item appears outside a list key in " + info.Name + ".");
                    var item = raw.Substring(4).Trim();
                    ValidateListItem(item, activeList);
                    var values = lists[activeList];
                    if (values.Count >= MaxListItems)
                        throw new InvalidDataException("Manifest list exceeds bounded item count: " + activeList + ".");
                    values.Add(item);
                    continue;
                }

                if (char.IsWhiteSpace(raw[0]))
                    throw new InvalidDataException("Only two-space list indentation is allowed in repository skill manifests.");

                var colon = raw.IndexOf(':');
                if (colon <= 0)
                    throw new InvalidDataException("Malformed manifest key line: " + raw);

                var key = raw.Substring(0, colon).Trim();
                if (!AllowedKeys.Contains(key, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidDataException("Unknown manifest key: " + key + ".");
                if (!seen.Add(key))
                    throw new InvalidDataException("Duplicate manifest key: " + key + ".");

                var value = raw.Substring(colon + 1).Trim();
                activeList = null;
                if (IsListKey(key))
                {
                    lists[key] = new List<string>();
                    if (value.Length == 0)
                    {
                        activeList = key;
                    }
                    else if (!string.Equals(value, "[]", StringComparison.Ordinal))
                    {
                        throw new InvalidDataException("Only block lists or [] are supported for manifest key: " + key + ".");
                    }
                }
                else
                {
                    if (value.Length == 0)
                        throw new InvalidDataException("Manifest scalar value is required for key: " + key + ".");
                    scalars[key] = value;
                }
            }

            RequireKeys(seen);
            var id = scalars["id"];
            ValidateSkillId(id, "manifest id");
            if (!int.TryParse(scalars["version"], NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version <= 0)
                throw new InvalidDataException("Manifest version must be a positive integer: " + id + ".");

            var triggers = lists["triggers"].Select(ParseDomain).ToArray();
            if (triggers.Length == 0)
                throw new InvalidDataException("Skill manifest must declare at least one trigger domain: " + id + ".");

            foreach (var prerequisite in lists["prerequisites"])
                ValidateSkillId(prerequisite, "prerequisite");
            foreach (var document in lists["required-docs"])
                ValidateRepositoryRelativePath(document);

            return new SkillDescriptor(
                id,
                version,
                triggers,
                lists["prerequisites"],
                lists["required-docs"],
                lists["tool-classes"],
                lists["validation"]);
        }

        private static bool IsListKey(string key)
        {
            return !string.Equals(key, "id", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(key, "version", StringComparison.OrdinalIgnoreCase);
        }

        private static void RequireKeys(ISet<string> seen)
        {
            foreach (var key in AllowedKeys)
            {
                if (!seen.Contains(key))
                    throw new InvalidDataException("Missing required manifest key: " + key + ".");
            }
        }

        private static void ValidateListItem(string item, string key)
        {
            if (string.IsNullOrWhiteSpace(item))
                throw new InvalidDataException("Blank list item is not allowed for manifest key: " + key + ".");
            if (item.StartsWith("[", StringComparison.Ordinal) || item.StartsWith("{", StringComparison.Ordinal))
                throw new InvalidDataException("Nested or inline structures are not allowed in repository skill manifests.");
        }

        private static TaskDomain ParseDomain(string value)
        {
            if (!Enum.TryParse<TaskDomain>(value, true, out var domain) || !Enum.IsDefined(domain))
                throw new InvalidDataException("Unknown task domain in repository skill manifest: " + value + ".");
            return domain;
        }

        private static void ValidateSkillId(string id, string label)
        {
            if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
                throw new InvalidDataException("Invalid " + label + ".");
            for (var i = 0; i < id.Length; i++)
            {
                var ch = id[i];
                if (!(ch >= 'a' && ch <= 'z') && !(ch >= '0' && ch <= '9') && ch != '-')
                    throw new InvalidDataException("Invalid " + label + ": " + id + ".");
            }
        }

        private void ValidateRepositoryRelativePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.IndexOf(':') >= 0 || Path.IsPathRooted(value))
                throw new InvalidDataException("Canonical document path must be repository-relative: " + value + ".");

            var normalized = value.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            var full = Path.GetFullPath(Path.Combine(_repositoryRoot, normalized));
            EnsureWithinRoot(full);
        }

        private void EnsureWithinRoot(string path)
        {
            var full = Path.GetFullPath(path);
            var rootWithSeparator = _repositoryRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase) && !string.Equals(full, _repositoryRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Repository skill path escapes repository root: " + path + ".");
        }

        private static void ValidateGraph(IReadOnlyList<SkillDescriptor> descriptors)
        {
            var byId = new Dictionary<string, SkillDescriptor>(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in descriptors)
            {
                if (byId.ContainsKey(descriptor.Id))
                    throw new InvalidDataException("Duplicate skill id: " + descriptor.Id + ".");
                byId.Add(descriptor.Id, descriptor);
            }

            foreach (var descriptor in descriptors)
            {
                foreach (var prerequisite in descriptor.Prerequisites)
                {
                    if (!byId.ContainsKey(prerequisite))
                        throw new InvalidDataException("Unknown prerequisite skill: " + prerequisite + ".");
                }
            }

            var state = new Dictionary<string, VisitState>(StringComparer.OrdinalIgnoreCase);
            foreach (var descriptor in descriptors.OrderBy(item => item.Id, StringComparer.OrdinalIgnoreCase))
                ValidateAcyclic(descriptor, byId, state);
        }

        private static void ValidateAcyclic(
            SkillDescriptor descriptor,
            IReadOnlyDictionary<string, SkillDescriptor> byId,
            IDictionary<string, VisitState> state)
        {
            if (state.TryGetValue(descriptor.Id, out var current))
            {
                if (current == VisitState.Visiting)
                    throw new InvalidDataException("Skill dependency cycle detected at '" + descriptor.Id + "'.");
                if (current == VisitState.Visited)
                    return;
            }

            state[descriptor.Id] = VisitState.Visiting;
            foreach (var prerequisite in descriptor.Prerequisites)
                ValidateAcyclic(byId[prerequisite], byId, state);
            state[descriptor.Id] = VisitState.Visited;
        }

        private enum VisitState
        {
            Visiting,
            Visited
        }
    }
}
