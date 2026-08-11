using System;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public sealed class FamilyDefinition
    {
        private ElementCategory _category;
        private string _name;
        private string _material;

        public FamilyDefinition(string name, ElementCategory category, string material = "Khác")
        {
            _name = RequireName(name);
            Category = category;
            _material = NormalizeMaterial(material);
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name
        {
            get => _name;
            set => _name = RequireName(value);
        }
        public ElementCategory Category
        {
            get => _category;
            set
            {
                if (!Enum.IsDefined(typeof(ElementCategory), value))
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Family category must be a defined ElementCategory.");
                _category = value;
            }
        }
        public string Material
        {
            get => _material;
            set => _material = NormalizeMaterial(value);
        }
        public string ColorMode { get; set; } = "Theo loại (mặc định)";
        public string Transparency { get; set; } = "ByLayer";
        public IDictionary<string, string> Metadata { get; }

        private static string RequireName(string value) =>
            string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Family name is required.", nameof(value)) : value.Trim();

        private static string NormalizeMaterial(string value) =>
            string.IsNullOrWhiteSpace(value) ? "Khác" : value.Trim();
    }
}
