using System;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public sealed class FamilyDefinition
    {
        private ElementCategory _category;

        public FamilyDefinition(string name, ElementCategory category, string material = "Khác")
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Family name is required.", nameof(name));
            Name = name.Trim();
            Category = category;
            Material = string.IsNullOrWhiteSpace(material) ? "Khác" : material.Trim();
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; set; }
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
        public string Material { get; set; }
        public string ColorMode { get; set; } = "Theo loại (mặc định)";
        public string Transparency { get; set; } = "ByLayer";
        public IDictionary<string, string> Metadata { get; }
    }
}
