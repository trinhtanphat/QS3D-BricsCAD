using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectNameFreshnessSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var fallback = new ProjectState("P-NAME-FALLBACK", "   ");
            Equal("QS3D Project", fallback.Name, "blank constructor fallback");
            Throws<ArgumentException>(() => new ProjectState("P-NAME-CONTROL", "Broken\u0001Project"));

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

            Throws<ArgumentException>(() => project.Name = "Broken\u0001Project");
            Equal("Renamed Project", project.Name, "control-character rename preserves value");
            Equal(savedVersion, project.ChangeVersion, "control-character rename preserves revision");
            Equal(savedUpdated, project.UpdatedUtc, "control-character rename preserves timestamp");
            False(stamp.RequiresSave(project), "control-character rename preserves save state");

            var snapshot = ProjectStateSnapshot.Capture(project);
            project.Name = "Temporary Name";
            Equal(savedVersion + 1L, project.ChangeVersion, "temporary rename revision");
            snapshot.Restore(project);
            Equal("Renamed Project", project.Name, "snapshot restored name");
            Equal(savedVersion, project.ChangeVersion, "snapshot restored revision");
            Equal(savedUpdated, project.UpdatedUtc, "snapshot restored timestamp");

            OverflowRenameIsAtomic();
        }

        private static void OverflowRenameIsAtomic()
        {
            var project = AtVersion(new ProjectState("P-NAME-OVERFLOW", "Overflow Original"), long.MaxValue);
            var beforeName = project.Name;
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = project.UpdatedUtc;

            project.Name = " Overflow Original ";
            Equal(beforeName, project.Name, "overflow canonical-equivalent name");
            Equal(beforeVersion, project.ChangeVersion, "overflow canonical-equivalent revision");
            Equal(beforeUpdated, project.UpdatedUtc, "overflow canonical-equivalent timestamp");

            Throws<OverflowException>(() => project.Name = "Overflow Changed");
            Equal(beforeName, project.Name, "overflow failed rename preserves name");
            Equal(beforeVersion, project.ChangeVersion, "overflow failed rename preserves revision");
            Equal(beforeUpdated, project.UpdatedUtc, "overflow failed rename preserves timestamp");
        }

        private static ProjectState AtVersion(ProjectState source, long version)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-project-name-overflow-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(source, path);
                var document = XDocument.Load(path);
                var root = document.Root ?? throw new Exception("ProjectNameFreshnessSmoke fixture has no root element.");
                root.SetAttributeValue("changeVersion", version.ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);
                return store.Load(path);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { }
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
