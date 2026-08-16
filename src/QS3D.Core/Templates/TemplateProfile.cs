using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Templates
{
    public sealed class TemplateProfile
    {
        private string _name;

        public TemplateProfile(string id, string name)
        {
            Id = RequirePersistedText(id, nameof(id), "Template id");
            _name = RequireName(name);
        }
        public string Id { get; }
        public string Name
        {
            get => _name;
            set => _name = RequireName(value);
        }
        public IList<ProjectFamily> Families { get; } = new List<ProjectFamily>();
        public IList<QuantityRule> QuantityRules { get; } = new List<QuantityRule>();
        public IDictionary<string, string> LayerMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IList<string> VisibleBqColumns { get; } = new List<string>();

        private static string RequireName(string value) =>
            RequirePersistedText(value, nameof(value), "Template name");

        private static string RequirePersistedText(string value, string parameterName, string label)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(label + " is required.", parameterName);

            var normalized = value.Trim();
            if (normalized.Any(char.IsControl))
                throw new ArgumentException(label + " cannot contain control characters.", parameterName);

            return PersistedTextXml.Verify(normalized, parameterName, label);
        }
    }
}
