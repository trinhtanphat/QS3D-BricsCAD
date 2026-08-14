using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class QsdbRelationIdentityCanonicalSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsPaddedProjectRelations();
            RejectsPaddedElementRelations();
            AllowsEmptyOptionalRelations();
        }

        private static void RejectsPaddedProjectRelations()
        {
            RejectsWithoutMutation(project => project.ActiveFloorId = " F1 ", project => project.ActiveFloorId, "_activeFloorId", " F1 ", "F1");
            RejectsWithoutMutation(project => project.ActiveZoneId = " Z1 ", project => project.ActiveZoneId, "_activeZoneId", " Z1 ", "Z1");
            RejectsWithoutMutation(project => project.ActiveFloorId = "   ", project => project.ActiveFloorId, "_activeFloorId", "   ", string.Empty);
        }

        private static void RejectsPaddedElementRelations()
        {
            RejectsElementWithoutMutation(element => element.FamilyId = " FAM ", element => element.FamilyId, "_familyId", " FAM ", "FAM");
            RejectsElementWithoutMutation(element => element.FloorId = " F1 ", element => element.FloorId, "_floorId", " F1 ", "F1");
            RejectsElementWithoutMutation(element => element.ZoneId = " Z1 ", element => element.ZoneId, "_zoneId", " Z1 ", "Z1");
        }

        private static void AllowsEmptyOptionalRelations()
        {
            WithPath(path =>
            {
                var project = NewProject();
                project.ActiveFloorId = string.Empty;
                project.ActiveZoneId = string.Empty;
                var element = project.Elements[0];
                element.FamilyId = string.Empty;
                element.FloorId = string.Empty;
                element.ZoneId = string.Empty;

                var store = new QsdbProjectStore();
                store.Save(project, path);
                var loaded = store.Load(path);
                Require(loaded.ActiveFloorId.Length == 0 && loaded.ActiveZoneId.Length == 0, "empty project relations did not roundtrip");
                var loadedElement = loaded.FindElement(element.Id) ?? throw new InvalidOperationException("missing element after roundtrip");
                Require(loadedElement.FamilyId.Length == 0 && loadedElement.FloorId.Length == 0 && loadedElement.ZoneId.Length == 0,
                    "empty element relations did not roundtrip");
            });
        }

        private static void RejectsWithoutMutation(
            Action<ProjectState> mutate,
            Func<ProjectState, string> read,
            string backingField,
            string rawValue,
            string expectedCanonical)
        {
            WithPath(path =>
            {
                var project = NewProject();
                mutate(project);
                Require(string.Equals(read(project), expectedCanonical, StringComparison.Ordinal), "public project relation setter did not canonicalize before raw boundary injection");
                SetRawRelation(project, backingField, rawValue);
                Require(string.Equals(read(project), rawValue, StringComparison.Ordinal), "raw project relation boundary injection did not reach state");
                var beforeUpdated = project.UpdatedUtc;
                Throws<InvalidDataException>(() => new QsdbProjectStore().Save(project, path));
                Require(string.Equals(read(project), rawValue, StringComparison.Ordinal), "failed Save normalized a project relation in memory");
                Require(project.UpdatedUtc == beforeUpdated, "failed validation touched project timestamp");
            });
        }

        private static void RejectsElementWithoutMutation(
            Action<ProjectElement> mutate,
            Func<ProjectElement, string> read,
            string backingField,
            string rawValue,
            string expectedCanonical)
        {
            WithPath(path =>
            {
                var project = NewProject();
                var element = project.Elements[0];
                mutate(element);
                Require(string.Equals(read(element), expectedCanonical, StringComparison.Ordinal), "public element relation setter did not canonicalize before raw boundary injection");
                SetRawRelation(element, backingField, rawValue);
                Require(string.Equals(read(element), rawValue, StringComparison.Ordinal), "raw element relation boundary injection did not reach state");
                var beforeProjectUpdated = project.UpdatedUtc;
                var beforeElementUpdated = element.UpdatedUtc;
                Throws<InvalidDataException>(() => new QsdbProjectStore().Save(project, path));
                Require(string.Equals(read(element), rawValue, StringComparison.Ordinal), "failed Save normalized an element relation in memory");
                Require(project.UpdatedUtc == beforeProjectUpdated, "failed validation touched project timestamp");
                Require(element.UpdatedUtc == beforeElementUpdated, "failed validation touched element timestamp");
            });
        }

        private static void SetRawRelation(object target, string backingField, string value)
        {
            var field = target.GetType().GetField(backingField, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null || field.FieldType != typeof(string))
                throw new InvalidOperationException("QsdbRelationIdentityCanonicalSmoke could not resolve raw relation backing field: " + backingField + ".");
            field.SetValue(target, value);
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("P-rel", "Relation identity");
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Beam, string.Empty, string.Empty, string.Empty));
            return project;
        }

        private static void WithPath(Action<string> action)
        {
            var dir = Path.Combine(Path.GetTempPath(), "qs3d-rel-id-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try { action(Path.Combine(dir, "project.qsdb")); }
            finally
            {
                try { Directory.Delete(dir, true); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("QsdbRelationIdentityCanonicalSmoke expected " + typeof(T).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("QsdbRelationIdentityCanonicalSmoke: " + message);
        }
    }
}
