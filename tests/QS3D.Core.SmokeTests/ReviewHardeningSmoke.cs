using System;
using System.IO;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Revisions;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class ReviewHardeningSmoke
    {
        public static void Run()
        {
            UnitConversions();
            RecognitionRules();
            RevisionRoundTrip();
        }

        private static void UnitConversions()
        {
            Near(0.0254d, UnitScale.ToMeters(1d, DrawingUnit.Inch));
            Near(0.3048d, UnitScale.ToMeters(1d, DrawingUnit.Foot));
            Near(1000d, UnitScale.FromMeters(1d, DrawingUnit.Millimeter));
            var policy = new ProjectUnitPolicy(LengthUnit.Centimeter); Near(2.5d, policy.ToMeters(250d)); Near(250d, policy.FromMeters(2.5d));
        }

        private static void RecognitionRules()
        {
            var snapshot = new EntitySnapshot("AB", "Line", "KC-DAM"); snapshot.Metadata["Text"] = "Dầm chính";
            var result = new RecognitionEngine().Suggest(snapshot);
            True(result.TopCandidate != null); Equal(ElementCategory.Beam, result.TopCandidate!.Category); True(result.Confidence >= .92d); True(!result.RequiresReview);
        }

        private static void RevisionRoundTrip()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Revision"); project.Zones.Add(new ZoneDefinition("z", "Vùng")); project.Floors.Add(new FloorDefinition("f", "Tầng", 0));
            var element = new ProjectElement("B1", ElementCategory.Beam, "beam-family", "f", "z"); element.Properties["Material"] = "C30"; element.SourceHandles.Add("A1"); element.SetQuantity("NetVolumeM3", 1.25d); project.Elements.Add(element);
            var service = new RevisionService(); var before = service.Capture(project, "BASE");
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-revision-" + Guid.NewGuid().ToString("N")); var path = Path.Combine(directory, "review.qsrev");
            try
            {
                var store = new RevisionSnapshotStore(); store.Save(before, path); var loaded = store.Load(path); var item = loaded.Elements.Single();
                Equal("beam-family", item.FamilyId); Equal("f", item.FloorId); Equal("z", item.ZoneId); Equal("C30", item.Properties["Material"]); Equal("A1", item.SourceHandles.Single()); Near(1.25d, item.Quantities["NetVolumeM3"]);
                element.SetQuantity("NetVolumeM3", 1.5d); var after = service.Capture(project, "CURRENT"); var row = new QuantityRevisionReport().Build(loaded, after).Single(x => x.QuantityName == "NetVolumeM3"); Near(.25d, row.Delta);
            }
            finally { try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { } }
        }

        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    }
}
