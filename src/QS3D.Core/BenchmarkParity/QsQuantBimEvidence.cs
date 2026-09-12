using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class QuantBimEvidenceLine
    {
        public QuantBimEvidenceLine(
            string documentPath,
            string revision,
            string guid,
            string entity,
            string storey,
            string classification,
            string quantityName,
            double quantity,
            string unit,
            string geometryReference)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            Guid = QsModelElementSnapshot.Require(guid, "guid");
            Entity = QsModelElementSnapshot.Require(entity, "entity");
            Storey = QsModelElementSnapshot.Optional(storey);
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            QuantityName = QsModelElementSnapshot.Require(quantityName, "quantityName");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            GeometryReference = QsModelElementSnapshot.Require(geometryReference, "geometryReference");
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public string Guid { get; private set; }
        public string Entity { get; private set; }
        public string Storey { get; private set; }
        public string Classification { get; private set; }
        public string QuantityName { get; private set; }
        public double Quantity { get; private set; }
        public string Unit { get; private set; }
        public string GeometryReference { get; private set; }
    }

    /// <summary>
    /// Host-neutral evidence layer for the standalone QuantBIM workbench.
    /// It deliberately reuses the canonical IFC DTOs from QsCubicostQuantBim.cs
    /// and adds fail-closed identity/evidence validation before quantities are
    /// allowed to become BOQ inventory.
    /// </summary>
    public sealed class QuantBimTraceableTakeoffEngine
    {
        public IReadOnlyList<QuantBimEvidenceLine> Build(IfcStandaloneDocument document, IfcSelectionSet selection)
        {
            if (document == null) throw new ArgumentNullException("document");
            if (selection == null) throw new ArgumentNullException("selection");

            var elements = new Dictionary<string, IfcStandaloneElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in document.Elements)
            {
                if (element == null) throw new InvalidOperationException("IFC document contains a null element.");
                if (elements.ContainsKey(element.Guid)) throw new InvalidOperationException("Duplicate IFC element GUID: " + element.Guid + ".");
                elements.Add(element.Guid, element);
            }

            foreach (var guid in selection.Guids)
            {
                if (!elements.ContainsKey(guid)) throw new InvalidOperationException("Selection references an IFC element that is not present in the open document: " + guid + ".");
            }

            var result = new List<QuantBimEvidenceLine>();
            foreach (var guid in selection.Guids.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var element = elements[guid];
                if (element.GeometryReference.Length == 0) throw new InvalidOperationException("Selected IFC element has no geometry evidence reference: " + element.Guid + ".");

                foreach (var item in element.Quantities)
                {
                    if (item == null) throw new InvalidOperationException("IFC quantity collection contains a null item for element " + element.Guid + ".");
                    if (!string.Equals(item.Guid, element.Guid, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("IFC quantity GUID does not match its owning element: " + item.Guid + ".");
                    if (!string.Equals(item.Entity, element.Entity, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("IFC quantity entity does not match its owning element: " + element.Guid + ".");
                    if (item.Storey.Length > 0 && element.Storey.Length > 0 && !string.Equals(item.Storey, element.Storey, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("IFC quantity storey does not match its owning element: " + element.Guid + ".");

                    var classification = item.Classification.Length > 0 ? item.Classification : element.Classification;
                    if (classification.Length == 0) throw new InvalidOperationException("Selected IFC quantity has no classification: " + element.Guid + ".");
                    if (item.Classification.Length > 0 && element.Classification.Length > 0 && !string.Equals(item.Classification, element.Classification, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("IFC quantity classification does not match its owning element: " + element.Guid + ".");

                    result.Add(new QuantBimEvidenceLine(
                        document.Path,
                        document.Revision,
                        element.Guid,
                        element.Entity,
                        element.Storey,
                        classification,
                        item.QuantityName,
                        item.Quantity,
                        item.Unit,
                        element.GeometryReference));
                }
            }

            return new ReadOnlyCollection<QuantBimEvidenceLine>(result
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }

        public IReadOnlyList<TakeoffInventoryLine> BuildBoq(IEnumerable<QuantBimEvidenceLine> evidence)
        {
            if (evidence == null) throw new ArgumentNullException("evidence");
            var lines = evidence.ToList();
            if (lines.Any(x => x == null)) throw new ArgumentException("Evidence collection contains null.", "evidence");

            return new ReadOnlyCollection<TakeoffInventoryLine>(lines
                .GroupBy(x => new { x.Classification, x.Unit })
                .Select(g => new TakeoffInventoryLine(g.Key.Classification, g.Key.Unit, g.Sum(x => x.Quantity), g.Select(x => x.Guid).Distinct(StringComparer.OrdinalIgnoreCase).Count()))
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }

        public string ExportEvidenceCsv(IEnumerable<QuantBimEvidenceLine> evidence)
        {
            if (evidence == null) throw new ArgumentNullException("evidence");
            var builder = new StringBuilder("DocumentPath,Revision,Guid,Entity,Storey,Classification,QuantityName,Quantity,Unit,GeometryReference\r\n");
            foreach (var line in evidence.OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Guid, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.QuantityName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase))
            {
                if (line == null) throw new ArgumentException("Evidence collection contains null.", "evidence");
                builder.Append(Csv(line.DocumentPath)).Append(',')
                    .Append(Csv(line.Revision)).Append(',')
                    .Append(Csv(line.Guid)).Append(',')
                    .Append(Csv(line.Entity)).Append(',')
                    .Append(Csv(line.Storey)).Append(',')
                    .Append(Csv(line.Classification)).Append(',')
                    .Append(Csv(line.QuantityName)).Append(',')
                    .Append(line.Quantity.ToString("R", CultureInfo.InvariantCulture)).Append(',')
                    .Append(Csv(line.Unit)).Append(',')
                    .Append(Csv(line.GeometryReference)).Append("\r\n");
            }
            return builder.ToString();
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0 ? value : "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
