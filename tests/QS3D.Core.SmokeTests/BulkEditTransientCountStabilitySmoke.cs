using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditTransientCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectObjectTransientGrowthBeforeSecondMoveNext();
            RejectIdTransientShrinkBeforeSecondMoveNext();
            RejectObjectTransientNegativeCountBeforeSecondMoveNext();
            RejectIdTransientConflictingCountsBeforeSecondMoveNext();
            PreserveStableMultiInterfaceObjectTargets();
            PreserveStableMultiInterfaceIdTargets();
        }

        private static void RejectObjectTransientGrowthBeforeSecondMoveNext()
        {
            var project = BuildProject(out var first, out var second);
            var source = new TransientCountCollection<ProjectElement>(new[] { first, second }, TransientCountMode.Grow);
            var beforeVersion = project.ChangeVersion;

            ExpectInvalid(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "input count changed during enumeration",
                "object transient growth");

            AssertStopsBeforeSecondMoveNext(source, "object transient growth");
            Equal(beforeVersion, project.ChangeVersion, "object transient growth project version");
            False(first.Properties.ContainsKey("Note"), "object transient growth mutation");
            False(second.Properties.ContainsKey("Note"), "object transient growth second mutation");
        }

        private static void RejectIdTransientShrinkBeforeSecondMoveNext()
        {
            var project = BuildProject(out var first, out var second);
            var source = new TransientCountCollection<string>(new[] { first.Id, second.Id }, TransientCountMode.Shrink);
            var beforeVersion = project.ChangeVersion;

            ExpectInvalid(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "input count changed during enumeration",
                "id transient shrink");

            AssertStopsBeforeSecondMoveNext(source, "id transient shrink");
            Equal(beforeVersion, project.ChangeVersion, "id transient shrink project version");
            False(first.Properties.ContainsKey("Note"), "id transient shrink mutation");
            False(second.Properties.ContainsKey("Note"), "id transient shrink second mutation");
        }

        private static void RejectObjectTransientNegativeCountBeforeSecondMoveNext()
        {
            var project = BuildProject(out var first, out var second);
            var source = new TransientCountCollection<ProjectElement>(new[] { first, second }, TransientCountMode.Negative);
            var beforeVersion = project.ChangeVersion;

            ExpectInvalid(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "reports an invalid negative input count",
                "object transient negative Count");

            AssertStopsBeforeSecondMoveNext(source, "object transient negative Count");
            Equal(beforeVersion, project.ChangeVersion, "object transient negative project version");
            False(first.Properties.ContainsKey("Note"), "object transient negative mutation");
        }

        private static void RejectIdTransientConflictingCountsBeforeSecondMoveNext()
        {
            var project = BuildProject(out var first, out var second);
            var source = new TransientCountCollection<string>(new[] { first.Id, second.Id }, TransientCountMode.Conflict);
            var beforeVersion = project.ChangeVersion;

            ExpectInvalid(
                () => new BulkEditService().SetProperty(project, source, "Note", "blocked"),
                "reports conflicting known input counts",
                "id transient conflicting Count surfaces");

            AssertStopsBeforeSecondMoveNext(source, "id transient conflicting Count surfaces");
            Equal(beforeVersion, project.ChangeVersion, "id transient conflict project version");
            False(first.Properties.ContainsKey("Note"), "id transient conflict mutation");
        }

        private static void PreserveStableMultiInterfaceObjectTargets()
        {
            var project = BuildProject(out var first, out var second);
            var source = new TransientCountCollection<ProjectElement>(new[] { first, second }, TransientCountMode.None);

            var changed = new BulkEditService().SetProperty(project, source, "Note", "stable-object");

            Equal(2, changed.Count, "stable object changed count");
            Equal(3, source.MoveNextCalls, "stable object MoveNext calls");
            Equal(2, source.CurrentReads, "stable object Current reads");
            Equal("stable-object", first.Properties["Note"], "stable object first value");
            Equal("stable-object", second.Properties["Note"], "stable object second value");
        }

        private static void PreserveStableMultiInterfaceIdTargets()
        {
            var project = BuildProject(out var first, out var second);
            var source = new TransientCountCollection<string>(new[] { first.Id, second.Id }, TransientCountMode.None);

            var changed = new BulkEditService().SetProperty(project, source, "Note", "stable-id");

            Equal(2, changed, "stable id changed count");
            Equal(3, source.MoveNextCalls, "stable id MoveNext calls");
            Equal(2, source.CurrentReads, "stable id Current reads");
            Equal("stable-id", first.Properties["Note"], "stable id first value");
            Equal("stable-id", second.Properties["Note"], "stable id second value");
        }

        private static ProjectState BuildProject(out ProjectElement first, out ProjectElement second)
        {
            var project = new ProjectState("bulk-transient-count", "Bulk Transient Count");
            first = new ProjectElement("E-1", ElementCategory.Room);
            second = new ProjectElement("E-2", ElementCategory.Room);
            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static void AssertStopsBeforeSecondMoveNext<T>(TransientCountCollection<T> source, string label)
        {
            Equal(1, source.MoveNextCalls, label + " MoveNext calls");
            Equal(1, source.CurrentReads, label + " Current reads");
        }

        private static void ExpectInvalid(Action action, string expectedFragment, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.Ordinal) >= 0) return;
                throw new Exception("BulkEditTransientCountStabilitySmoke " + label + " expected diagnostic containing '" + expectedFragment + "' but got '" + ex.Message + "'.");
            }
            throw new Exception("BulkEditTransientCountStabilitySmoke " + label + " expected InvalidOperationException.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("BulkEditTransientCountStabilitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new Exception("BulkEditTransientCountStabilitySmoke expected false: " + label + ".");
        }

        private enum TransientCountMode
        {
            None,
            Grow,
            Shrink,
            Negative,
            Conflict,
        }

        private enum CountSurface
        {
            Generic,
            ReadOnly,
            NonGeneric,
        }

        private sealed class TransientCountCollection<T> : ICollection<T>, IReadOnlyCollection<T>, ICollection
        {
            private readonly IReadOnlyList<T> _items;
            private readonly TransientCountMode _mode;
            private bool _transientArmed;

            public TransientCountCollection(IReadOnlyList<T> items, TransientCountMode mode)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
                _mode = mode;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            int ICollection<T>.Count => ReadCount(CountSurface.Generic);
            int IReadOnlyCollection<T>.Count => ReadCount(CountSurface.ReadOnly);
            int ICollection.Count => ReadCount(CountSurface.NonGeneric);
            bool ICollection<T>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            void ICollection<T>.Add(T item) => throw new NotSupportedException();
            void ICollection<T>.Clear() => throw new NotSupportedException();
            bool ICollection<T>.Contains(T item) => throw new NotSupportedException();
            void ICollection<T>.CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<T>.Remove(T item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();

            private int ReadCount(CountSurface surface)
            {
                if (!_transientArmed || _mode == TransientCountMode.None) return _items.Count;
                switch (_mode)
                {
                    case TransientCountMode.Grow:
                        return _items.Count + 1;
                    case TransientCountMode.Shrink:
                        return _items.Count - 1;
                    case TransientCountMode.Negative:
                        return -1;
                    case TransientCountMode.Conflict:
                        return surface == CountSurface.ReadOnly ? _items.Count + 2 : _items.Count + 1;
                    default:
                        return _items.Count;
                }
            }

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly TransientCountCollection<T> _owner;
                private int _index = -1;

                public ProbeEnumerator(TransientCountCollection<T> owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner._transientArmed) _owner._transientArmed = false;
                    _index++;
                    return _index < _owner._items.Count;
                }

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index == 0 && _owner._mode != TransientCountMode.None) _owner._transientArmed = true;
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
