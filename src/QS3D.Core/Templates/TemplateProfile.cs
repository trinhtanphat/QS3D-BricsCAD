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
            Id = RequireIdentityText(id, "Template id is required.", "Template id contains invalid text.", nameof(id));
            _name = RequireIdentityText(name, "Template name is required.", "Template name contains invalid text.", nameof(name));
        }
        public string Id { get; }
        public string Name
        {
            get => _name;
            set => _name = RequireIdentityText(value, "Template name is required.", "Template name contains invalid text.", nameof(value));
        }
        public IList<ProjectFamily> Families { get; } = new List<ProjectFamily>();
        public IList<QuantityRule> QuantityRules { get; } = new List<QuantityRule>();
        public IDictionary<string, string> LayerMappings { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public IList<string> VisibleBqColumns { get; } = new List<string>();

        private static string RequireIdentityText(string value, string requiredMessage, string invalidMessage, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(requiredMessage, parameterName);

            for (var index = 0; index < value.Length; index++)
            {
                if (char.IsControl(value[index]))
                    throw new ArgumentException(invalidMessage, parameterName);
            }

            try
            {
                XmlConvert.VerifyXmlChars(value);
            }
            catch (XmlException exception)
            {
                throw new ArgumentException(invalidMessage, parameterName, exception);
            }

            return value.Trim();
        }
    }
}
