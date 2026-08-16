using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace QS3D.Core.Domain
{
    public sealed class ProjectMaterial
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public ProjectMaterial(string id, string name, string unit, string description, bool builtIn)
        {
            Id = Required(id, nameof(id), 64);
            Name = Required(name, nameof(name), 120);
            Unit = Optional(unit, nameof(unit), 24);
            Description = Optional(description, nameof(description), 240);
            IsBuiltIn = builtIn;
        }

        public string Id { get; }
        public string Name { get; }
        public string Unit { get; }
        public string Description { get; }
        public bool IsBuiltIn { get; }

        private static string Required(string value, string name, int max)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length == 0 || text.Length > max) throw new ArgumentException(name + " must contain 1.." + max + " characters.", name);
            if (text.Any(char.IsControl)) throw new ArgumentException(name + " cannot contain control characters.", name);
            RequireWellFormedUnicode(text, name);
            RequireXmlText(text, name);
            return text;
        }

        private static string Optional(string value, string name, int max)
        {
            var text = (value ?? string.Empty).Trim();
            if (text.Length > max) throw new ArgumentException(name + " must contain at most " + max + " characters.", name);
            RequireWellFormedUnicode(text, name);
            RequireXmlText(text, name);
            return text;
        }

        private static void RequireWellFormedUnicode(string text, string name)
        {
            try
            {
                StrictUtf8.GetByteCount(text);
            }
            catch (EncoderFallbackException)
            {
                throw new ArgumentException(name + " must contain well-formed Unicode text.", name);
            }
        }

        private static void RequireXmlText(string text, string name)
        {
            try
            {
                XmlConvert.VerifyXmlChars(text);
            }
            catch (XmlException ex)
            {
                throw new ArgumentException(name + " contains characters that are invalid in XML.", name, ex);
            }
        }
    }

    public static class ProjectMaterialCatalog
    {
        public const string MetadataKey = "QS3D.MaterialCatalog.v1";
        private const int MaxCustomMaterials = 500;
        private const int MaxSerializedLength = 1024 * 1024;
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static readonly ProjectMaterial[] BuiltIns =
        {
            new ProjectMaterial("builtin-concrete", "Bê tông", "m³", "", true),
            new ProjectMaterial("builtin-steel", "Thép", "kg", "", true),
            new ProjectMaterial("builtin-brick", "Gạch", "m²", "", true),
            new ProjectMaterial("builtin-glass", "Kính", "m²", "", true),
            new ProjectMaterial("builtin-aluminium", "Nhôm", "m", "", true),
            new ProjectMaterial("builtin-waterproof", "Chống thấm", "m²", "", true),
            new ProjectMaterial("builtin-paint", "Sơn", "m²", "", true),
            new ProjectMaterial("builtin-wood", "Gỗ", "m²", "", true),
            new ProjectMaterial("builtin-earth", "Đất", "m³", "", true)
        };

        public static IReadOnlyList<ProjectMaterial> GetAll(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var result = new List<ProjectMaterial>(BuiltIns);
            result.AddRange(ReadCustom(project));
            return result
                .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList()
                .AsReadOnly();
        }

        public static IReadOnlyList<ProjectMaterial> GetCustom(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            return ReadCustom(project).AsReadOnly();
        }

        public static ProjectMaterial UpsertCustom(ProjectState project, string id, string name, string unit, string description)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var material = new ProjectMaterial(id, name, unit, description, false);
            EnsureDoesNotShadowBuiltIn(material);

            var custom = ReadCustom(project);
            var byId = custom.FindIndex(x => string.Equals(x.Id, material.Id, StringComparison.OrdinalIgnoreCase));
            var duplicateName = custom.FirstOrDefault(x => !string.Equals(x.Id, material.Id, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, material.Name, StringComparison.OrdinalIgnoreCase));
            if (duplicateName != null) throw new InvalidOperationException("Another custom material already uses the name '" + material.Name + "'.");

            string? previousName = null;
            if (byId >= 0)
            {
                var existing = custom[byId];
                if (SameMaterial(existing, material)) return existing;
                previousName = existing.Name;
                custom[byId] = material;
            }
            else
            {
                if (custom.Count >= MaxCustomMaterials) throw new InvalidOperationException("Project material catalog supports at most " + MaxCustomMaterials + " custom materials.");
                custom.Add(material);
            }

            MaterialReferenceScope? referenceScope = null;
            var renaming = previousName != null && !string.IsNullOrWhiteSpace(previousName) && !string.Equals(previousName, material.Name, StringComparison.Ordinal);
            if (renaming)
                referenceScope = ResolveReferenceScope(project);

            WriteCustom(project, custom);
            if (renaming)
                RenameReferences(referenceScope!, previousName!, material.Name);
            return material;
        }

        public static bool DeleteCustom(ProjectState project, string id)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var normalized = (id ?? string.Empty).Trim();
            if (normalized.Length == 0) return false;
            var custom = ReadCustom(project);
            var material = custom.FirstOrDefault(x => string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase));
            if (material == null) return false;
            if (ReferencedMaterialNames(project).Any(x => string.Equals(x, material.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Material '" + material.Name + "' is still referenced by a Family or Instance and cannot be deleted.");
            custom.RemoveAll(x => string.Equals(x.Id, normalized, StringComparison.OrdinalIgnoreCase));
            WriteCustom(project, custom);
            return true;
        }

        public static IReadOnlyList<string> ReferencedMaterialNames(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var scope = ResolveReferenceScope(project);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in scope.Families)
                AddMaterial(family.Properties, names);
            foreach (var element in scope.Elements)
                AddMaterial(element.Properties, names);
            return names.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly();
        }

        private static void RenameReferences(MaterialReferenceScope scope, string previousName, string nextName)
        {
            var inheritedMaterialFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inheritedFrameFamilies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in scope.Families)
            {
                if (RenameReference(family.Properties, "Material", previousName, nextName)) inheritedMaterialFamilies.Add(family.Id);
                if (RenameReference(family.Properties, "CurtainFrameMaterial", previousName, nextName)) inheritedFrameFamilies.Add(family.Id);
            }
            foreach (var element in scope.Elements)
            {
                var familyId = (element.FamilyId ?? string.Empty).Trim();
                RenameElementReference(element, "Material", previousName, nextName, inheritedMaterialFamilies.Contains(familyId));
                RenameElementReference(element, "CurtainFrameMaterial", previousName, nextName, inheritedFrameFamilies.Contains(familyId));
            }
        }

        private static MaterialReferenceScope ResolveReferenceScope(ProjectState project)
        {
            var families = new List<ProjectFamily>(project.Families.Count);
            var familyIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var family in project.Families)
            {
                if (family == null)
                    throw new InvalidOperationException("Project contains a null family entry.");
                var familyId = (family.Id ?? string.Empty).Trim();
                if (familyId.Length == 0)
                    throw new InvalidOperationException("Project contains a family with a blank semantic id.");
                if (!familyIds.Add(familyId))
                    throw new InvalidOperationException("Project contains duplicate family id: " + familyId);
                families.Add(family);
            }

            var elements = new List<ProjectElement>(project.Elements.Count);
            var elementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Project contains a null element entry.");
                var elementId = (element.Id ?? string.Empty).Trim();
                if (elementId.Length == 0)
                    throw new InvalidOperationException("Project contains an element with a blank semantic id.");
                if (!elementIds.Add(elementId))
                    throw new InvalidOperationException("Project contains duplicate element id: " + elementId);
                elements.Add(element);
            }

            return new MaterialReferenceScope(families, elements);
        }

        private static bool RenameReference(IDictionary<string, string> properties, string key, string previousName, string nextName)
        {
            if (!properties.TryGetValue(key, out var value) || !string.Equals((value ?? string.Empty).Trim(), previousName, StringComparison.OrdinalIgnoreCase)) return false;
            properties[key] = nextName;
            return true;
        }

        private static void RenameElementReference(ProjectElement element, string key, string previousName, string nextName, bool inheritedFamilyChanged)
        {
            if (element.Properties.TryGetValue(key, out var value))
            {
                if (string.Equals((value ?? string.Empty).Trim(), previousName, StringComparison.OrdinalIgnoreCase))
                    element.SetProperty(key, nextName);
                return;
            }
            if (inheritedFamilyChanged)
                element.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity);
        }

        private static void AddMaterial(IDictionary<string, string> properties, ISet<string> names)
        {
            if (properties.TryGetValue("Material", out var material) && !string.IsNullOrWhiteSpace(material)) names.Add(material.Trim());
            if (properties.TryGetValue("CurtainFrameMaterial", out var frame) && !string.IsNullOrWhiteSpace(frame)) names.Add(frame.Trim());
        }

        private static bool SameMaterial(ProjectMaterial left, ProjectMaterial right)
        {
            return string.Equals(left.Name, right.Name, StringComparison.Ordinal) &&
                   string.Equals(left.Unit, right.Unit, StringComparison.Ordinal) &&
                   string.Equals(left.Description, right.Description, StringComparison.Ordinal);
        }

        private static List<ProjectMaterial> ReadCustom(ProjectState project)
        {
            if (!project.Metadata.TryGetValue(MetadataKey, out var raw) || string.IsNullOrEmpty(raw)) return new List<ProjectMaterial>();
            if (raw.Length > MaxSerializedLength)
                throw new InvalidOperationException("Stored material catalog exceeds the serialized safety limit.");
            var lines = raw.Split(new[] { '\n' }, MaxCustomMaterials + 1, StringSplitOptions.None);
            if (lines.Length > MaxCustomMaterials) throw new InvalidOperationException("Stored material catalog exceeds the supported custom-material limit.");
            var result = new List<ProjectMaterial>(lines.Length);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]))
                    throw new InvalidOperationException("Material catalog contains an empty record at line " + (index + 1) + ".");
                var fields = lines[index].Split(new[] { '|' }, 5, StringSplitOptions.None);
                if (fields.Length != 4) throw new InvalidOperationException("Invalid material catalog record at line " + (index + 1) + ".");
                var lineNumber = index + 1;
                var material = new ProjectMaterial(
                    DecodeCanonicalText(fields[0], "id", lineNumber),
                    DecodeCanonicalText(fields[1], "name", lineNumber),
                    DecodeCanonicalText(fields[2], "unit", lineNumber),
                    DecodeCanonicalText(fields[3], "description", lineNumber),
                    false);
                EnsureDoesNotShadowBuiltIn(material);
                if (!ids.Add(material.Id)) throw new InvalidOperationException("Duplicate material id in project catalog: " + material.Id);
                if (!names.Add(material.Name)) throw new InvalidOperationException("Duplicate material name in project catalog: " + material.Name);
                result.Add(material);
            }
            return result;
        }

        private static string DecodeCanonicalText(string encoded, string label, int lineNumber)
        {
            var decoded = Decode(encoded);
            if (!string.Equals(decoded, decoded.Trim(), StringComparison.Ordinal))
                throw new InvalidOperationException("Material catalog contains non-canonical decoded " + label + " text at line " + lineNumber + ".");
            return decoded;
        }

        private static void EnsureDoesNotShadowBuiltIn(ProjectMaterial material)
        {
            if (BuiltIns.Any(x => string.Equals(x.Id, material.Id, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("Built-in material ids cannot be overwritten: " + material.Id + ".");
            if (BuiltIns.Any(x => string.Equals(x.Name, material.Name, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidOperationException("A built-in material already uses the name '" + material.Name + "'.");
        }

        private static void WriteCustom(ProjectState project, IEnumerable<ProjectMaterial> source)
        {
            var custom = source.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase).ToList();
            if (custom.Count == 0)
            {
                project.Metadata.Remove(MetadataKey);
                return;
            }
            project.Metadata[MetadataKey] = string.Join("\n", custom.Select(x => string.Join("|", Encode(x.Id), Encode(x.Name), Encode(x.Unit), Encode(x.Description))));
        }

        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));

        private static string Decode(string value)
        {
            try
            {
                var encoded = value ?? string.Empty;
                var bytes = Convert.FromBase64String(encoded);
                if (!string.Equals(Convert.ToBase64String(bytes), encoded, StringComparison.Ordinal))
                    throw new InvalidOperationException("Material catalog contains non-canonical Base64 data.");
                return StrictUtf8.GetString(bytes);
            }
            catch (Exception ex) when (ex is FormatException || ex is DecoderFallbackException)
            {
                throw new InvalidOperationException("Material catalog contains invalid Base64 or UTF-8 data.", ex);
            }
        }

        private sealed class MaterialReferenceScope
        {
            public MaterialReferenceScope(IReadOnlyList<ProjectFamily> families, IReadOnlyList<ProjectElement> elements)
            {
                Families = families;
                Elements = elements;
            }

            public IReadOnlyList<ProjectFamily> Families { get; }
            public IReadOnlyList<ProjectElement> Elements { get; }
        }
    }
}
