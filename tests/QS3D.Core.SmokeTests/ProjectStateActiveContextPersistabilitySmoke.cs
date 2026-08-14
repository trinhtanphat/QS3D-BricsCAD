using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectStateActiveContextPersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PaddedActiveContextNormalizesAndEquivalentAssignmentsAreNoOps();
            ControlCharacterAssignmentsFailAtomically();
            NullStillClearsActiveContext();
            DrawingIdentityTextRemainsExact();
        }

        private static void PaddedActiveContextNormalizesAndEquivalentAssignmentsAreNoOps()
        {
            var project = new ProjectState("PROJECT-ACTIVE-CONTEXT", "Active Context");

            project.ActiveZoneId = "  ZONE-01  ";
            project.ActiveFloorId = "  FLOOR-01  ";

            Equal("ZONE-01", project.ActiveZoneId);
            Equal("FLOOR-01", project.ActiveFloorId);

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            project.ActiveZoneId = " ZONE-01 ";
            project.ActiveFloorId = " FLOOR-01 ";

            Equal(version, project.ChangeVersion);
            Equal(updatedUtc, project.UpdatedUtc);
        }

        private static void ControlCharacterAssignmentsFailAtomically()
        {
            var project = new ProjectState("PROJECT-ACTIVE-CONTEXT-CONTROL", "Active Context Control")
            {
                ActiveZoneId = "ZONE-01",
                ActiveFloorId = "FLOOR-01"
            };

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;

            Throws<ArgumentException>(() => project.ActiveZoneId = "ZONE\u0001-02");
            Equal("ZONE-01", project.ActiveZoneId);
            Equal("FLOOR-01", project.ActiveFloorId);
            Equal(version, project.ChangeVersion);
            Equal(updatedUtc, project.UpdatedUtc);

            Throws<ArgumentException>(() => project.ActiveFloorId = "FLOOR\u0001-02");
            Equal("ZONE-01", project.ActiveZoneId);
            Equal("FLOOR-01", project.ActiveFloorId);
            Equal(version, project.ChangeVersion);
            Equal(updatedUtc, project.UpdatedUtc);
        }

        private static void NullStillClearsActiveContext()
        {
            var project = new ProjectState("PROJECT-ACTIVE-CONTEXT-NULL", "Active Context Null")
            {
                ActiveZoneId = "ZONE-01",
                ActiveFloorId = "FLOOR-01"
            };

            project.ActiveZoneId = null!;
            project.ActiveFloorId = null!;

            Equal(string.Empty, project.ActiveZoneId);
            Equal(string.Empty, project.ActiveFloorId);
        }

        private static void DrawingIdentityTextRemainsExact()
        {
            var project = new ProjectState("PROJECT-DRAWING-TEXT", "Drawing Text")
            {
                DrawingPath = "  C:/Exact Drawing.dwg  ",
                DrawingFingerprint = "  fingerprint:AbC123  "
            };

            Equal("  C:/Exact Drawing.dwg  ", project.DrawingPath);
            Equal("  fingerprint:AbC123  ", project.DrawingFingerprint);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }
    }
}
