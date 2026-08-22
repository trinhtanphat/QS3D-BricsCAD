using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyPropertyValuePersistabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("FAMILY-VALUE-PERSIST", "Family value persistability");
            var family = ProjectFamilyService.Create(project, "F1", "Wall family", ElementCategory.ArchitecturalWall);
            var member = new ProjectElement(
                "E1",
                ElementCategory.ArchitecturalWall,
                family.Id,
                string.Empty,
                string.Empty);
            project.Elements.Add(member);

            const string key = "Description";
            const string validValue = "  line 1\nline 2\t  ";
            ProjectFamilyService.SetProperty(project, family.Id, key, validValue);

            Equal(validValue, family.Properties[key], "Family property value");
            Equal(validValue, member.Properties[key], "Inherited member property value");

            var version = project.ChangeVersion;
            var updatedUtc = project.UpdatedUtc;
            var familyCount = family.Properties.Count;
            var memberCount = member.Properties.Count;

            var rejected = false;
            try
            {
                ProjectFamilyService.SetProperty(project, family.Id, key, "bad\u0001value");
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            if (!rejected)
                throw new InvalidOperationException("Family SetProperty accepted XML-illegal value text.");
            Equal(version, project.ChangeVersion, "Rejected Family property semantic revision");
            Equal(updatedUtc, project.UpdatedUtc, "Rejected Family property project timestamp");
            Equal(familyCount, family.Properties.Count, "Rejected Family property count");
            Equal(memberCount, member.Properties.Count, "Rejected inherited member property count");
            Equal(validValue, family.Properties[key], "Rejected Family property retained value");
            Equal(validValue, member.Properties[key], "Rejected inherited member retained value");

            var directory = Path.Combine(Path.GetTempPath(), "qs3d-family-value-persistability-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "project.qsdb");
            Directory.CreateDirectory(directory);
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(project, path);
                var loaded = store.Load(path);
                if (loaded.Families.Count != 1 || loaded.Elements.Count != 1)
                    throw new InvalidOperationException("Family property persistability fixture did not round-trip.");
                Equal(validValue, loaded.Families[0].Properties[key], "Loaded Family property value");
                Equal(validValue, loaded.Elements[0].Properties[key], "Loaded inherited member property value");
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " mismatch. Expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void Equal(long expected, long actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void Equal(int expected, int actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(label + " changed unexpectedly.");
        }

        private static void Equal(DateTime expected, DateTime actual, string label)
        {
            if (expected != actual) throw new InvalidOperationException(label + " changed unexpectedly.");
        }
    }
}
