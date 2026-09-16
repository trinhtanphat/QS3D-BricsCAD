using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Package-first integration facade for Autodesk Takeoff parity. Measurement,
    /// revision and estimate calculations remain delegated to the existing authorities.
    /// </summary>
    public sealed class AutodeskTakeoffPackageMember
    {
        public AutodeskTakeoffPackageMember(IngestedDrawingSheet2D sheet, TakeoffSheetResult2D takeoff)
        {
            Sheet = sheet ?? throw new ArgumentNullException("sheet");
            Takeoff = takeoff ?? throw new ArgumentNullException("takeoff");
            if (!string.Equals(sheet.Sheet.Id, takeoff.Sheet.Id, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(sheet.Sheet.Revision, takeoff.Sheet.Revision, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(sheet.Sheet.SourceReference, takeoff.Sheet.SourceReference, StringComparison.Ordinal))
                throw new InvalidOperationException("Package member ingestion and takeoff evidence must identify the same sheet revision and source.");
        }

        public IngestedDrawingSheet2D Sheet { get; private set; }
        public TakeoffSheetResult2D Takeoff { get; private set; }
    }

    public sealed class AutodeskTakeoffPackageSnapshot
    {
        internal AutodeskTakeoffPackageSnapshot(string id, string generation, IReadOnlyList<AutodeskTakeoffPackageMember> members, IReadOnlyList<TakeoffWorkflowLine> inventory)
        {
            Id = QsModelElementSnapshot.Require(id, "id");
            Generation = QsModelElementSnapshot.Require(generation, "generation");
            Members = members;
            Inventory = inventory;
        }

        public string Id { get; private set; }
        public string Generation { get; private set; }
        public IReadOnlyList<AutodeskTakeoffPackageMember> Members { get; private set; }
        public IReadOnlyList<TakeoffWorkflowLine> Inventory { get; private set; }
    }

    public sealed class AutodeskTakeoffPackageWorkflow
    {
        private readonly AutodeskTakeoffWorkflow workflow = new AutodeskTakeoffWorkflow();

        public AutodeskTakeoffPackageSnapshot Build(
            string packageId,
            string generation,
            IEnumerable<AutodeskTakeoffPackageMember> members,
            IEnumerable<IfcQtoItem> bimQuantities,
            Func<string, double, double> formula,
            Func<string, string, double> rateProvider)
        {
            QsModelElementSnapshot.Require(packageId, "packageId");
            QsModelElementSnapshot.Require(generation, "generation");
            if (members == null) throw new ArgumentNullException("members");
            if (bimQuantities == null) throw new ArgumentNullException("bimQuantities");

            var ordered = members.ToList();
            if (ordered.Any(x => x == null)) throw new ArgumentException("Package member collection contains null.", "members");
            var duplicate = ordered.GroupBy(x => x.Sheet.Sheet.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null) throw new InvalidOperationException("A package cannot contain multiple current revisions for the same logical sheet id.");
            ordered = ordered.OrderBy(x => x.Sheet.Sheet.Id, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Sheet.Sheet.Revision, StringComparer.OrdinalIgnoreCase).ToList();

            var evidence = ordered.SelectMany(x => x.Takeoff.Evidence).ToList();
            var inventory = workflow.BuildInventoryAndEstimate(evidence, bimQuantities, formula, rateProvider);
            return new AutodeskTakeoffPackageSnapshot(packageId, generation,
                new ReadOnlyCollection<AutodeskTakeoffPackageMember>(ordered),
                new ReadOnlyCollection<TakeoffWorkflowLine>(inventory.ToList()));
        }

        public void RequireCurrentGeneration(AutodeskTakeoffPackageSnapshot snapshot, string generation)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            var current = QsModelElementSnapshot.Require(generation, "generation");
            if (!string.Equals(snapshot.Generation, current, StringComparison.Ordinal))
                throw new InvalidOperationException("Takeoff package snapshot is stale and must be regenerated before estimate publication.");
        }

        public RevisionTakeoffPackage2D CompareRevision(TakeoffSheetResult2D previous, TakeoffSheetResult2D current)
        {
            return new RevisionTakeoffPackage2D(previous, current);
        }
    }
}