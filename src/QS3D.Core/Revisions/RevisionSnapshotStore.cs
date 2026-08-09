using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QS3D.Core.Revisions
{
    public sealed class RevisionSnapshotStore
    {
        public void Save(RevisionSnapshot snapshot, string path)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            var full = Path.GetFullPath(path); var dir = Path.GetDirectoryName(full); if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
            var temp = full + ".tmp"; var backup = full + ".bak";
            Serialize(snapshot).Save(temp, SaveOptions.DisableFormatting); Load(temp);
            if (File.Exists(full))
            {
                File.Copy(full, backup, true);
                try { File.Replace(temp, full, backup, true); }
                catch (PlatformNotSupportedException) { File.Delete(full); File.Move(temp, full); }
            }
            else File.Move(temp, full);
        }

        public RevisionSnapshot Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            var root = XDocument.Load(path, LoadOptions.None).Root ?? throw new InvalidDataException("Revision file has no root.");
            if (!string.Equals(root.Name.LocalName, "qs3dRevision", StringComparison.Ordinal)) throw new InvalidDataException("Invalid QS3D revision root.");
            var snapshot = new RevisionSnapshot { Id = Required(root, "id"), CreatedUtc = Date(root.Attribute("createdUtc")?.Value) };
            var elements = root.Element("elements");
            if (elements != null)
            {
                foreach (var node in elements.Elements("element"))
                {
                    var item = new RevisionElementSnapshot { ElementId = Required(node, "id"), Category = Value(node, "category"), FamilyId = Value(node, "familyId") };
                    foreach (var q in node.Element("quantities")?.Elements("q") ?? Enumerable.Empty<XElement>()) item.Quantities[Required(q, "name")] = Number(q.Attribute("value")?.Value);
                    snapshot.Elements.Add(item);
                }
            }
            if (snapshot.Elements.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) throw new InvalidDataException("Revision contains duplicate element ids.");
            return snapshot;
        }

        private static XDocument Serialize(RevisionSnapshot snapshot) => new XDocument(new XElement("qs3dRevision", new XAttribute("id", snapshot.Id ?? string.Empty), new XAttribute("createdUtc", snapshot.CreatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)), new XElement("elements", snapshot.Elements.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Select(x => new XElement("element", new XAttribute("id", x.ElementId), new XAttribute("category", x.Category ?? string.Empty), new XAttribute("familyId", x.FamilyId ?? string.Empty), new XElement("quantities", x.Quantities.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).Select(q => new XElement("q", new XAttribute("name", q.Key), new XAttribute("value", q.Value.ToString("R", CultureInfo.InvariantCulture))))))))));
        private static string Required(XElement e, string name) => !string.IsNullOrWhiteSpace(e.Attribute(name)?.Value) ? e.Attribute(name)!.Value.Trim() : throw new InvalidDataException("Missing attribute: " + name);
        private static string Value(XElement e, string name) => e.Attribute(name)?.Value?.Trim() ?? string.Empty;
        private static double Number(string? value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && !double.IsNaN(result) && !double.IsInfinity(result) ? result : throw new InvalidDataException("Invalid revision quantity.");
        private static DateTime Date(string? value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result.ToUniversalTime() : throw new InvalidDataException("Invalid revision timestamp.");
    }
}
