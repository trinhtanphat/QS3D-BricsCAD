using System;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticOverflowSmoke
    {
        public static void Run()
        {
            WallOverflowDoesNotPartiallyMutate();
            FinishOverflowDoesNotPartiallyMutate();
            BeamOverflowDoesNotPartiallyMutate();
            StairOverflowDoesNotPartiallyMutate();
            EarthworkOverflowDoesNotPartiallyMutate();
        }

        private static void WallOverflowDoesNotPartiallyMutate()
        {
            var project = NewProject();
            var element = Element("W-OVER", ElementCategory.ArchitecturalWall);
            element.Properties["LengthM"] = Max();
            element.Properties["HeightM"] = "2";
            element.Properties["ThicknessM"] = "0.2";
            element.Quantities["Sentinel"] = 1d;
            project.Elements.Add(element);

            Throws<OverflowException>(() => new WallRegenerator().Regenerate(project, element));
            OnlySentinel(element);
        }

        private static void FinishOverflowDoesNotPartiallyMutate()
        {
            var project = NewProject();
            var element = Element("F-OVER", ElementCategory.WallFinish);
            element.Properties["AreaM2"] = "1";
            element.Properties["PerimeterM"] = Max();
            element.Properties["HeightM"] = "2";
            element.Quantities["Sentinel"] = 1d;
            project.Elements.Add(element);

            Throws<OverflowException>(() => new RoomRegenerator().Regenerate(project, element));
            OnlySentinel(element);
        }

        private static void BeamOverflowDoesNotPartiallyMutate()
        {
            var project = NewProject();
            var element = Element("B-OVER", ElementCategory.Beam);
            element.Properties["LengthM"] = Max();
            element.Properties["WidthM"] = "2";
            element.Properties["HeightM"] = "2";
            element.Quantities["Sentinel"] = 1d;
            project.Elements.Add(element);

            Throws<OverflowException>(() => new StructuralRegenerator().Regenerate(project, element));
            OnlySentinel(element);
        }

        private static void StairOverflowDoesNotPartiallyMutate()
        {
            var project = NewProject();
            var element = Element("S-OVER", ElementCategory.Stair);
            element.Properties["AreaM2"] = "1";
            element.Properties["WidthM"] = "1";
            element.Properties["RunLengthM"] = Max();
            element.Properties["TotalRiseM"] = Max();
            element.Properties["ThicknessM"] = "0.2";
            element.Properties["StepCount"] = "10";
            element.Quantities["Sentinel"] = 1d;
            project.Elements.Add(element);

            Throws<OverflowException>(() => new StructuralRegenerator().Regenerate(project, element));
            OnlySentinel(element);
        }

        private static void EarthworkOverflowDoesNotPartiallyMutate()
        {
            var project = NewProject();
            var element = Element("E-OVER", ElementCategory.Earthwork);
            element.Properties["AreaM2"] = Max();
            element.Properties["DepthM"] = "2";
            element.Properties["BulkingFactor"] = "1.2";
            element.Quantities["Sentinel"] = 1d;
            project.Elements.Add(element);

            Throws<OverflowException>(() => new StructuralRegenerator().Regenerate(project, element));
            OnlySentinel(element);
        }

        private static ProjectElement Element(string id, ElementCategory category) => new ProjectElement(id, category, string.Empty, "f", "z");

        private static ProjectState NewProject()
        {
            var project = new ProjectState("semantic-overflow", "Semantic Overflow");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            return project;
        }

        private static void OnlySentinel(ProjectElement element)
        {
            if (element.Quantities.Count != 1 || !element.Quantities.TryGetValue("Sentinel", out var value) || Math.Abs(value - 1d) > 1e-12)
                throw new Exception("Regenerator partially mutated quantities before reporting overflow: " + element.Id + " → " + string.Join(",", element.Quantities.Keys.OrderBy(x => x)));
        }

        private static string Max() => double.MaxValue.ToString("R", CultureInfo.InvariantCulture);

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
