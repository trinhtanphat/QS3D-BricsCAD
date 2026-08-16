using System;
using System.Collections.Generic;
using System.Xml;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.Templates
{
    public sealed class TemplateProfile
    {
        private string _name;

        public TemplateProfile(string id, string name)
        {
            Id = RequireText(id, "Template id is required.", nameof(id));
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
            RequireText(value, "Template name is required.", nameof(value));

        private static string RequireText(string value, string requiredMessage, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(requiredMessage, parameterName);

            var normalized = value.Trim();
            for (var i = 0; i < normalized.Length; i++)
            {
                if (char.IsControl(normalized[i]))
                    throw new ArgumentException("Template identity text cannot contain control characters.", parameterName);
            }

            try
            {
                XmlConvert.VerifyXmlChars(normalized);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException("Template identity text contains characters that cannot be stored in XML.", parameterName, ex);
            }

            return normalized;
        }
    }
}
