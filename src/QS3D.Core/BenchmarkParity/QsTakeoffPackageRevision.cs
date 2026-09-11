using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class TakeoffPackageRevisionDelta
    {
        public TakeoffPackageRevisionDelta(
            string sheetId,
            string previousRevision,
            string currentRevision,
            IEnumerable<RevisionMarkupDelta2D> markupDeltas)
        {
            SheetId = QsModelElementSnapshot.Require(sheetId, "sheetId");
            PreviousRevision = QsModelElementSnapshot.Require(previousRevision, "previousRevision");
            CurrentRevision = QsModelElementSnapshot.Require(currentRevision, "currentRevision");
            MarkupDeltas = new ReadOnlyCollection<RevisionMarkupDelta2D>((markupDeltas ?? throw new ArgumentNullException("markupDeltas")).ToList());
        }

        public string SheetId { get; private set; }
        public string PreviousRevision { get; private set; }
        public string CurrentRevision { get; private set; }
        public IReadOnlyList<RevisionMarkupDelta2D> MarkupDeltas { get; private set; }
        public int AddedCount { get { return MarkupDeltas.Count(x => x.Kind == RevisionMarkupChangeKind.Added); } }
        public int RemovedCount { get { return MarkupDeltas.Count(x => x.Kind == RevisionMarkupChangeKind.Removed); } }
        public int ChangedCount { get { return MarkupDeltas.Count(x => x.Kind == RevisionMarkupChangeKind.Changed); } }
        public int UnchangedCount { get { return MarkupDeltas.Count(x => x.Kind == RevisionMarkupChangeKind.Unchanged); } }
        public double QuantityDelta { get { return MarkupDeltas.Sum(x => x.QuantityDelta); } }
        public bool HasMaterialChange { get { return AddedCount > 0 || RemovedCount > 0 || ChangedCount > 0; } }
    }

    public sealed class TakeoffPackageRevisionComparison
    {
        public TakeoffPackageRevisionComparison(
            string packageId,
            string previousRevision,
            string currentRevision,
            IEnumerable<TakeoffPackageRevisionDelta> sheetDeltas)
        {
            PackageId = QsModelElementSnapshot.Require(packageId, "packageId");
            PreviousRevision = QsModelElementSnapshot.Require(previousRevision, "previousRevision");
            CurrentRevision = QsModelElementSnapshot.Require(currentRevision, "currentRevision");
            SheetDeltas = new ReadOnlyCollection<TakeoffPackageRevisionDelta>((sheetDeltas ?? throw new ArgumentNullException("sheetDeltas")).ToList());
        }

        public string PackageId { get; private set; }
        public string PreviousRevision { get; private set; }
        public string CurrentRevision { get; private set; }
        public IReadOnlyList<TakeoffPackageRevisionDelta> SheetDeltas { get; private set; }
        public int ChangedSheetCount { get { return SheetDeltas.Count(x => x.HasMaterialChange); } }
        public int AddedMarkupCount { get { return SheetDeltas.Sum(x => x.AddedCount); } }
        public int RemovedMarkupCount { get { return SheetDeltas.Sum(x => x.RemovedCount); } }
        public int ChangedMarkupCount { get { return SheetDeltas.Sum(x => x.ChangedCount); } }
        public double QuantityDelta { get { return SheetDeltas.Sum(x => x.QuantityDelta); } }
        public bool RequiresReview { get { return ChangedSheetCount > 0; } }
    }

    public sealed class AutodeskTakeoffPackageRevisionComparer
    {
        public TakeoffPackageRevisionComparison Compare(
            TakeoffPackageDefinition previousPackage,
            IEnumerable<TakeoffSheetResult2D> previousSheets,
            TakeoffPackageDefinition currentPackage,
            IEnumerable<TakeoffSheetResult2D> currentSheets)
        {
            if (previousPackage == null) throw new ArgumentNullException("previousPackage");
            if (currentPackage == null) throw new ArgumentNullException("currentPackage");
            if (previousSheets == null) throw new ArgumentNullException("previousSheets");
            if (currentSheets == null) throw new ArgumentNullException("currentSheets");
            if (!string.Equals(previousPackage.Id, currentPackage.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Package revision compare requires the same logical package id.");

            var oldSheets = previousSheets.ToList();
            var newSheets = currentSheets.ToList();
            if (oldSheets.Any(x => x == null) || newSheets.Any(x => x == null))
                throw new ArgumentException("Package revision comparison cannot contain null sheet results.");

            var oldById = oldSheets.ToDictionary(x => x.Sheet.Id, StringComparer.OrdinalIgnoreCase);
            var newById = newSheets.ToDictionary(x => x.Sheet.Id, StringComparer.OrdinalIgnoreCase);
            var allIds = oldById.Keys.Union(newById.Keys, StringComparer.OrdinalIgnoreCase).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
            var deltas = new List<TakeoffPackageRevisionDelta>();
            var comparer = new DrawingRevisionComparer2D();

            foreach (var sheetId in allIds)
            {
                TakeoffSheetResult2D oldSheet;
                TakeoffSheetResult2D newSheet;
                var hasOld = oldById.TryGetValue(sheetId, out oldSheet!);
                var hasNew = newById.TryGetValue(sheetId, out newSheet!);

                if (hasOld && hasNew)
                {
                    deltas.Add(new TakeoffPackageRevisionDelta(sheetId, oldSheet.Sheet.Revision, newSheet.Sheet.Revision, comparer.Compare(oldSheet, newSheet)));
                    continue;
                }

                var synthetic = new List<RevisionMarkupDelta2D>();
                if (hasNew)
                {
                    synthetic.AddRange(newSheet.Evidence.Select(x => new RevisionMarkupDelta2D(x.MarkupId, RevisionMarkupChangeKind.Added, null, x)));
                    deltas.Add(new TakeoffPackageRevisionDelta(sheetId, previousPackage.Revision, newSheet.Sheet.Revision, synthetic));
                }
                else
                {
                    synthetic.AddRange(oldSheet.Evidence.Select(x => new RevisionMarkupDelta2D(x.MarkupId, RevisionMarkupChangeKind.Removed, x, null)));
                    deltas.Add(new TakeoffPackageRevisionDelta(sheetId, oldSheet.Sheet.Revision, currentPackage.Revision, synthetic));
                }
            }

            return new TakeoffPackageRevisionComparison(previousPackage.Id, previousPackage.Revision, currentPackage.Revision, deltas);
        }
    }
}
