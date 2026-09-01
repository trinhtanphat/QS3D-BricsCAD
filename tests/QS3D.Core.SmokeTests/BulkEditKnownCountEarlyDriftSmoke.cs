using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditKnownCountEarlyDriftSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectObjectTargetOverrunBeforeNullValidation();
            RejectIdTargetOverrunBeforeIdValidation();
            RejectObjectTargetUnderYieldAfterTraversal();
            RejectIdTargetUnderYieldAfterTraversal();
            PreserveHonestCountedObjectTargets();
            PreserveHonestCountedIdTargets();
        }

        private static void RejectObjectTargetOverrunBeforeNullValidation()
        {
            var project = BuildProject(out var element);
            var source = new DishonestCountCollection<ProjectElement>(1, new ProjectElement[] { element, null! });
            var beforeVersion = project.ChangeVersion;

            ExpectCountDrift(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "Bulk edit target collection input count changed during enumeration.");

            Equal(2, source.MoveNextCalls, "object overrun traversal stop");
            Equal(1, source.CurrentReads, "object overrun Current reads");
            Equal(beforeVersion, project.ChangeVersion, "object overrun project version");
            False(element.Properties.ContainsKey("Note"), "object overrun mutation");
        }

        private static void RejectIdTargetOverrunBeforeIdValidation()
        {
            var project = BuildProject(out var element);
            var source = new DishonestCountCollection<string>(1, new[] { "E-1", "   " });
            var beforeVersion = project.ChangeVersion;

            ExpectCountDrift(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "Bulk edit target list input count changed during enumeration.");

            Equal(2, source.MoveNextCalls, "id overrun traversal stop");
            Equal(1, source.CurrentReads, "id overrun Current reads");
            Equal(beforeVersion, project.ChangeVersion, "id overrun project version");
            False(element.Properties.ContainsKey("Note"), "id overrun mutation");
        }

        private static void RejectObjectTargetUnderYieldAfterTraversal()
        {
            var project = BuildProject(out var element);
            var source = new DishonestCountCollection<ProjectElement>(2, new[] { element });
            var beforeVersion = project.ChangeVersion;

            ExpectCountDrift(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "Bulk edit target collection input count changed during enumeration.");

            Equal(2, source.MoveNextCalls, "object under-yield completed traversal");
            Equal(1, source.CurrentReads, "object under-yield Current reads");
            Equal(beforeVersion, project.ChangeVersion, "object under-yield project version");
            False(element.Properties.ContainsKey("Note"), "object under-yield mutation");
        }

        private static void RejectIdTargetUnderYieldAfterTraversal()
        {
            var project = BuildProject(out var element);
            var source = new DishonestCountCollection<string>(2, new[] { "E-1" });
            var beforeVersion = project.ChangeVersion;

            ExpectCountDrift(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "Bulk edit target list input count changed during enumeration.");

            Equal(2, source.MoveNextCalls, "id under-yield completed traversal");
            Equal(1, source.CurrentReads, "id under-yield Current reads");
            Equal(beforeVersion, project.ChangeVersion, "id under-yield project version");
            False(element.Properties.ContainsKey("Note"), "id under-yield mutation");
        }

        private static void PreserveHonestCountedObjectTargets()
        {
            var project = BuildProject(out var element);
            var source = new DishonestCountCollection<ProjectElement>(1, new[] { element });

            var changed = new BulkEditService().SetProperty(project, source, "Note", "object-ok");

            Equal(1, changed.Count, "honest object changed count");
            Equal(1, source.CurrentReads, "honest object Current reads");
            Equal("object-ok", element.Properties["Note"], "honest object value");
        }

        private static void PreserveHonestCountedIdTargets()
        {
            var project = BuildProject(out var element);
            var source = new DishonestCountCollection<string>(1, new[] { "E-1" });

            var changed = new BulkEditService().SetProperty(project, source, "Note", "id-ok");

            Equal(1, changed, "honest id changed count");
            Equal(1, source.CurrentReads, "honest id Current reads");
            Equal("id-ok", element.Properties["Note"], "honest id value");
        }

        private static ProjectState BuildProject(out ProjectElement element)
        {
            var project = new ProjectState("bulk-known-count", "Bulk Known Count");
            element = new ProjectElement("E-1", ElementCategory.Room);
            project.Elements.Add(element);
            return project;
        }

        private static void ExpectCountDrift(Action action, string expectedMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (string.Equals(expectedMessage, ex.Message, StringComparison.Ordinal)) return;
                throw new Exception("BulkEditKnownCountEarlyDriftSmoke expected diagnostic '" + expectedMessage + "' but got '" + ex.Message + "'.");
            }
            throw new Exception("BulkEditKnownCountEarlyDriftSmoke expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BulkEditKnownCountEarlyDriftSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("BulkEditKnownCountEarlyDriftSmoke expected false: " + label + ".");
        }

        private sealed class DishonestCountCollection<T> : IReadOnlyCollection<T>
        {
            private readonly IReadOnlyList<T> _items;

            public DishonestCountCollection(int reportedCount, IReadOnlyList<T> items)
            {
                Count = reportedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly DishonestCountCollection<T> _owner;
                private int _index = -1;

                public ProbeEnumerator(DishonestCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._items.Count;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current!;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
