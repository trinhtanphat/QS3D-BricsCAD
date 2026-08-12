using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Review;

namespace QS3D.Core.SmokeTests
{
    internal static class PreviewReviewCompositeRowKeySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DelimiterPositionsRemainDistinctInComparisonKey();
            XmlInvalidDelimiterSnapshotsFailVerification();
            CaseInsensitiveIdentitySemanticsRemainStable();
        }

        private static void DelimiterPositionsRemainDistinctInComparisonKey()
        {
            var rowKey = typeof(PreviewReviewSnapshotComparisonService).GetMethod(
                "RowKey",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (rowKey == null)
                throw new InvalidOperationException("PreviewReviewSnapshotComparisonService.RowKey was not found for collision coverage.");

            var first = rowKey.Invoke(null, new object[] { "A", "B\u001fC" }) as string;
            var second = rowKey.Invoke(null, new object[] { "A\u001fB", "C" }) as string;
            if (string.IsNullOrEmpty(first) || string.IsNullOrEmpty(second))
                throw new InvalidOperationException("Preview review comparison row key returned an empty value.");
            if (string.Equals(first, second, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Preview review comparison row key still collides when separator positions differ.");
        }

        private static void XmlInvalidDelimiterSnapshotsFailVerification()
        {
            var snapshot = SnapshotWithoutFingerprint(Entry("A", "B\u001fC"));
            False(
                new PreviewReviewSnapshotService().Verify(snapshot),
                "Preview Review must reject XML-invalid separator text before treating the snapshot as verified.");
        }

        private static void CaseInsensitiveIdentitySemanticsRemainStable()
        {
            var baseline = Snapshot(Entry("Element-A", "Field-X"));
            var candidate = Snapshot(Entry("element-a", "field-x"));

            var comparison = new PreviewReviewSnapshotComparisonService().Compare(baseline, candidate);
            Equal(1, comparison.Rows.Count);
            Equal(0, comparison.AddedCount);
            Equal(0, comparison.RemovedCount);
            Equal(0, comparison.ChangedCount);
            Equal(1, comparison.UnchangedCount);
        }

        private static PreviewReviewEntry Entry(string elementId, string field)
        {
            var ctor = typeof(PreviewReviewEntry)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single();
            return (PreviewReviewEntry)ctor.Invoke(new object[]
            {
                elementId,
                "Wall",
                "Changed",
                field,
                "before",
                "after",
                string.Empty,
                string.Empty
            });
        }

        private static PreviewReviewSnapshot Snapshot(PreviewReviewEntry entry)
        {
            var draft = SnapshotWithoutFingerprint(entry);
            var compute = typeof(PreviewReviewSnapshotService).GetMethod(
                "ComputeFingerprint",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (compute == null)
                throw new InvalidOperationException("PreviewReviewSnapshotService.ComputeFingerprint was not found for comparison smoke coverage.");
            var fingerprint = compute.Invoke(null, new object[] { draft }) as string;
            if (string.IsNullOrWhiteSpace(fingerprint))
                throw new InvalidOperationException("Preview review comparison smoke failed to compute a snapshot fingerprint.");
            return ConstructSnapshot(entry, fingerprint);
        }

        private static PreviewReviewSnapshot SnapshotWithoutFingerprint(PreviewReviewEntry entry) =>
            ConstructSnapshot(entry, new string('0', 64));

        private static PreviewReviewSnapshot ConstructSnapshot(PreviewReviewEntry entry, string fingerprint)
        {
            var ctor = typeof(PreviewReviewSnapshot)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single();
            return (PreviewReviewSnapshot)ctor.Invoke(new object[]
            {
                "comparison-smoke",
                "P-COMPOSITE-ROW-KEY",
                PreviewReviewKind.Regeneration,
                1L,
                "Project",
                Array.Empty<string>(),
                new[] { entry },
                1,
                0,
                0,
                0,
                0,
                0,
                fingerprint
            });
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
