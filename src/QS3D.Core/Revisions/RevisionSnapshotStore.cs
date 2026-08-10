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
            var full = Path.GetFullPath(path); var directory = Path.GetDirectoryName(full); if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            var temp = full + ".tmp"; var backup = full + ".bak";
            try
            {
                Serialize(snapshot).Save(temp, SaveOptions.DisableFormatting);
                Load(temp);
                if (File.Exists(full))
                {
                    try { File.Replace(temp, full, backup, true); }
                    catch (PlatformNotSupportedException) { File.Copy(full, backup, true); File.Delete(full); File.Move(temp, full); }
                }
                else File.Move(temp, full);
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
        }

        public RevisionSnapshot LoadWithBackupFallback(string path)
        {
            try { return Load(path); }
            catch when (File.Exists(path + ".bak")) { return Load(path + ".bak"); }
        }

        public RevisionSnapshot Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Revision path is required.", nameof(path));
            var root = XDocument.Load(path, LoadOptions.None).Root ?? throw new InvalidDataException("Revision file has no root.");
            if (!string.Equals(root.Name.LocalName, "qs3dRevision", StringComparison.Ordinal)) throw new InvalidDataException("Invalid QS3D revision root.");
            var snapshot = new RevisionSnapshot { Id = Required(root, "id"), CreatedUtc = Date(root.Attribute("createdUtc")?.Value) };
            foreach (var node in root.Element("elements")?.Elements("element") ?? Enumerable.Empty<XElement>())
            {
                var item = new RevisionElementSnapshot
                {
                    ElementId = Required(node, "id"), Category = Value(node, "category"), FamilyId = Value(node, "familyId"),
                    FloorId = Value(node, "floorId"), ZoneId = Value(node, "zoneId")
                };
                foreach (var property in node.Element("properties")?.Elements("p") ?? Enumerable.Empty<XElement>())
                {
                    var name = Required(property, "name"); if (item.Properties.ContainsKey(name)) throw new InvalidDataException("Duplicate revision property: " + name); item.Properties[name] = property.Attribute("value")?.Value ?? string.Empty;
                }
                foreach (var quantity in node.Element("quantities")?.Elements("q") ?? Enumerable.Empty<XElement>())
                {
                    var name = Required(quantity, "name"); if (item.Quantities.ContainsKey(name)) throw new InvalidDataException("Duplicate revision quantity: " + name); item.Quantities[name] = Number(quantity.Attribute("value")?.Value);
                }
                foreach (var handle in node.Element("sourceHandles")?.Elements("h") ?? Enumerable.Empty<XElement>())
                {
                    var value = handle.Attribute("value")?.Value?.Trim();
                    if (string.IsNullOrWhiteSpace(value)) continue;
                    var safeValue = value!;
                    if (!item.SourceHandles.Contains(safeValue, StringComparer.OrdinalIgnoreCase)) item.SourceHandles.Add(safeValue);
                }
                snapshot.Elements.Add(item);
            }
            if (snapshot.Elements.GroupBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Any(x => x.Count() > 1)) throw new InvalidDataException("Revision contains duplicate element ids.");
            return snapshot;
        }

        private static XDocument Serialize(RevisionSnapshot snapshot) => new XDocument(
            new XElement("qs3dRevision",
                new XAttribute("id", snapshot.Id ?? string.Empty),
                new XAttribute("createdUtc", snapshot.CreatedUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)),
                new XElement("elements", snapshot.Elements.OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase).Select(x =>
                    new XElement("element",
                        new XAttribute("id", x.ElementId), new XAttribute("category", x.Category ?? string.Empty), new XAttribute("familyId", x.FamilyId ?? string.Empty),
                        new XAttribute("floorId", x.FloorId ?? string.Empty), new XAttribute("zoneId", x.ZoneId ?? string.Empty),
                        new XElement("properties", x.Properties.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase).Select(p => new XElement("p", new XAttribute("name", p.Key), new XAttribute("value", p.Value ?? string.Empty)))),
                        new XElement("quantities", x.Quantities.OrderBy(q => q.Key, StringComparer.OrdinalIgnoreCase).Select(q => new XElement("q", new XAttribute("name", q.Key), new XAttribute("value", q.Value.ToString("R", CultureInfo.InvariantCulture))))),
                        new XElement("sourceHandles", x.SourceHandles.Where(h => !string.IsNullOrWhiteSpace(h)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(h => h, StringComparer.OrdinalIgnoreCase).Select(h => new XElement("h", new XAttribute("value", h.Trim())))))))));

        private static string Required(XElement element, string name) => !string.IsNullOrWhiteSpace(element.Attribute(name)?.Value) ? element.Attribute(name)!.Value.Trim() : throw new InvalidDataException("Missing attribute: " + name);
        private static string Value(XElement element, string name) => element.Attribute(name)?.Value?.Trim() ?? string.Empty;
        private static double Number(string? value) => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && !double.IsNaN(result) && !double.IsInfinity(result) ? result : throw new InvalidDataException("Invalid revision quantity.");
        private static DateTime Date(string? value) => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var result) ? result.ToUniversalTime() : throw new InvalidDataException("Invalid revision timestamp.");
    }
}
