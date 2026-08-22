using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbDrawingIdentityRoundTripSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-drawing-identity-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);

            try
            {
                const string drawingPath = "  drawing/path.dwg  ";
                const string projectFingerprint = "project-fingerprint";
                const string elementFingerprint = "element-fingerprint";

                var project = new ProjectState("DRAW-IDENTITY", "Drawing identity round trip")
                {
                    DrawingPath = drawingPath,
                    DrawingFingerprint = projectFingerprint,
                    ActiveZoneId = "Z1",
                    ActiveFloorId = "F1"
                };
                project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
                project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
                project.Families.Add(new ProjectFamily("FAM-1", "Beam Family", ElementCategory.Beam));
                var element = new ProjectElement("E1", ElementCategory.Beam, "FAM-1", "F1", "Z1")
                {
                    DrawingFingerprint = elementFingerprint
                };
                project.Elements.Add(element);

                var versionBeforeRejectedPath = project.ChangeVersion;
                var updatedBeforeRejectedPath = project.UpdatedUtc;
                var rejectedPath = false;
                try
                {
                    project.DrawingPath = "drawing\u0001path.dwg";
                }
                catch (ArgumentException)
                {
                    rejectedPath = true;
                }

                if (!rejectedPath)
                    throw new InvalidOperationException("Project DrawingPath accepted an XML-illegal control character.");
                Equal(drawingPath, project.DrawingPath, "Rejected Project drawing path assignment");
                if (project.ChangeVersion != versionBeforeRejectedPath)
                    throw new InvalidOperationException("Rejected Project DrawingPath assignment changed ChangeVersion.");
                if (project.UpdatedUtc != updatedBeforeRejectedPath)
                    throw new InvalidOperationException("Rejected Project DrawingPath assignment changed UpdatedUtc.");

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var roundTrip = store.Load(path);

                Equal(drawingPath, roundTrip.DrawingPath, "Project drawing path");
                Equal(projectFingerprint, roundTrip.DrawingFingerprint, "Project drawing fingerprint");
                Equal("Z1", roundTrip.ActiveZoneId, "Active zone id");
                Equal("F1", roundTrip.ActiveFloorId, "Active floor id");

                var loadedElement = roundTrip.FindElement("E1") ?? throw new InvalidOperationException("Saved element E1 did not round-trip.");
                Equal(elementFingerprint, loadedElement.DrawingFingerprint, "Element drawing fingerprint");
                Equal("FAM-1", loadedElement.FamilyId, "Element family id");
                Equal("F1", loadedElement.FloorId, "Element floor id");
                Equal("Z1", loadedElement.ZoneId, "Element zone id");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " changed across QSDB save/load.");
        }
    }
}
