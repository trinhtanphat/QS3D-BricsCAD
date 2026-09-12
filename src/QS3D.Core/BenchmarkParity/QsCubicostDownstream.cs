using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class CubicostDownstreamBinding
    {
        public CubicostDownstreamBinding(string classification, string concreteFormulaId, string formworkFormulaId)
        {
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            ConcreteFormulaId = QsModelElementSnapshot.Require(concreteFormulaId, "concreteFormulaId");
            FormworkFormulaId = QsModelElementSnapshot.Require(formworkFormulaId, "formworkFormulaId");
        }

        public string Classification { get; private set; }
        public string ConcreteFormulaId { get; private set; }
        public string FormworkFormulaId { get; private set; }
    }

    public sealed class CubicostDownstreamLine
    {
        public CubicostDownstreamLine(string componentId, string sourceClassification, string inventoryClassification, string formulaId, string unit, double quantity, ComponentRecognitionStatus reviewStatus, QuantityEvidence evidence)
        {
            ComponentId = QsModelElementSnapshot.Require(componentId, "componentId");
            SourceClassification = QsModelElementSnapshot.Require(sourceClassification, "sourceClassification");
            InventoryClassification = QsModelElementSnapshot.Require(inventoryClassification, "inventoryClassification");
            FormulaId = QsModelElementSnapshot.Require(formulaId, "formulaId");
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            if (Quantity < 0d) throw new ArgumentOutOfRangeException("quantity");
            ReviewStatus = reviewStatus;
            Evidence = evidence ?? throw new ArgumentNullException("evidence");
        }

        public string ComponentId { get; private set; }
        public string SourceClassification { get; private set; }
        public string InventoryClassification { get; private set; }
        public string FormulaId { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
        public ComponentRecognitionStatus ReviewStatus { get; private set; }
        public QuantityEvidence Evidence { get; private set; }
    }

    public sealed class CubicostCommercialHandoff
    {
        public CubicostCommercialHandoff(string destination, CubicostDownstreamLine line)
        {
            Destination = QsModelElementSnapshot.Require(destination, "destination");
            Line = line ?? throw new ArgumentNullException("line");
        }

        public string Destination { get; private set; }
        public CubicostDownstreamLine Line { get; private set; }
    }

    public sealed class CubicostReviewedQuantityDownstreamBridge
    {
        public IReadOnlyList<CubicostDownstreamLine> Admit(IEnumerable<CubicostQuantityLine> lines, IEnumerable<CubicostDownstreamBinding> bindings, bool allowProposed)
        {
            if (lines == null) throw new ArgumentNullException("lines");
            if (bindings == null) throw new ArgumentNullException("bindings");
            var bindingMap = new Dictionary<string, CubicostDownstreamBinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var binding in bindings)
            {
                if (binding == null) throw new ArgumentException("Binding collection contains null.", "bindings");
                if (bindingMap.ContainsKey(binding.Classification)) throw new InvalidOperationException("Duplicate Cubicost downstream binding: " + binding.Classification + ".");
                bindingMap.Add(binding.Classification, binding);
            }

            var componentIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var admitted = new List<CubicostDownstreamLine>();
            foreach (var line in lines)
            {
                if (line == null) throw new ArgumentException("Quantity collection contains null.", "lines");
                if (!componentIds.Add(line.ComponentId)) throw new InvalidOperationException("Duplicate Cubicost component id at downstream handoff: " + line.ComponentId + ".");
                if (line.ReviewStatus == ComponentRecognitionStatus.Rejected) continue;
                if (line.ReviewStatus == ComponentRecognitionStatus.Proposed && !allowProposed) throw new InvalidOperationException("Unreviewed Cubicost quantity cannot enter the commercial handoff: " + line.ComponentId + ".");

                CubicostDownstreamBinding binding;
                if (!bindingMap.TryGetValue(line.Classification, out binding)) throw new InvalidOperationException("Missing Cubicost downstream classification/formula binding: " + line.Classification + ".");
                ValidateQuantity(line.ConcreteVolume, line.ComponentId, "concrete");
                ValidateQuantity(line.FormworkArea, line.ComponentId, "formwork");
                admitted.Add(new CubicostDownstreamLine(line.ComponentId, line.Classification, line.Classification + ".CONCRETE", binding.ConcreteFormulaId, "m3", line.ConcreteVolume, line.ReviewStatus, line.Evidence));
                admitted.Add(new CubicostDownstreamLine(line.ComponentId, line.Classification, line.Classification + ".FORMWORK", binding.FormworkFormulaId, "m2", line.FormworkArea, line.ReviewStatus, line.Evidence));
            }

            return new ReadOnlyCollection<CubicostDownstreamLine>(admitted.OrderBy(x => x.InventoryClassification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public IReadOnlyList<TakeoffInventoryLine> BuildInventory(IEnumerable<CubicostDownstreamLine> admitted)
        {
            if (admitted == null) throw new ArgumentNullException("admitted");
            var snapshot = admitted.ToList();
            ValidateAdmitted(snapshot);
            return new ReadOnlyCollection<TakeoffInventoryLine>(snapshot
                .GroupBy(x => x.InventoryClassification + "\u001f" + x.Unit, StringComparer.OrdinalIgnoreCase)
                .Select(g => new TakeoffInventoryLine(g.First().InventoryClassification, g.First().Unit, g.Sum(x => x.Quantity), g.Select(x => x.ComponentId).Distinct(StringComparer.OrdinalIgnoreCase).Count()))
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public IReadOnlyList<CubicostCommercialHandoff> BuildCommercialHandoffs(IEnumerable<CubicostDownstreamLine> admitted)
        {
            if (admitted == null) throw new ArgumentNullException("admitted");
            var snapshot = admitted.ToList();
            ValidateAdmitted(snapshot);
            var destinations = new[] { "Estimate", "Tender", "Procurement" };
            return new ReadOnlyCollection<CubicostCommercialHandoff>(destinations.SelectMany(destination => snapshot.Select(line => new CubicostCommercialHandoff(destination, line))).OrderBy(x => x.Destination, StringComparer.Ordinal).ThenBy(x => x.Line.InventoryClassification, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Line.ComponentId, StringComparer.OrdinalIgnoreCase).ToList());
        }

        private static void ValidateAdmitted(IEnumerable<CubicostDownstreamLine> lines)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in lines)
            {
                if (line == null) throw new ArgumentException("Downstream collection contains null.", "lines");
                if (line.ReviewStatus != ComponentRecognitionStatus.Accepted && line.ReviewStatus != ComponentRecognitionStatus.Corrected) throw new InvalidOperationException("Only accepted/corrected Cubicost quantities may be published downstream.");
                ValidateQuantity(line.Quantity, line.ComponentId, line.Unit);
                var key = line.ComponentId + "\u001f" + line.InventoryClassification + "\u001f" + line.Unit;
                if (!keys.Add(key)) throw new InvalidOperationException("Duplicate Cubicost downstream line: " + line.ComponentId + "/" + line.InventoryClassification + ".");
            }
        }

        private static void ValidateQuantity(double quantity, string componentId, string kind)
        {
            if (double.IsNaN(quantity) || double.IsInfinity(quantity) || quantity < 0d) throw new InvalidOperationException("Invalid " + kind + " quantity for Cubicost component " + componentId + ".");
        }
    }
}
