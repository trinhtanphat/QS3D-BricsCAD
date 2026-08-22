using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationWorkbookRoundTripSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            StablePairIdentityIsOrderIndependent();
            WorkbookRoundTripsCanonicalPairProvenance();
            DuplicatePairRowsFailClosed();
        }

        private static void StablePairIdentityIsOrderIndependent()
        {
            var leftToRight = CoordinationClashIdentity.Create("drawing-fp", "MEP_EXACT_HARD", "00AF", "000B");
            var rightToLeft = CoordinationClashIdentity.Create("drawing-fp", "MEP_EXACT_HARD", "B", "AF");
            Equal(leftToRight, rightToLeft, "pair identity changed when left/right order changed");
        }

        private static void WorkbookRoundTripsCanonicalPairProvenance()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = CoordinationClashExportRow.CreateExactHard(
                    "drawing-fp",
                    "000A",
                    "000B",
                    "E-A",
                    "E-B",
                    "Pipe",
                    "Beam",
                    "L03");

                CoordinationWorkbookExporter.Export(path, new[] { row });
                var trace = CoordinationWorkbookTraceReader.Read(path, 2);

                Equal(row.ClashId, trace.ClashId, "ClashId did not round-trip");
                Equal("A", trace.LeftHandle, "left Handle was not canonicalized");
                Equal("B", trace.RightHandle, "right Handle was not canonicalized");
                Equal("drawing-fp", trace.DrawingFingerprint, "drawing fingerprint did not round-trip");
                Equal("MEP_EXACT_HARD", trace.RuleId, "rule id did not round-trip");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void DuplicatePairRowsFailClosed()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-duplicate-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var row = CoordinationClashExportRow.CreateExactHard("drawing-fp", "A", "B");
                Throws<InvalidDataException>(() => CoordinationWorkbookExporter.Export(path, new[] { row, row }));
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
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
            throw new InvalidOperationException("CoordinationWorkbookRoundTripSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationWorkbookRoundTripSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
