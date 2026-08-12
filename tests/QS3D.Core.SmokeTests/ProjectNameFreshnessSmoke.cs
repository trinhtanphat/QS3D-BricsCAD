using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectNameFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-NAME-FRESH", "Original Project");
            var stamp = new ProjectPersistenceStamp(project);
            False(stamp.RequiresSave(project), "fresh baseline save state");

            var beforeVersion = project.ChangeVersion;
            project.Name = "  Renamed Project  ";
            Equal("Renamed Project", project.Name, "canonical renamed value");
            Equal(beforeVersion + 1L, project.ChangeVersion, "rename revision");
            True(stamp.RequiresSave(project), "rename persistence dirty state");

            stamp.MarkSaved(project);
            False(stamp.RequiresSave(project), "saved rename state");
            var savedVersion = project.ChangeVersion;
            var savedUpdated = project.UpdatedUtc;

            project.Name = " Renamed Project ";
            Equal(savedVersion, project.ChangeVersion, "canonical-equivalent rename no-op revision");
            Equal(savedUpdated, project.UpdatedUtc, "canonical-equivalent rename no-op timestamp");
            False(stamp.RequiresSave(project), "canonical-equivalent rename no-op save state");

            Throws<ArgumentException>(() => project.Name = "   ");
            Equal("Renamed Project", project.Name, "invalid rename preserves value");
            Equal(savedVersion, project.ChangeVersion, "invalid rename preserves revision");
            Equal(savedUpdated, project.UpdatedUtc, "invalid rename preserves timestamp");
            False(stamp.RequiresSave(project), "invalid rename preserves save state");

            var snapshot = ProjectStateSnapshot.Capture(project);
            project.Name = "Temporary Name";
            Equal(savedVersion + 1L, project.ChangeVersion, "temporary rename revision");
            snapshot.Restore(project);
            Equal("Renamed Project", project.Name, "snapshot restored name");
            Equal(savedVersion, project.ChangeVersion, "snapshot restored revision");
            Equal(savedUpdated, project.UpdatedUtc, "snapshot restored timestamp");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new Exception("ProjectNameFreshnessSmoke expected true: " + label + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("ProjectNameFreshnessSmoke expected false: " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectNameFreshnessSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectNameFreshnessSmoke expected " + typeof(TException).Name + ".");
        }
    }
}
