using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Agent.Harness
{
    public sealed class SkillDescriptor
    {
        private readonly TaskDomain[] _triggers;
        private readonly string[] _prerequisites;
        private readonly string[] _requiredDocuments;
        private readonly string[] _toolClasses;
        private readonly string[] _validationExpectations;

        public SkillDescriptor(
            string id,
            int version,
            IEnumerable<TaskDomain> triggers,
            IEnumerable<string> prerequisites,
            IEnumerable<string> requiredDocuments,
            IEnumerable<string> toolClasses,
            IEnumerable<string> validationExpectations)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Skill id is required.", nameof(id));
            if (version <= 0)
                throw new ArgumentOutOfRangeException(nameof(version));
            if (triggers == null)
                throw new ArgumentNullException(nameof(triggers));
            if (prerequisites == null)
                throw new ArgumentNullException(nameof(prerequisites));
            if (requiredDocuments == null)
                throw new ArgumentNullException(nameof(requiredDocuments));
            if (toolClasses == null)
                throw new ArgumentNullException(nameof(toolClasses));
            if (validationExpectations == null)
                throw new ArgumentNullException(nameof(validationExpectations));

            Id = id.Trim();
            Version = version;
            _triggers = triggers.Distinct().ToArray();
            _prerequisites = NormalizeStrings(prerequisites);
            _requiredDocuments = NormalizeStrings(requiredDocuments);
            _toolClasses = NormalizeStrings(toolClasses);
            _validationExpectations = NormalizeStrings(validationExpectations);
        }

        public string Id { get; }
        public int Version { get; }
        public IReadOnlyList<TaskDomain> Triggers => _triggers;
        public IReadOnlyList<string> Prerequisites => _prerequisites;
        public IReadOnlyList<string> RequiredDocuments => _requiredDocuments;
        public IReadOnlyList<string> ToolClasses => _toolClasses;
        public IReadOnlyList<string> ValidationExpectations => _validationExpectations;

        private static string[] NormalizeStrings(IEnumerable<string> values)
        {
            var result = values
                .Select(value => value == null ? string.Empty : value.Trim())
                .ToArray();

            if (result.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Skill metadata entries cannot be blank.");

            return result.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
