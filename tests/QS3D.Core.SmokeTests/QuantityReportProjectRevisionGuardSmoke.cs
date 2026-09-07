using System;
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

        private static object Snapshot(ProjectState project)
        {
            var snapshotType = typeof(ProjectQuantityReportBuilder).GetNestedType(
                "ProjectQuantityGenerationSnapshot",
                BindingFlags.NonPublic)
                ?? throw new Exception("Expected ProjectQuantityReportBuilder.ProjectQuantityGenerationSnapshot.");
            var capture = snapshotType.GetMethod(
                "Capture",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("Expected ProjectQuantityGenerationSnapshot.Capture.");
            try
            {
                return capture.Invoke(null, new object[] { project })
                    ?? throw new Exception("Project quantity generation snapshot capture returned null.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static void InvokeGuard(ProjectState project, object snapshot)
        {
            var method = typeof(ProjectQuantityReportBuilder).GetMethod(
                "EnsureProjectRevision",
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ProjectState), snapshot.GetType() },
                modifiers: null)
                ?? throw new Exception("Expected ProjectQuantityReportBuilder.EnsureProjectRevision(ProjectState, ProjectQuantityGenerationSnapshot).");
            try
            {
                method.Invoke(null, new[] { (object)project, snapshot });
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
    }
}
