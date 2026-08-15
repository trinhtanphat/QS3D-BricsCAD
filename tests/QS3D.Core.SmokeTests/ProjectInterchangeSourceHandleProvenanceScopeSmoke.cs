using System;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSourceHandleProvenanceScopeSmoke
    {
        internal static void Run()
        {
            DrawingLocalRecordReadsUnchanged();
            NonDrawingLocalRecordFailsClosed();
        }

        private static void DrawingLocalRecordReadsUnchanged()
        {
            var target = StoreFixture();
            var handles = ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "P-SOURCE", "E-1");

            Require(handles.Count == 1, "Expected exactly one stored source handle.");
            Require(string.Equals(handles[0], "1A", StringComparison.Ordinal), "Valid drawing-local source handle changed during provenance read.");
            Require(ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "P-SOURCE", "E-MISSING").Count == 0,
                "Missing source-handle provenance must continue returning an empty list.");
        }

        private static void NonDrawingLocalRecordFailsClosed()
        {
            var target = StoreFixture();
            var elementKey = target.Metadata.Keys.Single(x =>
                x.StartsWith(ProjectInterchangeSourceHandleProvenance.MetadataPrefix, StringComparison.OrdinalIgnoreCase) &&
                x.IndexOf(".Element.", StringComparison.Ordinal) >= 0);
            var drawingLocal = Convert.ToBase64String(Encoding.UTF8.GetBytes("drawing-local"));
            var nonDrawingLocal = Convert.ToBase64String(Encoding.UTF8.GetBytes("project-global"));
            var encoded = target.Metadata[elementKey];

            Require(encoded.IndexOf(drawingLocal, StringComparison.Ordinal) >= 0,
                "Provenance smoke fixture no longer contains the canonical drawing-local scope token.");
            target.Metadata[elementKey] = encoded.Replace(drawingLocal, nonDrawingLocal);

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeSourceHandleProvenance.ReadSourceHandles(target, "P-SOURCE", "E-1"));
        }

        private static ProjectState StoreFixture()
        {
            var source = new ProjectState("P-SOURCE", "Source provenance scope")
            {
                DrawingFingerprint = "SRC-DWG-FP",
                UpdatedUtc = new DateTime(2026, 8, 15, 7, 0, 0, DateTimeKind.Utc)
            };
            source.Zones.Add(new ZoneDefinition("Z-1", "Zone 1"));
            source.Floors.Add(new FloorDefinition("FL-1", "L01", 0d));
            source.Families.Add(new ProjectFamily("FAM-1", "Beam 300x500", ElementCategory.Beam));

            var element = new ProjectElement("E-1", ElementCategory.Beam, "FAM-1", "FL-1", "Z-1")
            {
                DrawingFingerprint = "SRC-DWG-FP"
            };
            element.SourceHandles.Add("1A");
            source.Elements.Add(element);

            var target = new ProjectState("P-TARGET", "Target provenance scope");
            ProjectInterchangeSourceHandleProvenance.Store(target, ProjectInterchangeJsonExporter.Build(source));
            return target;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
