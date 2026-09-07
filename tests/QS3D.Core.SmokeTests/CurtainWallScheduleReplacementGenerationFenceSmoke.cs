using System;
using System.Reflection;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallScheduleReplacementGenerationFenceSmoke
    {
        public static void Run()
        {
            StableProjectBuildsNormally();
            EquivalentElementReplacementWithoutTouchFailsClosed();
        }

        private static void StableProjectBuildsNormally()
        {
            var rows = CurtainWallScheduleBuilder.Build(Project());
            Equal(1, rows.Count);
            Equal(1, rows[0].WallCount);
            Near(2.5d, rows[0].TotalWallLengthM);
            Equal("E1", rows[0].ElementIds[0]);
        }

        private static void EquivalentElementReplacementWithoutTouchFailsClosed()
        {
            var project = Project();
            var snapshot = CaptureSnapshot(project);
            InvokeGuard(project, snapshot);

            var original = project.Elements[0];
            var originalVersion = project.ChangeVersion;
            var replacement = EquivalentReplacement(original);
            project.Elements[0] = replacement;

            Equal(checked(originalVersion + 1L), project.ChangeVersion);
            if (ReferenceEquals(original, replacement)) throw new Exception("Expected a distinct replacement instance.");
            ThrowsInvalidOperation(
                () => InvokeGuard(project, snapshot),
                "Project changed while the curtain wall schedule was being built");
        }

        private static object CaptureSnapshot(ProjectState project)
        {
            var method = typeof(CurtainWallScheduleBuilder).GetMethod(
                "CaptureProjectRevision",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("Expected CurtainWallScheduleBuilder.CaptureProjectRevision.");
            return method.Invoke(null, new object[] { project })
                ?? throw new Exception("Expected a Curtain Wall schedule revision snapshot.");
        }

        private static void InvokeGuard(ProjectState project, object snapshot)
        {
            var method = typeof(CurtainWallScheduleBuilder).GetMethod(
                "EnsureProjectRevision",
                BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new Exception("Expected CurtainWallScheduleBuilder.EnsureProjectRevision.");
            try
            {
                method.Invoke(null, new[] { (object)project, snapshot });
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                throw ex.InnerException;
            }
        }

        private static ProjectElement EquivalentReplacement(ProjectElement source)
        {
            var replacement = new ProjectElement(
                source.Id,
                source.Category,
                source.FamilyId,
                source.FloorId,
                source.ZoneId);
            foreach (var quantity in source.Quantities) replacement.Quantities.Add(quantity.Key, quantity.Value);
            foreach (var handle in source.SourceHandles) replacement.SourceHandles.Add(handle);

            var updatedUtc = typeof(ProjectElement).GetProperty(
                nameof(ProjectElement.UpdatedUtc),
                BindingFlags.Instance | BindingFlags.Public)
                ?? throw new Exception("Expected ProjectElement.UpdatedUtc.");
            updatedUtc.SetValue(replacement, source.UpdatedUtc);
            return replacement;
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("curtain-schedule-replacement-fence", "Curtain schedule replacement fence");
            project.Floors.Add(new FloorDefinition("floor", "Floor", 0d));
            project.Families.Add(new ProjectFamily("family", "Curtain Family", ElementCategory.GlassWall));
            var element = new ProjectElement("E1", ElementCategory.GlassWall, "family", "floor", string.Empty);
            element.Quantities["LengthM"] = 2.5d;
            element.Quantities["GrossWallAreaM2"] = 5d;
            element.Quantities["CurtainNetGlassAreaM2"] = 5d;
            element.Quantities["CurtainPanelCount"] = 1d;
            element.Quantities["CurtainMinClearPanelWidthM"] = 1d;
            element.Quantities["CurtainMaxClearPanelWidthM"] = 1d;
            element.Quantities["CurtainMinClearPanelHeightM"] = 2d;
            element.Quantities["CurtainMaxClearPanelHeightM"] = 2d;
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
            if (Math.Abs(expected - actual) > 1e-12d) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
