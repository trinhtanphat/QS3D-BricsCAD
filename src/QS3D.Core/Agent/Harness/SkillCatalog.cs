using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Agent.Harness
{
    public sealed class SkillCatalog
    {
        private readonly SkillDescriptor[] _skills;
        private readonly Dictionary<string, SkillDescriptor> _byId;

        public SkillCatalog(IEnumerable<SkillDescriptor> skills)
        {
            if (skills == null)
                throw new ArgumentNullException(nameof(skills));

            _skills = skills.ToArray();
            _byId = new Dictionary<string, SkillDescriptor>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < _skills.Length; i++)
            {
                var skill = _skills[i] ?? throw new ArgumentException("Skill catalog cannot contain null descriptors.", nameof(skills));
                if (_byId.ContainsKey(skill.Id))
                    throw new InvalidOperationException("Duplicate skill id: " + skill.Id + ".");
                _byId.Add(skill.Id, skill);
            }
        }

        public IReadOnlyList<SkillDescriptor> Skills => _skills;

        public SkillDescriptor Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Skill id is required.", nameof(id));

            if (!_byId.TryGetValue(id, out var skill) || skill == null)
                throw new InvalidOperationException("Unknown prerequisite skill: " + id + ".");
            return skill;
        }

        public static SkillCatalog CreateDefault()
        {
            var none = Array.Empty<string>();
            return new SkillCatalog(new[]
            {
                new SkillDescriptor(
                    "repository-lifecycle",
                    1,
                    new[]
                    {
                        TaskDomain.Source,
                        TaskDomain.ContinuousIntegration,
                        TaskDomain.GithubCarrier,
                        TaskDomain.McpTransport,
                        TaskDomain.PersistenceDurability,
                        TaskDomain.BricsCadHost,
                        TaskDomain.ReleasePackage,
                        TaskDomain.CadInspect,
                        TaskDomain.CadMutate
                    },
                    none,
                    new[] { "AGENTS.md", "docs/AGENT-RESERVATION-V2.md", "docs/AGENT-WORK-REGISTRATION.md" },
                    none,
                    new[] { "canonical-carrier", "exact-head-evidence" }),
                new SkillDescriptor(
                    "tdd-source",
                    1,
                    new[] { TaskDomain.Source },
                    new[] { "repository-lifecycle" },
                    none,
                    new[] { "source.read", "source.edit", "process.test" },
                    new[] { "red-before-green" }),
                new SkillDescriptor(
                    "ci-remediation",
                    1,
                    new[] { TaskDomain.ContinuousIntegration },
                    new[] { "repository-lifecycle", "tdd-source" },
                    new[] { "CI_POLICY.md" },
                    new[] { "github.ci", "process.test" },
                    new[] { "exact-head-evidence" }),
                new SkillDescriptor(
                    "github-lifecycle",
                    1,
                    new[] { TaskDomain.GithubCarrier },
                    new[] { "repository-lifecycle" },
                    new[] { "docs/MAIN-WRITE-AUTHORIZATION.md" },
                    new[] { "github.issue", "github.pr" },
                    new[] { "protected-merge-gates" }),
                new SkillDescriptor(
                    "mcp-transport",
                    1,
                    new[] { TaskDomain.McpTransport },
                    new[] { "repository-lifecycle", "tdd-source" },
                    new[] { "docs/MCP-CANONICAL-RUNBOOK.md" },
                    new[] { "mcp.inspect" },
                    new[] { "transport-identity" }),
                new SkillDescriptor(
                    "persistence-durability",
                    1,
                    new[] { TaskDomain.PersistenceDurability },
                    new[] { "mcp-transport" },
                    none,
                    new[] { "source.read", "source.edit", "process.test" },
                    new[] { "stable-mutation-identity", "durability-boundary" }),
                new SkillDescriptor(
                    "bricscad-host",
                    1,
                    new[] { TaskDomain.BricsCadHost },
                    new[] { "repository-lifecycle" },
                    new[] { "docs/REMOTE-AGENT-SCOPE.md" },
                    new[] { "cad.host" },
                    new[] { "v25-v26-boundary", "local-only-boundary" }),
                new SkillDescriptor(
                    "cad-safety",
                    1,
                    new[] { TaskDomain.CadInspect, TaskDomain.CadMutate },
                    new[] { "bricscad-host" },
                    none,
                    new[] { "cad.inspect", "cad.mutate" },
                    new[] { "document-affinity", "typed-mutation" }),
                new SkillDescriptor(
                    "release-local-only",
                    1,
                    new[] { TaskDomain.ReleasePackage },
                    new[] { "repository-lifecycle" },
                    new[] { "docs/REMOTE-AGENT-SCOPE.md" },
                    new[] { "release.inspect" },
                    new[] { "local-only-boundary" })
            });
        }
    }
}
