using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyGraphTransientCountStabilitySmoke
    {
        internal static void Run()
        {
            RebuildRejectsTransientGrowthBeforeCurrent();
            DirtyOrderingRejectsTransientShrinkBeforeCurrent();
            RebuildRejectsTransientNegativeCountBeforeCurrent();
            DirtyOrderingRejectsTransientCrossInterfaceConflictBeforeCurrent();
            StableCountedAndStreamingInputsRemainAccepted();
        }

        private static void RebuildRejectsTransientGrowthBeforeCurrent()
        {
            var source = Hostile(TransientCountMode.Growth, Element("RG"));
            var error = Throws<InvalidOperationException>(() => new DependencyGraph().Rebuild(source));
            Contains(error.Message, "count", "rebuild transient growth diagnostic");
            Equal(1, source.MoveNextCalls, "rebuild transient growth MoveNext count");
            Equal(0, source.CurrentReads, "rebuild transient growth must fail before Current");
        }

        private static void DirtyOrderingRejectsTransientShrinkBeforeCurrent()
        {
            var element = Element("OS");
            element.MarkDirty(ElementDirtyFlags.Geometry);
            var source = Hostile(TransientCountMode.Shrink, element);
            var error = Throws<InvalidOperationException>(() => new DependencyGraph().TopologicalDirtyOrder(source));
            Contains(error.Message, "count", "ordering transient shrink diagnostic");
            Equal(1, source.MoveNextCalls, "ordering transient shrink MoveNext count");
            Equal(0, source.CurrentReads, "ordering transient shrink must fail before Current");
        }

        private static void RebuildRejectsTransientNegativeCountBeforeCurrent()
        {
            var source = Hostile(TransientCountMode.Negative, Element("RN"));
            var error = Throws<InvalidOperationException>(() => new DependencyGraph().Rebuild(source));
            Contains(error.Message, "negative", "rebuild transient negative diagnostic");
            Equal(1, source.MoveNextCalls, "rebuild transient negative MoveNext count");
            Equal(0, source.CurrentReads, "rebuild transient negative must fail before Current");
        }

        private static void DirtyOrderingRejectsTransientCrossInterfaceConflictBeforeCurrent()
        {
            var element = Element("OC");
            element.MarkDirty(ElementDirtyFlags.Geometry);
            var source = Hostile(TransientCountMode.Conflict, element);
            var error = Throws<InvalidOperationException>(() => new DependencyGraph().TopologicalDirtyOrder(source));
            Contains(error.Message, "conflicting", "ordering transient Count conflict diagnostic");
            Equal(1, source.MoveNextCalls, "ordering transient Count conflict MoveNext count");
            Equal(0, source.CurrentReads, "ordering transient Count conflict must fail before Current");
        }

        private static void StableCountedAndStreamingInputsRemainAccepted()
        {
            var first = Element("A");
            var second = Element("B");
            second.DependsOn.Add("A");
            var graph = new DependencyGraph();
            graph.Rebuild(new[] { first, second });
            var dependents = graph.GetDirectDependents("A");
            Equal(1, dependents.Count, "stable counted rebuild dependent count");
            Equal("B", dependents[0], "stable counted rebuild dependent id");

            first.MarkDirty(ElementDirtyFlags.Geometry);
            second.MarkDirty(ElementDirtyFlags.Geometry);
            var ordered = graph.TopologicalDirtyOrder(Streaming(first, second));
            Equal(2, ordered.Count, "pure-streaming ordering count");
            Equal("A", ordered[0].Id, "pure-streaming ordering first id");
            Equal("B", ordered[1].Id, "pure-streaming ordering second id");
        }

        private static ProjectElement Element(string id)
        {
            return new ProjectElement(id, ElementCategory.Room, string.Empty, string.Empty, string.Empty);
        }

        private static TransientMoveNextCountCollection Hostile(TransientCountMode mode, ProjectElement element)
        {
            return new TransientMoveNextCountCollection(mode, element);
        }

        private static IEnumerable<ProjectElement> Streaming(ProjectElement first, ProjectElement second)
        {
            yield return first;
            yield return second;
        }

        private static T Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T error)
            {
                return error;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }

        private static void Contains(string value, string expected, string label)
        {
            if (value == null || value.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new Exception(label + ": expected text containing '" + expected + "', got '" + (value ?? string.Empty) + "'.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private enum TransientCountMode
        {
            Growth,
            Shrink,
            Negative,
            Conflict
        }

        private sealed class TransientMoveNextCountCollection : ICollection<ProjectElement>, IReadOnlyCollection<ProjectElement>
        {
            private readonly TransientCountMode _mode;
            private readonly ProjectElement _element;
            private int _genericCount = 1;
            private int _readOnlyCount = 1;

            internal TransientMoveNextCountCollection(TransientCountMode mode, ProjectElement element)
            {
                _mode = mode;
                _element = element;
            }

            public int Count => _genericCount;
            int IReadOnlyCollection<ProjectElement>.Count => _readOnlyCount;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<ProjectElement> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private void DriftCount()
            {
                switch (_mode)
                {
                    case TransientCountMode.Growth:
                        _genericCount = 2;
                        _readOnlyCount = 2;
                        break;
                    case TransientCountMode.Shrink:
                        _genericCount = 0;
                        _readOnlyCount = 0;
                        break;
                    case TransientCountMode.Negative:
                        _genericCount = -1;
                        _readOnlyCount = -1;
                        break;
                    case TransientCountMode.Conflict:
                        _genericCount = 2;
                        _readOnlyCount = 1;
                        break;
                    default:
                        throw new InvalidOperationException("Unknown transient Count mode.");
                }
            }

            private void RestoreCount()
            {
                _genericCount = 1;
                _readOnlyCount = 1;
            }

            public void Add(ProjectElement item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(ProjectElement item) => throw new NotSupportedException();
            public bool Contains(ProjectElement item) => ReferenceEquals(item, _element);
            public void CopyTo(ProjectElement[] array, int arrayIndex) => array[arrayIndex] = _element;

            private sealed class Enumerator : IEnumerator<ProjectElement>
            {
                private readonly TransientMoveNextCountCollection _owner;
                private bool _yielded;

                internal Enumerator(TransientMoveNextCountCollection owner)
                {
                    _owner = owner;
                }

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_yielded) return false;
                    _yielded = true;
                    _owner.DriftCount();
                    return true;
                }

                public ProjectElement Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner.RestoreCount();
                        return _owner._element;
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class DependencyGraphTransientCountStabilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => DependencyGraphTransientCountStabilitySmoke.Run();
    }
}
