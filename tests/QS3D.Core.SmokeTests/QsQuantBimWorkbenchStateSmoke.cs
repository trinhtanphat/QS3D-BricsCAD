using System;
using System.Collections.Generic;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimWorkbenchStateSmoke
    {
        internal static void Run()
        {
            var wall = new IfcStandaloneElement(
                "G-WALL-1",
                "IfcWall",
                "Wall 1",
                "L01",
                "External Wall",
                "ARC.WALL",
                new[] { new IfcPropertyNode("Pset_WallCommon.IsExternal", "true") },
                new[] { new IfcQtoItem("G-WALL-1", "IfcWall", "L01", "ARC.WALL", "NetSideArea", 12d, "m2") },
                "ifc-step://#100");
            var slab = new IfcStandaloneElement(
                "G-SLAB-1",
                "IfcSlab",
                "Slab 1",
                "L01",
                "Floor Slab",
                "STR.SLAB",
                Array.Empty<IfcPropertyNode>(),
                new[] { new IfcQtoItem("G-SLAB-1", "IfcSlab", "L01", "STR.SLAB", "NetVolume", 4d, "m3") },
                "ifc-step://#200");
            var document = new IfcStandaloneDocument("C:\\models\\sample.ifc", "REV-A", new[] { wall, slab });
            var filter = new IfcWorkbenchFilter("IfcWall", "L01", "", "ARC.WALL");
            var selection = new IfcSelectionSet("Exterior walls", new[] { "G-WALL-1", "g-wall-1" });
            var coordinator = new QuantBimWorkbenchStateCoordinator();

            var snapshot = coordinator.Capture(document, filter, selection);
            Equal(1, snapshot.SelectedGuids.Count, "selection GUIDs are deduplicated");
            Equal("G-WALL-1", snapshot.SelectedGuids[0], "selection identity preserved");

            var encoded = QuantBimWorkbenchStateCodec.Encode(snapshot);
            var decoded = QuantBimWorkbenchStateCodec.Decode(encoded);
            Equal(encoded, QuantBimWorkbenchStateCodec.Encode(decoded), "state transport is deterministic");

            var restored = coordinator.Restore(document, decoded);
            Equal("IfcWall", restored.Filter.Entity, "entity filter restored");
            Equal("ARC.WALL", restored.Filter.Classification, "classification filter restored");
            Equal("Exterior walls", restored.Selection.Name, "selection name restored");
            Equal("G-WALL-1", restored.Selection.Guids[0], "selection GUID restored");

            RejectsInvalidOperation(
                () => coordinator.Restore(new IfcStandaloneDocument(document.Path, "REV-B", document.Elements), decoded),
                "stale revision state is rejected");

            RejectsInvalidOperation(
                () => coordinator.Capture(document, filter, new IfcSelectionSet("Unknown", new[] { "G-MISSING" })),
                "unknown selection GUID is rejected");

            RejectsInvalidOperation(
                () => coordinator.Capture(document, new IfcWorkbenchFilter("IfcSlab", "", "", ""), selection),
                "selection hidden by filter is rejected");

            RejectsInvalidOperation(
                () => QuantBimWorkbenchStateCodec.Decode(encoded.Replace("Revision=", "Unexpected=")),
                "unexpected state field is rejected");

            QuantBimStandaloneCompositionSmoke.Run();
        }

        private static void RejectsInvalidOperation(Action action, string label)
        {
            var rejected = false;
            try { action(); }
            catch (InvalidOperationException) { rejected = true; }
            if (!rejected) throw new InvalidOperationException(label + ": expected rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
