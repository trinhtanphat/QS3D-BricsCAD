using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimTakeoffPublicationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            Run();
        }

        internal static void Run()
        {
            var wall = new IfcStandaloneElement(
                "G-WALL-1", "IfcWall", "Tường | ngoài", "L01", "External", "ARC.WALL",
                new[] { new IfcPropertyNode("Pset_WallCommon.IsExternal", "true") },
                new[] { new IfcQtoItem("G-WALL-1", "IfcWall", "L01", "ARC.WALL", "NetSideArea", 12d, "m2") },
                "ifc-step://#100");
            var slab = new IfcStandaloneElement(
                "G-SLAB-1", "IfcSlab", "Sàn", "L01", "Floor", "STR.SLAB",
                Array.Empty<IfcPropertyNode>(),
                new[] { new IfcQtoItem("G-SLAB-1", "IfcSlab", "L01", "STR.SLAB", "NetVolume", 4d, "m3") },
                "ifc-step://#200");
            var document = new IfcStandaloneDocument("C:\\Mô hình\\sample.ifc", "REV-A|sha256:123", new[] { wall, slab });
            var selection = new IfcSelectionSet("Gói BOQ | L01", new[] { "G-SLAB-1", "G-WALL-1", "g-wall-1" });
            var publisher = new QuantBimTakeoffPublisher();

            var snapshot = publisher.Publish(document, document.Revision, selection);
            Equal(document.Path, snapshot.DocumentPath, "document path");
            Equal(document.Revision, snapshot.Revision, "revision");
            Equal(2, snapshot.Evidence.Count, "deduplicated evidence count");
            Equal("G-SLAB-1", snapshot.Evidence[0].Guid, "deterministic evidence order");
            True(snapshot.Boq.Count > 0, "BOQ rows published");
            True(snapshot.Boq.All(x => !double.IsNaN(x.Quantity) && !double.IsInfinity(x.Quantity)), "finite BOQ quantities");

            var encoded = QuantBimTakeoffPublicationCodec.Encode(snapshot);
            var decoded = QuantBimTakeoffPublicationCodec.Decode(encoded);
            Equal(encoded, QuantBimTakeoffPublicationCodec.Encode(decoded), "deterministic publication roundtrip");
            Equal("Gói BOQ | L01", decoded.SelectionName, "unicode selection survives transport");
            publisher.ValidateCurrent(document, decoded);

            RejectsInvalidOperation(
                () => publisher.Publish(document, "REV-OLD", selection),
                "stale expected revision rejected");
            RejectsInvalidOperation(
                () => publisher.Publish(document, document.Revision, new IfcSelectionSet("Missing", new[] { "NOT-IN-DOCUMENT" })),
                "missing selection GUID rejected");
            RejectsInvalidOperation(
                () => publisher.ValidateCurrent(new IfcStandaloneDocument(document.Path, "REV-B", document.Elements), decoded),
                "published snapshot rejected against replacement IFC revision");
            RejectsInvalidOperation(
                () => publisher.Publish(
                    new IfcStandaloneDocument(document.Path, document.Revision, new[] { wall, wall }),
                    document.Revision,
                    new IfcSelectionSet("Duplicate", new[] { wall.Guid })),
                "duplicate IFC GUID rejected");
            RejectsInvalidOperation(
                () => QuantBimTakeoffPublicationCodec.Decode(encoded.Replace("DocumentPath=", "WrongField=")),
                "malformed publication field rejected");
        }

        private static void RejectsInvalidOperation(Action action, string label)
        {
            var rejected = false;
            try { action(); }
            catch (InvalidOperationException) { rejected = true; }
            True(rejected, label);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ".");
        }
    }
}
