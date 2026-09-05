using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Agent.Harness
{
    public enum TaskDomain
    {
        Source,
        ContinuousIntegration,
        GithubCarrier,
        McpTransport,
        PersistenceDurability,
        BricsCadHost,
        ReleasePackage,
        CadInspect,
        CadMutate
    }

    public sealed class TaskIntent
    {
        private readonly TaskDomain[] _domains;
        private readonly string[] _evidence;

        public TaskIntent(string outcome, IEnumerable<TaskDomain> domains, IEnumerable<string> evidence)
        {
            if (string.IsNullOrWhiteSpace(outcome))
                throw new ArgumentException("Task outcome is required.", nameof(outcome));
            if (domains == null)
                throw new ArgumentNullException(nameof(domains));
            if (evidence == null)
                throw new ArgumentNullException(nameof(evidence));

            Outcome = outcome.Trim();
            _domains = domains.Distinct().ToArray();
            _evidence = evidence
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (_domains.Length == 0)
                throw new ArgumentException("At least one task domain is required.", nameof(domains));
        }

        public string Outcome { get; }
        public IReadOnlyList<TaskDomain> Domains => _domains;
        public IReadOnlyList<string> Evidence => _evidence;
    }
}
