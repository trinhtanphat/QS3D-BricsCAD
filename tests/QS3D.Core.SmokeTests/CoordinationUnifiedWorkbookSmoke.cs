using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class CoordinationUnifiedWorkbookSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DuplicateIdentityUsesSemanticPairAndCanonicalEvidence();
            UnifiedWorkbookRoundTripsClashAndDuplicate();
            DuplicateIdentityRejectsHandleOnlyAuthority();
            MixedDrawingWorkbookFailsClosed();
        }

        private static void DuplicateIdentityUsesSemanticPairAndCanonicalEvidence()
        {
            var leftToRight = CoordinationDuplicateIdentity.Create(
                "drawing-fp", "QS3D_DUPLICATE_V1", "EL-A", "EL-B",
                DuplicateMatchKind.ExactGeometry | DuplicateMatchKind.SemanticIdentity);
            var rightToLeft = CoordinationDuplicateIdentity.Create(
                "drawing-fp", "QS3D_DUPLICATE_V1", "EL-B", "EL-A",
                DuplicateMatchKind.SemanticIdentity | DuplicateMatchKind.ExactGeometry);
            Equal(leftToRight, rightToLeft, "duplicate identity changed when semantic pair order changed");
        }

        private static void UnifiedWorkbookRoundTripsClashAndDuplicate()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-unified-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var clash = CoordinationClashExportRow.CreateExactHard(
                    "drawing-fp", "A", "B", "CL-A", "CL-B", "Pipe", "Beam", "L03");
                var duplicate = CoordinationDuplicateExportRow.Create(
                    "drawing-fp",
                    "EL-B", "00D",
                    "EL-A", "00C",
                    DuplicateMatchKind.ExactGeometry,
                    "Column", "Column", "L03");

                CoordinationUnifiedWorkbookExporter.Export(path, new[] { clash }, new[] { duplicate });
                var clashTrace = CoordinationUnifiedWorkbookTraceReader.ReadClash(path, 2);
                var duplicateTrace = CoordinationUnifiedWorkbookTraceReader.ReadDuplicate(path, 2);

                Equal(clash.ClashId, clashTrace.ClashId, "unified clash id did not round-trip");
                Equal("A", clashTrace.LeftHandle, "unified clash left handle was not canonical");
                Equal("B", clashTrace.RightHandle, "unified clash right handle was not canonical");
                Equal(duplicate.DuplicateId, duplicateTrace.DuplicateId, "duplicate id did not round-trip");
                Equal("C", duplicateTrace.LeftHandle, "duplicate left handle did not follow semantic canonical ordering");
                Equal("D", duplicateTrace.RightHandle, "duplicate right handle did not follow semantic canonical ordering");
                Equal("drawing-fp", duplicateTrace.DrawingFingerprint, "duplicate drawing fingerprint did not round-trip");
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void DuplicateIdentityRejectsHandleOnlyAuthority()
        {
            Throws<InvalidDataException>(() => CoordinationDuplicateExportRow.Create(
                "drawing-fp", "", "A", "EL-B", "B", DuplicateMatchKind.ExactGeometry));
            Throws<InvalidDataException>(() => CoordinationDuplicateExportRow.Create(
                "drawing-fp", "EL-A", "A", "el-a", "B", DuplicateMatchKind.ExactGeometry));
            Throws<InvalidDataException>(() => CoordinationDuplicateExportRow.Create(
                "drawing-fp", "EL-A", "A", "EL-B", "B", DuplicateMatchKind.None));
        }

        private static void MixedDrawingWorkbookFailsClosed()
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-coordination-unified-mixed-" + Guid.NewGuid().ToString("N") + ".xlsx");
            try
            {
                var clash = CoordinationClashExportRow.CreateExactHard("drawing-a", "A", "B");
                var duplicate = CoordinationDuplicateExportRow.Create(
                    "drawing-b", "EL-A", "C", "EL-B", "D", DuplicateMatchKind.ExactGeometry);
                Throws<InvalidDataException>(() => CoordinationUnifiedWorkbookExporter.Export(path, new[] { clash }, new[] { duplicate }));
            }
            finally
            {
                TryDelete(path);
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
            throw new InvalidOperationException("CoordinationUnifiedWorkbookSmoke: expected " + typeof(T).Name + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "CoordinationUnifiedWorkbookSmoke: " + message + ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
