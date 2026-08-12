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
            DelimiterPositionsRemainDistinctAcrossSnapshots();
            CaseInsensitiveIdentitySemanticsRemainStable();
        }

        private static void DelimiterPositionsRemainDistinctAcrossSnapshots()
        {
            var baseline = Snapshot(Entry("A", "B\u001fC"));
            var candidate = Snapshot(Entry("A\u001fB", "C"));
            var verifier = new PreviewReviewSnapshotService();
            True(verifier.Verify(baseline), "Baseline collision fixture must be a verified snapshot.");
            True(verifier.Verify(candidate), "Candidate collision fixture must be a verified snapshot.");

            var comparison = new PreviewReviewSnapshotComparisonService().Compare(baseline, candidate);
            Equal(2, comparison.Rows.Count);
            Equal(1, comparison.RemovedCount);
            Equal(1, comparison.AddedCount);
            Equal(0, comparison.ChangedCount);
            Equal(0, comparison.UnchangedCount);
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
            var ctor = typeof(PreviewReviewSnapshot)
                .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                .Single();
            var args = new object[]
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
                new string('0', 64)
            };
            var draft = (PreviewReviewSnapshot)ctor.Invoke(args);
            var compute = typeof(PreviewReviewSnapshotService).GetMethod(
                "ComputeFingerprint",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (compute == null)
                throw new InvalidOperationException("PreviewReviewSnapshotService.ComputeFingerprint was not found for comparison smoke coverage.");
            var fingerprint = compute.Invoke(null, new object[] { draft }) as string;
            if (string.IsNullOrWhiteSpace(fingerprint))
                throw new InvalidOperationException("Preview review comparison smoke failed to compute a snapshot fingerprint.");
            args[13] = fingerprint;
            return (PreviewReviewSnapshot)ctor.Invoke(args);
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
