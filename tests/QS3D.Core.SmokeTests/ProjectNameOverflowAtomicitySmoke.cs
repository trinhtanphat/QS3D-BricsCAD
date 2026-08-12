using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectNameOverflowAtomicitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-NAME-OVERFLOW", "Stable Name");
            var seededUtc = new DateTime(2026, 8, 12, 0, 0, 0, DateTimeKind.Utc);
            SeedPersistenceState(project, seededUtc, long.MaxValue);

            Throws<OverflowException>(() => project.Name = "Changed Name");
            Equal("Stable Name", project.Name, "overflow preserves name");
            Equal(long.MaxValue, project.ChangeVersion, "overflow preserves revision");
            Equal(seededUtc, project.UpdatedUtc, "overflow preserves timestamp");

            var normal = new ProjectState("P-NAME-NORMAL", "Before");
            var beforeVersion = normal.ChangeVersion;
            normal.Name = "After";
            Equal("After", normal.Name, "normal rename value");
            Equal(beforeVersion + 1L, normal.ChangeVersion, "normal rename revision");
        }

        private static void SeedPersistenceState(ProjectState project, DateTime updatedUtc, long changeVersion)
        {
            var restore = typeof(ProjectState).GetMethod("RestorePersistenceState", BindingFlags.Instance | BindingFlags.NonPublic);
            if (restore == null)
                throw new Exception("ProjectNameOverflowAtomicitySmoke could not find ProjectState.RestorePersistenceState.");
            restore.Invoke(project, new object[] { updatedUtc, changeVersion });
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("ProjectNameOverflowAtomicitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("ProjectNameOverflowAtomicitySmoke expected " + typeof(TException).Name + ".");
        }
    }
}
