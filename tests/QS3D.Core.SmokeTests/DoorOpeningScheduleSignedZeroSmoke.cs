using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class DoorOpeningScheduleSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("SIGNED-ZERO", "Door schedule signed-zero smoke");
            project.Elements.Add(Opening("OPEN-A", "0", "0"));
            project.Elements.Add(Opening("OPEN-B", "-0", "-0"));
            project.Elements.Add(Opening("OPEN-C", "0.1", "0"));

            var rows = DoorOpeningScheduleBuilder.Build(project);
            Equal(2, rows.Count);

            var zeroRow = rows.Single(x => x.SillHeightM == 0d && x.ThicknessM == 0d);
            Equal(2, zeroRow.Count);
            Equal(2, zeroRow.ElementIds.Count);
            True(zeroRow.ElementIds.Contains("OPEN-A"));
            True(zeroRow.ElementIds.Contains("OPEN-B"));

            var shiftedRow = rows.Single(x => x.SillHeightM == 0.1d);
            Equal(1, shiftedRow.Count);
            Equal("OPEN-C", shiftedRow.ElementIds[0]);
        }

        private static ProjectElement Opening(string id, string sillHeightM, string thicknessM)
        {
            var element = new ProjectElement(id, ElementCategory.WallOpening);
            element.Properties["WidthM"] = "0.9";
            element.Properties["HeightM"] = "2.2";
            element.Properties["SillHeightM"] = sillHeightM;
            element.Properties["ThicknessM"] = thicknessM;
            element.Properties["Material"] = "Glass";
            return element;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected condition to be true.");
        }
    }
}
