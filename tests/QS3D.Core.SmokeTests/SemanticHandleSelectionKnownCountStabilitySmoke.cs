using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticHandleSelectionKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            GenericCountDriftRejects();
            ReadOnlyCountDriftRejects();
            NonGenericCountDriftRejects();
            NegativePostTraversalCountRejects();
            ConflictingPostTraversalCountsReject();
            CountOverrunRejectsBeforeSecondCurrent();
            CountUnderYieldRejects();
            MoveNextTransientGrowthRejectsBeforeCurrent();
            MoveNextTransientShrinkRejectsBeforeCurrent();
            MoveNextTransientNegativeRejectsBeforeCurrent();
            MoveNextTransientConflictRejectsBeforeCurrent();
            StableCountedSelectionResolves();
            StableMultiInterfaceSelectionResolves();
            PureStreamingSelectionResolves();
        }

        private static void GenericCountDriftRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new GenericDriftCollection("A", 1, 2)), "changed during traversal");
        }

        private static void ReadOnlyCountDriftRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new ReadOnlyDriftCollection("A", 1, 2)), "changed during traversal");
        }

        private static void NonGenericCountDriftRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new NonGenericDriftCollection("A", 1, 2)), "changed during traversal");
        }

        private static void NegativePostTraversalCountRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new GenericDriftCollection("A", 1, -1)), "negative known Count value during traversal");
        }

        private static void ConflictingPostTraversalCountsReject()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new ConflictingAfterTraversalCollection("A")), "conflicting known Count values during traversal");
        }

        private static void CountOverrunRejectsBeforeSecondCurrent()
        {
            var project = CreateProject(out _);
            var input = new TrackingFixedCountEnumerable(1, "A", "UNOWNED");
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, input), "does not match completed traversal cardinality");
            Require(input.MoveNextCalls == 2,
                "Known-Count overrun should require exactly the second MoveNext attempt.");
            Require(input.CurrentReads == 1,
                "Known-Count overrun must reject before reading the second Current value.");
        }

        private static void CountUnderYieldRejects()
        {
            var project = CreateProject(out _);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(
                project, new FixedCountEnumerable(2, "A")), "does not match completed traversal cardinality");
        }

        private static void MoveNextTransientGrowthRejectsBeforeCurrent() =>
            MoveNextTransientRejectsBeforeCurrent(TransientCountMode.Growth, "changed during traversal");

        private static void MoveNextTransientShrinkRejectsBeforeCurrent() =>
            MoveNextTransientRejectsBeforeCurrent(TransientCountMode.Shrink, "changed during traversal");

        private static void MoveNextTransientNegativeRejectsBeforeCurrent() =>
            MoveNextTransientRejectsBeforeCurrent(TransientCountMode.Negative, "negative known Count value during traversal");

        private static void MoveNextTransientConflictRejectsBeforeCurrent() =>
            MoveNextTransientRejectsBeforeCurrent(TransientCountMode.Conflict, "conflicting known Count values during traversal");

        private static void MoveNextTransientRejectsBeforeCurrent(TransientCountMode mode, string expectedFragment)
        {
            var project = CreateProject(out _);
            var input = new MoveNextTransientCountCollection("A", mode);
            ThrowsCountIntegrity(() => SemanticHandleOwnershipResolver.Resolve(project, input), expectedFragment);
            Require(input.MoveNextCalls == 1,
                "Transient Count rejection should occur immediately after the first successful MoveNext.");
            Require(input.CurrentReads == 0,
                "Transient Count rejection must occur before semantic handle Current is read.");
        }

        private static void StableCountedSelectionResolves()
        {
            var project = CreateProject(out var element);
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, new List<string> { "A" });
            Require(resolved.Count == 1 && ReferenceEquals(resolved[0], element),
                "Stable counted semantic handle selection did not resolve the expected owner.");
        }

        private static void StableMultiInterfaceSelectionResolves()
        {
            var project = CreateProject(out var element);
            var input = new MoveNextTransientCountCollection("A", TransientCountMode.Stable);
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, input);
            Require(resolved.Count == 1 && ReferenceEquals(resolved[0], element),
                "Stable multi-interface counted semantic handle selection did not resolve the expected owner.");
            Require(input.CurrentReads == 1,
                "Stable multi-interface control should read exactly one semantic handle Current value.");
        }

        private static void PureStreamingSelectionResolves()
        {
            var project = CreateProject(out var element);
            var resolved = SemanticHandleOwnershipResolver.Resolve(project, Streaming("A"));
            Require(resolved.Count == 1 && ReferenceEquals(resolved[0], element),
                "Pure streaming semantic handle selection did not preserve supported behavior.");
        }

        private static ProjectState CreateProject(out ProjectElement element)
        {
            var project = new ProjectState("HANDLE-COUNT", "Semantic handle Count stability");
            element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return project;
        }

        private static IEnumerable<string> Streaming(string value)
        {
            yield return value;
        }

        private static void ThrowsCountIntegrity(Action action, string expectedFragment)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedFragment, StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException(
                        "Unexpected semantic handle Count-integrity error: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException(
                "Expected semantic handle Count-integrity rejection containing: " + expectedFragment);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class GenericDriftCollection : ICollection<string>
        {
            private readonly string _value;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            internal GenericDriftCollection(string value, int before, int after)
            {
                _value = value;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsReadOnly => true;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => string.Equals(item, _value, StringComparison.Ordinal);
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class ReadOnlyDriftCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            internal ReadOnlyDriftCollection(string value, int before, int after)
            {
                _value = value;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class NonGenericDriftCollection : IEnumerable<string>, ICollection
        {
            private readonly string _value;
            private readonly int _before;
            private readonly int _after;
            private bool _completed;

            internal NonGenericDriftCollection(string value, int before, int after)
            {
                _value = value;
                _before = before;
                _after = after;
            }

            public int Count => _completed ? _after : _before;
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => array.SetValue(_value, index);
        }

        private sealed class ConflictingAfterTraversalCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _completed;

            internal ConflictingAfterTraversalCollection(string value) => _value = value;

            public int Count => 1;
            int IReadOnlyCollection<string>.Count => _completed ? 2 : 1;
            public bool IsReadOnly => true;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
                _completed = true;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => string.Equals(item, _value, StringComparison.Ordinal);
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }

        private sealed class FixedCountEnumerable : IReadOnlyCollection<string>
        {
            private readonly int _count;
            private readonly string[] _values;

            internal FixedCountEnumerable(int count, params string[] values)
            {
                _count = count;
                _values = values;
            }

            public int Count => _count;
            public IEnumerator<string> GetEnumerator()
            {
                for (var i = 0; i < _values.Length; i++)
                    yield return _values[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class TrackingFixedCountEnumerable : IReadOnlyCollection<string>
        {
            private readonly string[] _values;

            internal TrackingFixedCountEnumerable(int count, params string[] values)
            {
                Count = count;
                _values = values;
            }

            public int Count { get; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly TrackingFixedCountEnumerable _owner;
                private int _index = -1;

                internal Enumerator(TrackingFixedCountEnumerable owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._values.Length;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._values[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private enum TransientCountMode
        {
            Stable,
            Growth,
            Shrink,
            Negative,
            Conflict
        }

        private sealed class MoveNextTransientCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string _value;
            private readonly TransientCountMode _mode;
            private bool _transient;

            internal MoveNextTransientCountCollection(string value, TransientCountMode mode)
            {
                _value = value;
                _mode = mode;
            }

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }

            public int Count => ObservedCount(primary: true);
            int IReadOnlyCollection<string>.Count => ObservedCount(primary: false);
            int ICollection.Count => ObservedCount(primary: true);
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            private int ObservedCount(bool primary)
            {
                if (!_transient || _mode == TransientCountMode.Stable) return 1;
                switch (_mode)
                {
                    case TransientCountMode.Growth: return 2;
                    case TransientCountMode.Shrink: return 0;
                    case TransientCountMode.Negative: return -1;
                    case TransientCountMode.Conflict: return primary ? 1 : 2;
                    default: return 1;
                }
            }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public bool Contains(string item) => string.Equals(item, _value, StringComparison.Ordinal);
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_value, index);
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly MoveNextTransientCountCollection _owner;
                private bool _moved;

                internal Enumerator(MoveNextTransientCountCollection owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    if (_owner._mode != TransientCountMode.Stable)
                        _owner._transient = true;
                    return true;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        var value = _owner._value;
                        _owner._transient = false;
                        return value;
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
