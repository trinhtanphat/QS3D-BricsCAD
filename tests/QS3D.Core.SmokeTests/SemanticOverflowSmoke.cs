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
            FinishLiteralUnderflowDoesNotPartiallyMutate();
            SemanticLiteralZeroAndSubnormalRemainRepresentable();
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

        private static void FinishLiteralUnderflowDoesNotPartiallyMutate()
        {
            var project = NewProject();
            var element = Element("F-UNDER", ElementCategory.WallFinish);
            element.Properties["AreaM2"] = "1";
            element.Properties["PerimeterM"] = "1e-4000";
            element.Properties["HeightM"] = "1";
            element.Quantities["Sentinel"] = 1d;
            project.Elements.Add(element);

            var error = Capture<InvalidOperationException>(() => new RoomRegenerator().Regenerate(project, element));
            Contains("F-UNDER/PerimeterM underflowed to zero.", error.Message);
            OnlySentinel(element);
        }

        private static void SemanticLiteralZeroAndSubnormalRemainRepresentable()
        {
            var project = NewProject();
            var element = Element("R-SMALL", ElementCategory.Room);
            element.Properties["AreaM2"] = "0e-4000";
            element.Properties["PerimeterM"] = "5e-324";
            project.Elements.Add(element);

            new RoomRegenerator().Regenerate(project, element);

            Exact(0d, element.Quantities["AreaM2"]);
            Exact(double.Epsilon, element.Quantities["PerimeterM"]);
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

        private static void Exact(double expected, double actual)
        {
            if (!expected.Equals(actual))
                throw new Exception("Expected exact " + expected + " but got " + actual + ".");
        }

        private static string Max() => double.MaxValue.ToString("R", CultureInfo.InvariantCulture);

        private static void Throws<T>(Action action) where T : Exception
        {
            Capture<T>(action);
        }

        private static T Capture<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T ex) { return ex; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected '" + actual + "' to contain '" + expected + "'.");
        }
    }
}
