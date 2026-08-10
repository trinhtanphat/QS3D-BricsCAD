using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RegenerationPreviewSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreviewRunsOnDetachedState();
            StalePreviewFailsBeforeLiveMutation();
            FreshPreviewCanApplyWithoutNewHealthErrors();
        }

        private static void PreviewRunsOnDetachedState()
        {
            var project = Fixture();
            var beam = project.FindElement("B1")!;
            var originalDirty = beam.Dirty;
            var originalUpdated = beam.UpdatedUtc;
            var projectUpdated = project.UpdatedUtc;

            var preview = new RegenerationPreviewService().Preview(project);
            True(preview.RegeneratedElementCount >= 1);
            True(preview.HasSemanticChanges);
            True(preview.Deltas.Any(x => x.ElementId == "B1" && x.Fields.Any(f => f.Field == "Quantity:NetVolumeM3")));
            True(!beam.Quantities.ContainsKey("NetVolumeM3"));
            Equal(originalDirty, beam.Dirty);
            Equal(originalUpdated, beam.UpdatedUtc);
            Equal(projectUpdated, project.UpdatedUtc);
        }

        private static void StalePreviewFailsBeforeLiveMutation()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var preview = service.Preview(project);
            var beam = project.FindElement("B1")!;
            beam.SetProperty("LengthM", "8");
            Throws<InvalidOperationException>(() => service.Apply(project, preview));
            True(!beam.Quantities.ContainsKey("NetVolumeM3"));
        }

        private static void FreshPreviewCanApplyWithoutNewHealthErrors()
        {
            var project = Fixture();
            var service = new RegenerationPreviewService();
            var preview = service.Preview(project);
            True(!preview.IntroducesHealthErrors);

            var result = service.Apply(project, preview);
            True(result.RegeneratedElementCount >= 1);
            Equal(0, result.HealthDiff.NewErrorCount);
            Near(0.9d, project.FindElement("B1")!.Quantities["NetVolumeM3"]);
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-REGEN-PREVIEW", "Regen Preview");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            var family = new ProjectFamily("FAM", "Beam", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);

            var beam = new ProjectElement("B1", ElementCategory.Beam, "FAM", "F", "Z");
            beam.Properties["LengthM"] = "6";
            beam.Properties["WidthM"] = "0.3";
            beam.Properties["HeightM"] = "0.5";
            project.Elements.Add(beam);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
