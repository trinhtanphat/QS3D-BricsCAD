using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum CubicostQuantityDomain
    {
        Concrete,
        Formwork
    }

    public sealed class CubicostDomainQuantityRow
    {
        public CubicostDomainQuantityRow(
            CubicostQuantityDomain domain,
            string componentId,
            string classification,
            string storey,
            string unit,
            double quantity,
            ComponentRecognitionStatus reviewStatus,
            QuantityEvidence evidence)
        {
            Domain = domain;
            ComponentId = QsModelElementSnapshot.Require(componentId, "componentId");
            Classification = QsModelElementSnapshot.Require(classification, "classification");
            Storey = QsModelElementSnapshot.Optional(storey);
            Unit = QsModelElementSnapshot.Require(unit, "unit");
            Quantity = QsModelElementSnapshot.Finite(quantity, "quantity");
            if (Quantity < 0d) throw new ArgumentOutOfRangeException("quantity");
            ReviewStatus = reviewStatus;
            Evidence = evidence ?? throw new ArgumentNullException("evidence");
        }

        public CubicostQuantityDomain Domain { get; private set; }
        public string ComponentId { get; private set; }
        public string Classification { get; private set; }
        public string Storey { get; private set; }
        public string Unit { get; private set; }
        public double Quantity { get; private set; }
        public ComponentRecognitionStatus ReviewStatus { get; private set; }
        public QuantityEvidence Evidence { get; private set; }
    }

    public sealed class CubicostDomainQuantityBundle
    {
        public CubicostDomainQuantityBundle(
            IEnumerable<CubicostQuantityLine> combined,
            IEnumerable<CubicostDomainQuantityRow> concrete,
            IEnumerable<CubicostDomainQuantityRow> formwork)
        {
            Combined = Snapshot(combined, "combined");
            Concrete = Snapshot(concrete, "concrete");
            Formwork = Snapshot(formwork, "formwork");
        }

        public IReadOnlyList<CubicostQuantityLine> Combined { get; private set; }
        public IReadOnlyList<CubicostDomainQuantityRow> Concrete { get; private set; }
        public IReadOnlyList<CubicostDomainQuantityRow> Formwork { get; private set; }

        private static IReadOnlyList<T> Snapshot<T>(IEnumerable<T> source, string name)
        {
            if (source == null) throw new ArgumentNullException(name);
            var snapshot = source.ToList();
            if (snapshot.Any(x => ReferenceEquals(x, null))) throw new ArgumentException("Cubicost domain bundle contains null values.", name);
            return new ReadOnlyCollection<T>(snapshot);
        }
    }

    public sealed class CubicostConcreteDomainWorkflow
    {
        public IReadOnlyList<CubicostDomainQuantityRow> Project(IEnumerable<CubicostQuantityLine> lines)
        {
            return CubicostDomainProjector.Project(lines, CubicostQuantityDomain.Concrete);
        }

        public IReadOnlyList<CubicostDomainQuantityRow> RequireReviewed(IEnumerable<CubicostDomainQuantityRow> rows)
        {
            return CubicostDomainProjector.RequireReviewed(rows, CubicostQuantityDomain.Concrete);
        }
    }

    public sealed class CubicostFormworkDomainWorkflow
    {
        public IReadOnlyList<CubicostDomainQuantityRow> Project(IEnumerable<CubicostQuantityLine> lines)
        {
            return CubicostDomainProjector.Project(lines, CubicostQuantityDomain.Formwork);
        }

        public IReadOnlyList<CubicostDomainQuantityRow> RequireReviewed(IEnumerable<CubicostDomainQuantityRow> rows)
        {
            return CubicostDomainProjector.RequireReviewed(rows, CubicostQuantityDomain.Formwork);
        }
    }

    public sealed class CubicostConcreteFormworkDomainOrchestrator
    {
        private readonly CubicostConcreteFormworkWorkflow _quantityWorkflow = new CubicostConcreteFormworkWorkflow();
        private readonly CubicostConcreteDomainWorkflow _concrete = new CubicostConcreteDomainWorkflow();
        private readonly CubicostFormworkDomainWorkflow _formwork = new CubicostFormworkDomainWorkflow();

        public CubicostDomainQuantityBundle Quantify(
            IEnumerable<RecognizedQsComponent> components,
            IEnumerable<ComponentReviewDecision> reviews,
            bool includeEnds)
        {
            var combined = _quantityWorkflow.Quantify(components, reviews, includeEnds);
            return new CubicostDomainQuantityBundle(combined, _concrete.Project(combined), _formwork.Project(combined));
        }
    }

    internal static class CubicostDomainProjector
    {
        internal static IReadOnlyList<CubicostDomainQuantityRow> Project(IEnumerable<CubicostQuantityLine> lines, CubicostQuantityDomain domain)
        {
            if (lines == null) throw new ArgumentNullException("lines");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rows = new List<CubicostDomainQuantityRow>();
            foreach (var line in lines)
            {
                if (line == null) throw new ArgumentException("Cubicost quantity collection contains null.", "lines");
                if (!ids.Add(line.ComponentId)) throw new InvalidOperationException("Duplicate Cubicost component id in domain projection: " + line.ComponentId + ".");
                var concrete = domain == CubicostQuantityDomain.Concrete;
                rows.Add(new CubicostDomainQuantityRow(
                    domain,
                    line.ComponentId,
                    line.Classification,
                    line.Storey,
                    concrete ? "m3" : "m2",
                    concrete ? line.ConcreteVolume : line.FormworkArea,
                    line.ReviewStatus,
                    line.Evidence));
            }

            return new ReadOnlyCollection<CubicostDomainQuantityRow>(rows
                .OrderBy(x => x.Storey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Storey, StringComparer.Ordinal)
                .ThenBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Classification, StringComparer.Ordinal)
                .ThenBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ComponentId, StringComparer.Ordinal)
                .ToList());
        }

        internal static IReadOnlyList<CubicostDomainQuantityRow> RequireReviewed(IEnumerable<CubicostDomainQuantityRow> rows, CubicostQuantityDomain domain)
        {
            if (rows == null) throw new ArgumentNullException("rows");
            var snapshot = rows.ToList();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in snapshot)
            {
                if (row == null) throw new ArgumentException("Cubicost domain row collection contains null.", "rows");
                if (row.Domain != domain) throw new InvalidOperationException("Cubicost domain row was published through the wrong workflow: " + row.ComponentId + ".");
                if (!ids.Add(row.ComponentId)) throw new InvalidOperationException("Duplicate Cubicost component id in reviewed domain publication: " + row.ComponentId + ".");
                if (row.ReviewStatus != ComponentRecognitionStatus.Accepted && row.ReviewStatus != ComponentRecognitionStatus.Corrected)
                    throw new InvalidOperationException("Only accepted/corrected Cubicost " + domain.ToString().ToLowerInvariant() + " quantities may be published: " + row.ComponentId + ".");
            }

            return new ReadOnlyCollection<CubicostDomainQuantityRow>(snapshot
                .OrderBy(x => x.Storey, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Storey, StringComparer.Ordinal)
                .ThenBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Classification, StringComparer.Ordinal)
                .ThenBy(x => x.ComponentId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.ComponentId, StringComparer.Ordinal)
                .ToList());
        }
    }
}
