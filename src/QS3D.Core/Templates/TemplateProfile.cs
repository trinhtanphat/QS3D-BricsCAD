using System;
using System.Collections.Generic;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Templates
{
    public sealed class TemplateProfile
    {
        public TemplateProfile(string id, string name)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Template id is required.", nameof(id)) : id.Trim();
            Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("Template name is required.", nameof(name)) : name.Trim();
        }
        public string Id { get; }
        public string Name { get; set; }
        public IList<ProjectFamily> Families { get; } = new List<ProjectFamily>();
        public IList<QuantityRule> QuantityRules { get; } = new List<QuantityRule>();
        public IDictionary<string, string> LayerMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IList<string> VisibleBqColumns { get; } = new List<string>();
    }
}
