using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Templates
{
    public sealed class TemplateProfile
    {
        private string _name;

        public TemplateProfile(string id, string name)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Template id is required.", nameof(id)) : id.Trim();
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
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Template name is required.", nameof(value)) : value.Trim();
    }
}
