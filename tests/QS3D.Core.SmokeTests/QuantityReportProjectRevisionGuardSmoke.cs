using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportProjectRevisionGuardSmoke
    {
        public static void Run()
        {
            StableProjectBuildsNormally();
            ChangeVersionDriftFailsClosed();
            StructuralReplacementWithoutTouchFailsClosed();
        }

        private static void StableProjectBuildsNormally()
        {
            var project = Project();
            var rows = ProjectQuantityReportBuilder.Group(project);
            Equal(1, rows.Count);
            Equal(1, rows[0].Count);
            Near(2.5d, rows[0].LengthM);
        }

        private static void ChangeVersionDriftFailsClosed()
        {
            var project = Project();
            var snapshot = Snapshot(project);
            InvokeGuard(project, snapshot);

            project.Touch();
            ThrowsInvalidOperation(() => InvokeGuard(project, snapshot), "Project changed while the quantity report was being built");
        }

        private static void StructuralReplacementWithoutTouchFailsClosed()
        {
            var project = Project();
            var snapshot = Snapshot(project);
            var originalVersion = project.ChangeVersion;
            project.Elements[0] = new ProjectElement("E1", ElementCategory.Beam, "family", "floor", "zone");
            Equal(originalVersion, project.ChangeVersion);

            ThrowsInvalidOperation(() => InvokeGuard(project, snapshot), "Project changed while the quantity report was being built");
        }

        private static SnapshotState Snapshot(ProjectState project) => new SnapshotState(
            project.ChangeVersion,
            project.Elements.ToList(),
            project.Floors.ToList(),
            project.Zones.ToList(),
            project.Families.ToList(),
            project.DrawingFingerprint);

        private static void InvokeGuard(ProjectState project, SnapshotState snapshot)
        {
            var method = typeof(ProjectQuantityReportBuilder).GetMethod(
                "EnsureProjectRevision",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("Expected ProjectQuantityReportBuilder.EnsureProjectRevision.");
            try
            {
                method.Invoke(null, new object[]
                {
                    project,
                    snapshot.ChangeVersion,
                    snapshot.Elements,
                    snapshot.Floors,
                    snapshot.Zones,
                    snapshot.Families,
                    snapshot.DrawingFingerprint
                });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("quantity-report-revision-guard", "Quantity report revision guard");
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Zones.Add(new ZoneDefinition("zone", "Zone"));
            project.Families.Add(new ProjectFamily("family", "Family", ElementCategory.Beam));
            var element = new ProjectElement("E1", ElementCategory.Beam, "family", "floor", "zone");
            element.Quantities["LengthM"] = 2.5d;
            element.SourceHandles.Add("H-E1");
            project.Elements.Add(element);
            return project;
        }

        private static void ThrowsInvalidOperation(Action action, string expectedMessagePart)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedMessagePart, StringComparison.Ordinal) >= 0) return;
                throw new Exception("Expected message containing '" + expectedMessagePart + "', got '" + ex.Message + "'.");
            }
            throw new Exception("Expected InvalidOperationException.");
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private sealed class SnapshotState
        {
            public SnapshotState(
                long changeVersion,
                IReadOnlyList<ProjectElement> elements,
                IReadOnlyList<FloorDefinition> floors,
                IReadOnlyList<ZoneDefinition> zones,
                IReadOnlyList<ProjectFamily> families,
                string drawingFingerprint)
            {
                ChangeVersion = changeVersion;
                Elements = elements;
                Floors = floors;
                Zones = zones;
                Families = families;
                DrawingFingerprint = drawingFingerprint;
            }

            public long ChangeVersion { get; }
            public IReadOnlyList<ProjectElement> Elements { get; }
            public IReadOnlyList<FloorDefinition> Floors { get; }
            public IReadOnlyList<ZoneDefinition> Zones { get; }
            public IReadOnlyList<ProjectFamily> Families { get; }
            public string DrawingFingerprint { get; }
        }
    }
}
