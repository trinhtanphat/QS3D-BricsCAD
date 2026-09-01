using System;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleSmoke
    {
        public static void Run()
        {
            GroupsByStableFloorAndFamily();
            RejectsNonIntegerPanelCounts();
            RejectsInvertedClearPanelWidth();
            RejectsInvertedClearPanelHeight();
        }

        private static void GroupsByStableFloorAndFamily()
        {
            var project = new ProjectState("p", "Curtain schedule");
            project.Floors.Add(new FloorDefinition("f1", "Tầng 1", 0d));
            project.Families.Add(new ProjectFamily("cw", "Vách kính 12mm", ElementCategory.GlassWall));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            Add(project, "g1", 6d, 18d, 2d, 14.3d, 1.7d, 33d, 8, 5, 3, 1.4d, 1.45d, 1.4d, 1.45d);
            Add(project, "g2", 3d, 9d, 0d, 8.2d, 0.8d, 18d, 4, 3, 3, 1.3d, 1.4d, 1.35d, 1.45d);

            var rows = CurtainWallScheduleBuilder.Build(project);
            if (rows.Count != 1) throw new Exception("Expected one grouped curtain row.");
            var row = rows[0];
            if (row.Floor != "Tầng 1" || row.FamilyName != "Vách kính 12mm") throw new Exception("Stable floor/family grouping failed.");
            if (row.WallCount != 2 || row.PanelCount != 12 || row.VerticalFrameCount != 8 || row.HorizontalFrameCount != 6) throw new Exception("Curtain integer aggregation failed.");
            Near(9d, row.TotalWallLengthM);
            Near(27d, row.GrossWallAreaM2);
            Near(2d, row.OpeningAreaM2);
            Near(22.5d, row.NetGlassAreaM2);
            Near(2.5d, row.FrameFaceAreaM2);
            Near(51d, row.FrameLengthM);
            Near(1.3d, row.MinimumClearPanelWidthM);
            Near(1.45d, row.MaximumClearPanelWidthM);
            Near(1.35d, row.MinimumClearPanelHeightM);
            Near(1.45d, row.MaximumClearPanelHeightM);
            if (row.ElementIds.Count != 2) throw new Exception("Curtain element provenance failed.");
        }

        private static void RejectsNonIntegerPanelCounts()
        {
            var project = NewProject("p2", "Bad curtain schedule");
            var wall = new ProjectElement("g1", ElementCategory.GlassWall, "cw", "f1", "z");
            wall.Quantities["CurtainPanelCount"] = 2.5d;
            project.Elements.Add(wall);
            Throws<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
        }

        private static void RejectsInvertedClearPanelWidth()
        {
            var project = NewProject("p-width", "Inverted curtain width");
            Add(project, "g-width", 3d, 9d, 0d, 8d, 1d, 18d, 4, 3, 3, 2.0d, 1.0d, 1.2d, 1.4d);

            var error = Capture<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
            Contains("g-width/CurtainClearPanelWidthM minimum cannot exceed maximum", error.Message);
        }

        private static void RejectsInvertedClearPanelHeight()
        {
            var project = NewProject("p-height", "Inverted curtain height");
            Add(project, "g-height", 3d, 9d, 0d, 8d, 1d, 18d, 4, 3, 3, 1.0d, 2.0d, 2.5d, 1.5d);

            var error = Capture<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
            Contains("g-height/CurtainClearPanelHeightM minimum cannot exceed maximum", error.Message);
        }

        private static ProjectState NewProject(string projectId, string name)
        {
            var project = new ProjectState(projectId, name);
            project.Floors.Add(new FloorDefinition("f1", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Families.Add(new ProjectFamily("cw", "Curtain", ElementCategory.GlassWall));
            return project;
        }

        private static void Add(ProjectState project, string id, double length, double grossArea, double openingArea, double netGlass, double frameArea, double frameLength, int panels, int verticalFrames, int horizontalFrames, double minWidth, double maxWidth, double minHeight, double maxHeight)
        {
            var wall = new ProjectElement(id, ElementCategory.GlassWall, "cw", "f1", "z");
            wall.Quantities["LengthM"] = length;
            wall.Quantities["GrossWallAreaM2"] = grossArea;
            wall.Quantities["OpeningAreaM2"] = openingArea;
            wall.Quantities["CurtainNetGlassAreaM2"] = netGlass;
            wall.Quantities["CurtainFrameFaceAreaM2"] = frameArea;
            wall.Quantities["CurtainFrameLengthM"] = frameLength;
            wall.Quantities["CurtainPanelCount"] = panels;
            wall.Quantities["CurtainVerticalFrameCount"] = verticalFrames;
            wall.Quantities["CurtainHorizontalFrameCount"] = horizontalFrames;
            wall.Quantities["CurtainMinClearPanelWidthM"] = minWidth;
            wall.Quantities["CurtainMaxClearPanelWidthM"] = maxWidth;
            wall.Quantities["CurtainMinClearPanelHeightM"] = minHeight;
            wall.Quantities["CurtainMaxClearPanelHeightM"] = maxHeight;
            project.Elements.Add(wall);
        }

        private static void Near(double expected, double actual, double tolerance = 1e-10d)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static TException Capture<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException ex) { return ex; }
            throw new Exception("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new Exception("Expected message to contain '" + expected + "', got '" + actual + "'.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
