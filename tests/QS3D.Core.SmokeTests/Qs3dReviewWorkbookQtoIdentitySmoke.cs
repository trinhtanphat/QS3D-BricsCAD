using System;
using System.IO;
using System.Linq;
using QS3D.Core.Coordination;
using QS3D.Core.Export;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class Qs3dReviewWorkbookQtoIdentitySmoke
    {
        public static void Run()
        {
            CanonicalUnicodeRoundTrips();
            SurroundingWhitespaceFailsBeforeIo();
            InvalidXmlIdentityPreservesDestination();
            MalformedSurrogatePreservesDestination();
        }

        private static void CanonicalUnicodeRoundTrips()
        {
            var directory = TempDirectory("review-qto-identity-valid");
            var path = Path.Combine(directory, "review.xlsx");
            const string elementId = "QTO-Å-測";
            try
            {
                Export(path, elementId);
                var trace = Qs3dReviewWorkbookTraceReader.Read(
                    path,
                    Qs3dReviewWorkbookExporter.QuantitySheet,
                    2);
                Equal(elementId, trace.ElementIds.Single());
                Equal("A1", trace.Handles.Single());
                Equal("FP-4051", trace.DrawingFingerprint);
                True(trace.TraceKey.StartsWith("QTO:", StringComparison.Ordinal));
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static void SurroundingWhitespaceFailsBeforeIo()
        {
            var root = Path.Combine(Path.GetTempPath(), "qs3d-review-qto-identity-" + Guid.NewGuid().ToString("N"));
            var directory = Path.Combine(root, "nested");
            var path = Path.Combine(directory, "review.xlsx");
            try
            {
                Throws<InvalidDataException>(() => Export(path, " QTO-1 "));
                True(!Directory.Exists(directory));
                True(!File.Exists(path));
            }
            finally
            {
                TryDeleteDirectory(root);
            }
        }

        private static void InvalidXmlIdentityPreservesDestination()
        {
            PreserveDestinationOnInvalidIdentity("QTO-\u0001-1", "xml-control");
        }

        private static void MalformedSurrogatePreservesDestination()
        {
            PreserveDestinationOnInvalidIdentity("QTO-\uD800-1", "malformed-surrogate");
        }

        private static void PreserveDestinationOnInvalidIdentity(string elementId, string suffix)
        {
            var directory = TempDirectory("review-qto-identity-" + suffix);
            var path = Path.Combine(directory, "review.xlsx");
            var sentinel = new byte[] { 0x51, 0x53, 0x33, 0x44, 0x2D, 0x34, 0x30, 0x35, 0x31 };
            File.WriteAllBytes(path, sentinel);
            try
            {
                Throws<InvalidDataException>(() => Export(path, elementId));
                True(File.ReadAllBytes(path).SequenceEqual(sentinel));
                Equal(1, Directory.GetFiles(directory).Length);
            }
            finally
            {
                TryDeleteDirectory(directory);
            }
        }

        private static void Export(string path, string elementId)
        {
            var details = CreateRow(elementId);
            var summary = CreateRow(elementId);
            var model = new Qs3dReviewModelInfo(
                "PROJECT-4051",
                "Review QTO",
                "FP-4051",
                "REV-1",
                new DateTimeOffset(2026, 8, 26, 0, 0, 0, TimeSpan.Zero));
            Qs3dReviewWorkbookExporter.Export(
                path,
                new[] { details },
                new[] { summary },
                Array.Empty<CoordinationClashExportRow>(),
                Array.Empty<CoordinationDuplicateExportRow>(),
                null,
                model);
        }

        private static QuantityReportRow CreateRow(string elementId)
        {
            var row = new QuantityReportRow
            {
                Floor = "L1",
                Zone = "Z1",
                Category = "Wall",
                FamilyId = "FAMILY-1",
                FamilyName = "Wall",
                ElementName = "Wall instance",
                Material = "Concrete",
                DrawingFingerprint = "FP-4051",
                Count = 1
            };
            row.ElementIds.Add(elementId);
            row.SourceHandles.Add("A1");
            return row;
        }

        private static string TempDirectory(string suffix)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-" + suffix + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
                // Best-effort cleanup only; assertions above are the regression contract.
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected condition to be true.");
        }
    }
}
