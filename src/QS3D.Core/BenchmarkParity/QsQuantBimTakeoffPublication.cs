using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class QuantBimPublishedElementEvidence
    {
        public QuantBimPublishedElementEvidence(string guid, string entity, string storey, string type, string classification, string geometryReference)
        {
            Guid = QsModelElementSnapshot.Require(guid, "guid");
            Entity = QsModelElementSnapshot.Require(entity, "entity");
            Storey = QsModelElementSnapshot.Optional(storey);
            Type = QsModelElementSnapshot.Optional(type);
            Classification = QsModelElementSnapshot.Optional(classification);
            GeometryReference = QsModelElementSnapshot.Optional(geometryReference);
        }

        public string Guid { get; private set; }
        public string Entity { get; private set; }
        public string Storey { get; private set; }
        public string Type { get; private set; }
        public string Classification { get; private set; }
        public string GeometryReference { get; private set; }
    }

    public sealed class QuantBimPublishedBoqLine
    {
        public QuantBimPublishedBoqLine(string classification, string unit, double quantity, int sourceCount)
        {
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            if (sourceCount < 0) throw new ArgumentOutOfRangeException("sourceCount");
            SourceCount = sourceCount;
        }

        public string Classification { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
        public int SourceCount { get; private set; }
    }

    public sealed class QuantBimTakeoffPublicationSnapshot
    {
        public QuantBimTakeoffPublicationSnapshot(
            string documentPath,
            string revision,
            string selectionName,
            IEnumerable<QuantBimPublishedElementEvidence> evidence,
            IEnumerable<QuantBimPublishedBoqLine> boq)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            SelectionName = QsModelElementSnapshot.Require(selectionName, "selectionName");
            Evidence = new ReadOnlyCollection<QuantBimPublishedElementEvidence>((evidence ?? throw new ArgumentNullException("evidence")).ToList());
            Boq = new ReadOnlyCollection<QuantBimPublishedBoqLine>((boq ?? throw new ArgumentNullException("boq")).ToList());
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public string SelectionName { get; private set; }
        public IReadOnlyList<QuantBimPublishedElementEvidence> Evidence { get; private set; }
        public IReadOnlyList<QuantBimPublishedBoqLine> Boq { get; private set; }
    }

    public sealed class QuantBimTakeoffPublisher
    {
        public QuantBimTakeoffPublicationSnapshot Publish(IfcStandaloneDocument document, string expectedRevision, IfcSelectionSet selection)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (selection == null) throw new ArgumentNullException("selection");
            expectedRevision = QsModelElementSnapshot.Require(expectedRevision, "expectedRevision");
            if (!string.Equals(document.Revision, expectedRevision, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM takeoff publication is stale for the current IFC document revision.");

            var byGuid = new Dictionary<string, IfcStandaloneElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in document.Elements)
            {
                if (element == null) throw new InvalidOperationException("IFC document contains a null element.");
                if (byGuid.ContainsKey(element.Guid))
                    throw new InvalidOperationException("IFC document contains duplicate element GUID: " + element.Guid + ".");
                byGuid.Add(element.Guid, element);
            }

            var selected = new List<IfcStandaloneElement>();
            foreach (var guid in selection.Guids)
            {
                IfcStandaloneElement element;
                if (!byGuid.TryGetValue(guid, out element))
                    throw new InvalidOperationException("QuantBIM publication selection references a missing IFC element: " + guid + ".");
                selected.Add(element);
            }

            selected = selected
                .OrderBy(x => x.Guid, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Guid, StringComparer.Ordinal)
                .ToList();

            var evidence = selected.Select(x => new QuantBimPublishedElementEvidence(
                x.Guid, x.Entity, x.Storey, x.Type, x.Classification, x.GeometryReference)).ToList();

            var inventory = new IfcQtoWorkbench().Aggregate(selected.SelectMany(x => x.Quantities));
            var boq = inventory
                .Select(x => new QuantBimPublishedBoqLine(x.Classification, x.Unit, x.Quantity, x.SourceCount))
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Classification, StringComparer.Ordinal)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.Ordinal)
                .ToList();

            return new QuantBimTakeoffPublicationSnapshot(document.Path, document.Revision, selection.Name, evidence, boq);
        }

        public void ValidateCurrent(IfcStandaloneDocument document, QuantBimTakeoffPublicationSnapshot snapshot)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (!string.Equals(document.Path, snapshot.DocumentPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("QuantBIM publication belongs to a different IFC document path.");
            if (!string.Equals(document.Revision, snapshot.Revision, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM publication is stale for the current IFC document revision.");

            var guids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in document.Elements)
            {
                if (element == null) throw new InvalidOperationException("IFC document contains a null element.");
                if (!guids.Add(element.Guid))
                    throw new InvalidOperationException("IFC document contains duplicate element GUID: " + element.Guid + ".");
            }
            foreach (var evidence in snapshot.Evidence)
            {
                if (!guids.Contains(evidence.Guid))
                    throw new InvalidOperationException("QuantBIM publication evidence references an element missing from the current IFC document: " + evidence.Guid + ".");
            }
        }
    }

    public static class QuantBimTakeoffPublicationCodec
    {
        private const string Header = "QS3D-QUANTBIM-TAKEOFF-PUBLICATION/1";

        public static string Encode(QuantBimTakeoffPublicationSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var builder = new StringBuilder();
            builder.Append(Header).Append('\n');
            Append(builder, "DocumentPath", snapshot.DocumentPath);
            Append(builder, "Revision", snapshot.Revision);
            Append(builder, "SelectionName", snapshot.SelectionName);
            foreach (var evidence in snapshot.Evidence)
            {
                builder.Append("Evidence=")
                    .Append(Pack(evidence.Guid, evidence.Entity, evidence.Storey, evidence.Type, evidence.Classification, evidence.GeometryReference))
                    .Append('\n');
            }
            foreach (var line in snapshot.Boq)
            {
                builder.Append("Boq=")
                    .Append(Pack(line.Classification, line.Unit, line.Quantity.ToString("R", CultureInfo.InvariantCulture), line.SourceCount.ToString(CultureInfo.InvariantCulture)))
                    .Append('\n');
            }
            return builder.ToString();
        }

        public static QuantBimTakeoffPublicationSnapshot Decode(string encoded)
        {
            if (encoded == null) throw new ArgumentNullException("encoded");
            var lines = encoded.Replace("\r\n", "\n").Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length < 4 || !string.Equals(lines[0], Header, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported QuantBIM takeoff publication format.");

            var path = ReadSingle(lines[1], "DocumentPath");
            var revision = ReadSingle(lines[2], "Revision");
            var selection = ReadSingle(lines[3], "SelectionName");
            var evidence = new List<QuantBimPublishedElementEvidence>();
            var boq = new List<QuantBimPublishedBoqLine>();
            var seenBoq = false;

            for (var i = 4; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                {
                    if (i == lines.Length - 1) continue;
                    throw new InvalidOperationException("Unexpected blank line in QuantBIM takeoff publication.");
                }
                if (lines[i].StartsWith("Evidence=", StringComparison.Ordinal))
                {
                    if (seenBoq) throw new InvalidOperationException("Evidence records must precede BOQ records.");
                    var values = Unpack(lines[i].Substring("Evidence=".Length), 6);
                    evidence.Add(new QuantBimPublishedElementEvidence(values[0], values[1], values[2], values[3], values[4], values[5]));
                }
                else if (lines[i].StartsWith("Boq=", StringComparison.Ordinal))
                {
                    seenBoq = true;
                    var values = Unpack(lines[i].Substring("Boq=".Length), 4);
                    double quantity;
                    int sourceCount;
                    if (!double.TryParse(values[2], NumberStyles.Float, CultureInfo.InvariantCulture, out quantity) ||
                        !int.TryParse(values[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out sourceCount))
                        throw new InvalidOperationException("Invalid numeric value in QuantBIM takeoff publication BOQ record.");
                    boq.Add(new QuantBimPublishedBoqLine(values[0], values[1], quantity, sourceCount));
                }
                else
                {
                    throw new InvalidOperationException("Unknown QuantBIM takeoff publication record.");
                }
            }

            return new QuantBimTakeoffPublicationSnapshot(path, revision, selection, evidence, boq);
        }

        private static void Append(StringBuilder builder, string key, string value)
        {
            builder.Append(key).Append('=').Append(ToBase64(value)).Append('\n');
        }

        private static string ReadSingle(string line, string key)
        {
            var prefix = key + "=";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid QuantBIM takeoff publication field; expected " + key + ".");
            return FromBase64(line.Substring(prefix.Length));
        }

        private static string Pack(params string[] values)
        {
            return string.Join(".", values.Select(ToBase64).ToArray());
        }

        private static string[] Unpack(string packed, int expectedCount)
        {
            var parts = packed.Split('.');
            if (parts.Length != expectedCount) throw new InvalidOperationException("Malformed QuantBIM takeoff publication record.");
            return parts.Select(FromBase64).ToArray();
        }

        private static string ToBase64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        }

        private static string FromBase64(string value)
        {
            try { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
            catch (FormatException ex) { throw new InvalidOperationException("Invalid base64 in QuantBIM takeoff publication.", ex); }
        }
    }
}
