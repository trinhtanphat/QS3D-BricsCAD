using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceStampSchemaVersionSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SchemaVersionOnlyChangeIsDirty();
            MarkSavedRefreshesSchemaVersion();
            OrdinaryCleanAndScalarDirtyBehaviorRemainsIntact();
            ConstructorRejectsRevisionDriftDuringTraversal();
            RequiresSaveRejectsRevisionDriftDuringTraversal();
            FailedMarkSavedDoesNotPublishMixedRevisionState();
        }

        private static void SchemaVersionOnlyChangeIsDirty()
        {
            var project = NewProject("schema-only");
            var stamp = new ProjectPersistenceStamp(project);
            var originalChangeVersion = project.ChangeVersion;

            project.SchemaVersion = ProjectState.CurrentSchemaVersion - 1;

            Require(project.ChangeVersion == originalChangeVersion,
                "schema-version mutation unexpectedly changed ChangeVersion, so the regression no longer isolates stamp coverage");
            Require(stamp.RequiresSave(project),
                "schema-version-only mutation was not detected as persisted dirty state");
        }

        private static void MarkSavedRefreshesSchemaVersion()
        {
            var project = NewProject("mark-saved");
            var stamp = new ProjectPersistenceStamp(project);

            project.SchemaVersion = ProjectState.CurrentSchemaVersion - 1;
            Require(stamp.RequiresSave(project), "precondition: legacy schema value should be dirty");

            stamp.MarkSaved(project);
            Require(!stamp.RequiresSave(project), "MarkSaved did not refresh the saved schema version");

            project.SchemaVersion = ProjectState.CurrentSchemaVersion;
            Require(stamp.RequiresSave(project), "subsequent schema-version change was not detected");
        }

        private static void OrdinaryCleanAndScalarDirtyBehaviorRemainsIntact()
        {
            var project = NewProject("ordinary-control");
            var stamp = new ProjectPersistenceStamp(project);

            Require(!stamp.RequiresSave(project), "fresh persistence stamp should be clean");

            project.DrawingFingerprint = "fingerprint-v2";
            Require(stamp.RequiresSave(project), "ordinary persisted scalar mutation stopped being detected");

            stamp.MarkSaved(project);
            Require(!stamp.RequiresSave(project), "ordinary MarkSaved clean control regressed");
        }

        private static void ConstructorRejectsRevisionDriftDuringTraversal()
        {
            var project = NewProject("constructor-drift");
            var stableZones = CopyZones(project);
            ReplaceZones(project, new MutatingList<ZoneDefinition>(stableZones, () =>
            {
                project.DrawingFingerprint = "constructor-drift";
            }));

            RequireThrows<InvalidOperationException>(
                () => new ProjectPersistenceStamp(project),
                "constructor accepted mixed-revision persisted content");
        }

        private static void RequiresSaveRejectsRevisionDriftDuringTraversal()
        {
            var project = NewProject("requires-save-drift");
            var stamp = new ProjectPersistenceStamp(project);
            var stableZones = CopyZones(project);
            ReplaceZones(project, new MutatingList<ZoneDefinition>(stableZones, () =>
            {
                project.DrawingFingerprint = "requires-save-drift";
            }));

            RequireThrows<InvalidOperationException>(
                () => stamp.RequiresSave(project),
                "RequiresSave accepted mixed-revision persisted content");
        }

        private static void FailedMarkSavedDoesNotPublishMixedRevisionState()
        {
            var project = NewProject("mark-saved-drift");
            var stamp = new ProjectPersistenceStamp(project);
            project.DrawingFingerprint = "dirty-before-mark";

            var stableZones = CopyZones(project);
            ReplaceZones(project, new MutatingList<ZoneDefinition>(stableZones, () =>
            {
                project.ActiveZoneId = "zone-a";
            }));

            RequireThrows<InvalidOperationException>(
                () => stamp.MarkSaved(project),
                "MarkSaved accepted mixed-revision persisted content");

            ReplaceZones(project, stableZones);
            Require(stamp.RequiresSave(project),
                "failed MarkSaved partially published mixed-revision saved state");
        }

        private static ProjectState NewProject(string id)
        {
            var project = new ProjectState(id, "Persistence stamp schema-version smoke");
            project.Zones.Add(new ZoneDefinition("zone-a", "Zone A"));
            project.Zones.Add(new ZoneDefinition("zone-b", "Zone B"));
            return project;
        }

        private static List<ZoneDefinition> CopyZones(ProjectState project)
        {
            var result = new List<ZoneDefinition>(project.Zones.Count);
            foreach (var zone in project.Zones) result.Add(zone);
            return result;
        }

        private static void ReplaceZones(ProjectState project, IList<ZoneDefinition> zones)
        {
            var field = typeof(ProjectState).GetField(
                "<Zones>k__BackingField",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
                throw new InvalidOperationException("ProjectPersistenceStampSchemaVersionSmoke: Zones backing field was not found.");
            field.SetValue(project, zones);
        }

        private static void RequireThrows<TException>(Action action, string message)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("ProjectPersistenceStampSchemaVersionSmoke: " + message + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectPersistenceStampSchemaVersionSmoke: " + message + ".");
        }

        private sealed class MutatingList<T> : IList<T>
        {
            private readonly IList<T> _items;
            private readonly Action _mutation;
            private bool _mutated;

            public MutatingList(IList<T> items, Action mutation)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _mutation = mutation ?? throw new ArgumentNullException(nameof(mutation));
            }

            public T this[int index]
            {
                get => _items[index];
                set => _items[index] = value;
            }

            public int Count => _items.Count;
            public bool IsReadOnly => _items.IsReadOnly;
            public void Add(T item) => _items.Add(item);
            public void Clear() => _items.Clear();
            public bool Contains(T item) => _items.Contains(item);
            public void CopyTo(T[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public int IndexOf(T item) => _items.IndexOf(item);
            public void Insert(int index, T item) => _items.Insert(index, item);
            public bool Remove(T item) => _items.Remove(item);
            public void RemoveAt(int index) => _items.RemoveAt(index);

            public IEnumerator<T> GetEnumerator()
            {
                for (var index = 0; index < _items.Count; index++)
                {
                    yield return _items[index];
                    if (index == 0 && !_mutated)
                    {
                        _mutated = true;
                        _mutation();
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
