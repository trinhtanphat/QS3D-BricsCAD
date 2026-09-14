using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class CubicostDomainPublication
    {
        public CubicostDomainPublication(
            IEnumerable<CubicostDownstreamLine> lines,
            IEnumerable<TakeoffInventoryLine> inventory,
            IEnumerable<CubicostCommercialHandoff> commercialHandoffs)
        {
            Lines = Snapshot(lines, "lines");
            Inventory = Snapshot(inventory, "inventory");
            CommercialHandoffs = Snapshot(commercialHandoffs, "commercialHandoffs");
        }

        public IReadOnlyList<CubicostDownstreamLine> Lines { get; private set; }
        public IReadOnlyList<TakeoffInventoryLine> Inventory { get; private set; }
        public IReadOnlyList<CubicostCommercialHandoff> CommercialHandoffs { get; private set; }

        private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string name)
        {
            if (source == null) throw new ArgumentNullException(name);
            var snapshot = source.ToList();
            if (snapshot.Any(x => ReferenceEquals(x, null))) throw new ArgumentException("Cubicost domain publication contains null values.", name);
            return new ReadOnlyCollection<T>(snapshot);
        }
    }

    public sealed class CubicostConcreteFormworkPublicationWorkflow
    {
        private readonly CubicostConcreteDomainWorkflow _concrete = new CubicostConcreteDomainWorkflow();
        private readonly CubicostFormworkDomainWorkflow _formwork = new CubicostFormworkDomainWorkflow();
        private readonly CubicostReviewedQuantityDownstreamBridge _downstream = new CubicostReviewedQuantityDownstreamBridge();

        public CubicostDomainPublication Publish(
            CubicostDomainQuantityBundle bundle,
            IEnumerable<CubicostDownstreamBinding> bindings)
        {
            if (bundle == null) throw new ArgumentNullException("bundle");
            if (bindings == null) throw new ArgumentNullException("bindings");

            var concrete = _concrete.RequireReviewed(bundle.Concrete).ToList();
            var formwork = _formwork.RequireReviewed(bundle.Formwork).ToList();
            var bindingMap = BuildBindingMap(bindings);
            var formworkById = formwork.ToDictionary(x => x.ComponentId, StringComparer.OrdinalIgnoreCase);

            if (concrete.Count != formwork.Count)
                throw new InvalidOperationException("Concrete/Formwork domain publication cardinality mismatch.");

            var lines = new List<CubicostDownstreamLine>(concrete.Count * 2);
            foreach (var concreteRow in concrete)
            {
                CubicostDomainQuantityRow formworkRow;
                if (!formworkById.TryGetValue(concreteRow.ComponentId, out formworkRow))
                    throw new InvalidOperationException("Missing Formwork peer for Cubicost component " + concreteRow.ComponentId + ".");

                Reconcile(concreteRow, formworkRow);

                CubicostDownstreamBinding binding;
                if (!bindingMap.TryGetValue(concreteRow.Classification, out binding))
                    throw new InvalidOperationException("Missing Cubicost downstream classification/formula binding: " + concreteRow.Classification + ".");

                lines.Add(new CubicostDownstreamLine(
                    concreteRow.ComponentId,
                    concreteRow.Classification,
                    concreteRow.Classification + ".CONCRETE",
                    binding.ConcreteFormulaId,
                    concreteRow.Unit,
                    concreteRow.Quantity,
                    concreteRow.ReviewStatus,
                    concreteRow.Evidence));

                lines.Add(new CubicostDownstreamLine(
                    formworkRow.ComponentId,
                    formworkRow.Classification,
                    formworkRow.Classification + ".FORMWORK",
                    binding.FormworkFormulaId,
                    formworkRow.Unit,
                    formworkRow.Quantity,
                    formworkRow.ReviewStatus,
                    formworkRow.Evidence));
            }

            var ordered = new ReadOnlyCollection<CubicostDownstreamLine>(lines
                .OrderBy(x => x.InventoryClassification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.InventoryClassification, StringComparer.Ordinal)
                .ThenBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ComponentId, StringComparer.Ordinal)
                .ToList());

            var inventory = _downstream.BuildInventory(ordered);
            var handoffs = _downstream.BuildCommercialHandoffs(ordered);
            return new CubicostDomainPublication(ordered, inventory, handoffs);
        }

        private static Dictionary<string, CubicostDownstreamBinding> BuildBindingMap(IEnumerable<CubicostDownstreamBinding> bindings)
        {
            var result = new Dictionary<string, CubicostDownstreamBinding>(StringComparer.OrdinalIgnoreCase);
            foreach (var binding in bindings)
            {
                if (binding == null) throw new ArgumentException("Binding collection contains null.", "bindings");
                if (result.ContainsKey(binding.Classification))
                    throw new InvalidOperationException("Duplicate Cubicost downstream binding: " + binding.Classification + ".");
                result.Add(binding.Classification, binding);
            }
            return result;
        }

        private static void Reconcile(CubicostDomainQuantityRow concrete, CubicostDomainQuantityRow formwork)
        {
            if (concrete.Domain != CubicostQuantityDomain.Concrete || formwork.Domain != CubicostQuantityDomain.Formwork)
                throw new InvalidOperationException("Cubicost domain peer types are invalid for " + concrete.ComponentId + ".");
            if (!string.Equals(concrete.ComponentId, formwork.ComponentId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Concrete/Formwork component identity mismatch.");
            if (!string.Equals(concrete.Classification, formwork.Classification, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Concrete/Formwork classification mismatch for " + concrete.ComponentId + ".");
            if (!string.Equals(concrete.Storey, formwork.Storey, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Concrete/Formwork storey mismatch for " + concrete.ComponentId + ".");
            if (concrete.ReviewStatus != formwork.ReviewStatus)
                throw new InvalidOperationException("Concrete/Formwork review-state mismatch for " + concrete.ComponentId + ".");
            if (!ReferenceEquals(concrete.Evidence, formwork.Evidence))
                throw new InvalidOperationException("Concrete/Formwork evidence generation mismatch for " + concrete.ComponentId + ".");
            if (!string.Equals(concrete.Unit, "m3", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Concrete domain must publish cubic metres for " + concrete.ComponentId + ".");
            if (!string.Equals(formwork.Unit, "m2", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Formwork domain must publish square metres for " + concrete.ComponentId + ".");
            if (double.IsNaN(concrete.Quantity) || double.IsInfinity(concrete.Quantity) || concrete.Quantity < 0d)
                throw new InvalidOperationException("Invalid Concrete quantity for " + concrete.ComponentId + ".");
            if (double.IsNaN(formwork.Quantity) || double.IsInfinity(formwork.Quantity) || formwork.Quantity < 0d)
                throw new InvalidOperationException("Invalid Formwork quantity for " + concrete.ComponentId + ".");
        }
    }
}
