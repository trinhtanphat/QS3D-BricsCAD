using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Compatibility-safe package coordinator for releases whose drawing sheets carry
    /// independent drawing revisions. The legacy AutodeskTakeoffPackageCoordinator.Build
    /// contract remains strict; this adapter validates evidence against each source sheet,
    /// then delegates the normalized package through the canonical package workflow.
    /// </summary>
    public sealed class AutodeskTakeoffPackagePerSheetRevisionCoordinator
    {
        private readonly AutodeskTakeoffPackageCoordinator inner = new AutodeskTakeoffPackageCoordinator();

        public TakeoffPackageBuildResult Build(
            TakeoffPackageDefinition package,
            IEnumerable<DrawingSheet2D> drawingSheets,
            IEnumerable<TakeoffQuantityEvidence2D> drawingEvidence,
            IEnumerable<IfcQtoItem> bimQuantities,
            Func<string, double, double> formula,
            Func<string, string, double> rateProvider)
        {
            if (package == null) throw new ArgumentNullException("package");
            if (drawingSheets == null) throw new ArgumentNullException("drawingSheets");
            if (drawingEvidence == null) throw new ArgumentNullException("drawingEvidence");
            if (bimQuantities == null) throw new ArgumentNullException("bimQuantities");
            if (formula == null) throw new ArgumentNullException("formula");
            if (rateProvider == null) throw new ArgumentNullException("rateProvider");

            var sheets = drawingSheets.ToList();
            var evidence = drawingEvidence.ToList();
            var uniqueSheets = new Dictionary<string, DrawingSheet2D>(StringComparer.OrdinalIgnoreCase);
            var duplicateSheetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sheet in sheets)
            {
                if (sheet == null) continue;
                if (uniqueSheets.ContainsKey(sheet.Id)) duplicateSheetIds.Add(sheet.Id);
                else uniqueSheets.Add(sheet.Id, sheet);
            }

            var normalizedSheets = sheets.Select(sheet => sheet == null
                ? null!
                : new DrawingSheet2D(
                    sheet.Id,
                    sheet.Name,
                    sheet.SourceKind,
                    sheet.SourceReference,
                    package.Revision,
                    sheet.Calibration)).ToList();

            var normalizedEvidence = evidence.Select(item => NormalizeEvidence(
                item,
                package,
                uniqueSheets,
                duplicateSheetIds)).ToList();

            var result = inner.Build(
                package,
                normalizedSheets,
                normalizedEvidence,
                bimQuantities,
                formula,
                rateProvider);

            var sourceRevisions = uniqueSheets.ToDictionary(x => x.Key, x => x.Value.Revision, StringComparer.OrdinalIgnoreCase);
            var restoredSources = result.Sources.Select(source => RestoreSourceRevision(source, sourceRevisions)).ToList();
            return new TakeoffPackageBuildResult(
                result.Package,
                result.Readiness,
                new ReadOnlyCollection<TakeoffPackageSource>(restoredSources),
                result.Issues,
                result.Inventory);
        }

        private static TakeoffQuantityEvidence2D NormalizeEvidence(
            TakeoffQuantityEvidence2D item,
            TakeoffPackageDefinition package,
            IDictionary<string, DrawingSheet2D> uniqueSheets,
            ISet<string> duplicateSheetIds)
        {
            if (item == null) return null!;

            DrawingSheet2D? sourceSheet;
            if (!duplicateSheetIds.Contains(item.SheetId)
                && uniqueSheets.TryGetValue(item.SheetId, out sourceSheet))
            {
                var revision = string.Equals(item.Revision, sourceSheet.Revision, StringComparison.OrdinalIgnoreCase)
                    ? package.Revision
                    : package.Revision + "\u001fSTALE-SHEET-REVISION";
                return new TakeoffQuantityEvidence2D(
                    item.MarkupId,
                    item.SheetId,
                    revision,
                    item.SourceReference,
                    item.SourceHandle,
                    item.Classification,
                    item.Zone,
                    item.Layer,
                    item.Quantity,
                    item.Unit);
            }

            return item;
        }

        private static TakeoffPackageSource RestoreSourceRevision(
            TakeoffPackageSource source,
            IDictionary<string, string> sourceRevisions)
        {
            string? revision;
            if (source.Kind == TakeoffPackageSourceKind.Drawing2D
                && sourceRevisions.TryGetValue(source.Id, out revision))
            {
                return new TakeoffPackageSource(source.Id, source.Kind, source.SourceReference, revision);
            }
            return source;
        }
    }
}
