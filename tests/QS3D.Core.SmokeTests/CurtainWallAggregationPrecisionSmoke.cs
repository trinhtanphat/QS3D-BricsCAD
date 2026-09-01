using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallAggregationPrecisionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            LargeFirstRepresentableAggregateIsPreserved();
            SmallFirstRepresentableAggregateIsPreserved();
            MetricsAndGroupsRemainIsolated();
            FinalUnrepresentableAggregateFailsClosed();
            NonFiniteInputStillFailsClosed();
        }

        private static void LargeFirstRepresentableAggregateIsPreserved()
        {
            var project = Project("CW-PREC-LARGE");
            AddWall(project, "g1", "cw", 1e16d);
            AddWall(project, "g2", "cw", 1d);
            AddWall(project, "g3", "cw", 1d);

            var row = Single(project);
            Equal(3, row.WallCount, "large-first wall count");
            Equal(3, row.ElementIds.Count, "large-first provenance count");
            Equal(10000000000000002d, row.TotalWallLengthM, "large-first length");
            Equal(10000000000000002d, row.GrossWallAreaM2, "large-first gross area");
            Equal(10000000000000002d, row.OpeningAreaM2, "large-first opening area");
            Equal(10000000000000002d, row.NetGlassAreaM2, "large-first net glass");
            Equal(10000000000000002d, row.FrameFaceAreaM2, "large-first frame face");
            Equal(10000000000000002d, row.FrameLengthM, "large-first frame length");
        }

        private static void SmallFirstRepresentableAggregateIsPreserved()
        {
            var project = Project("CW-PREC-SMALL");
            AddWall(project, "g1", "cw", 1d);
            AddWall(project, "g2", "cw", 1d);
            AddWall(project, "g3", "cw", 1e16d);

            var row = Single(project);
            Equal(10000000000000002d, row.TotalWallLengthM, "small-first length");
        }

        private static void MetricsAndGroupsRemainIsolated()
        {
            var project = Project("CW-PREC-GROUPS");
            project.Families.Add(new ProjectFamily("cw2", "Curtain 2", ElementCategory.GlassWall));
            AddWall(project, "g1", "cw", 10d, frameLength: 2d);
            AddWall(project, "g2", "cw", 5d, frameLength: 3d);
            AddWall(project, "g3", "cw2", 7d, frameLength: 11d);

            var rows = CurtainWallScheduleBuilder.Build(project);
            Equal(2, rows.Count, "isolated group count");
            Equal(15d, rows[0].TotalWallLengthM, "first-group length");
            Equal(5d, rows[0].FrameLengthM, "first-group frame length");
            Equal(7d, rows[1].TotalWallLengthM, "second-group length");
            Equal(11d, rows[1].FrameLengthM, "second-group frame length");
        }

        private static void FinalUnrepresentableAggregateFailsClosed()
        {
            var project = Project("CW-PREC-LOSS");
            AddWall(project, "g1", "cw", 9007199254740992d);
            AddWall(project, "g2", "cw", 1d);
            Throws<OverflowException>(() => CurtainWallScheduleBuilder.Build(project));
        }

        private static void NonFiniteInputStillFailsClosed()
        {
            var project = Project("CW-PREC-NAN");
            AddWall(project, "g1", "cw", double.NaN);
            Throws<InvalidOperationException>(() => CurtainWallScheduleBuilder.Build(project));
        }

        private static ProjectState Project(string id)
        {
            var project = new ProjectState(id, "Curtain precision smoke");
            project.Floors.Add(new FloorDefinition("f1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Families.Add(new ProjectFamily("cw", "Curtain", ElementCategory.GlassWall));
            return project;
        }

        private static void AddWall(ProjectState project, string id, string familyId, double value, double? frameLength = null)
        {
            var wall = new ProjectElement(id, ElementCategory.GlassWall, familyId, "f1", "z");
            wall.Quantities["LengthM"] = value;
            wall.Quantities["GrossWallAreaM2"] = value;
            wall.Quantities["OpeningAreaM2"] = value;
            wall.Quantities["CurtainNetGlassAreaM2"] = value;
            wall.Quantities["CurtainFrameFaceAreaM2"] = value;
            wall.Quantities["CurtainFrameLengthM"] = frameLength ?? value;
            wall.Quantities["CurtainPanelCount"] = 1d;
            wall.Quantities["CurtainVerticalFrameCount"] = 1d;
            wall.Quantities["CurtainHorizontalFrameCount"] = 1d;
            wall.Quantities["CurtainMinClearPanelWidthM"] = 1d;
            wall.Quantities["CurtainMaxClearPanelWidthM"] = 1d;
            wall.Quantities["CurtainMinClearPanelHeightM"] = 1d;
            wall.Quantities["CurtainMaxClearPanelHeightM"] = 1d;
            project.Elements.Add(wall);
        }

        private static CurtainWallScheduleRow Single(ProjectState project)
        {
            var rows = CurtainWallScheduleBuilder.Build(project);
            Equal(1, rows.Count, "single grouped row");
            return rows[0];
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("CurtainWallAggregationPrecisionSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("CurtainWallAggregationPrecisionSmoke expected " + typeof(TException).Name + ".");
        }
    }
}