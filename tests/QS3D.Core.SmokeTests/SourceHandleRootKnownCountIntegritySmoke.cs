using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleRootKnownCountIntegritySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            OverrunRejectsBeforeUnexpectedCurrent();
            UnderYieldRejects();
            GenericCountDriftRejects();
            ReadOnlyCountDriftRejects();
            NonGenericCountDriftRejects();
            NegativeAdmissionRejectsBeforeEnumeration();
            ConflictingAdmissionRejectsBeforeEnumeration();
            NegativePostTraversalCountRejects();
            ConflictingPostTraversalCountsReject();
            StableMultiInterfaceCountResolves();
            CanonicalValidationStillWinsInsideAdmittedCount();
            PureStreamingInputResolves();
        }

        private static void OverrunRejectsBeforeUnexpectedCurrent()
        {
            var source = new CurrentTrackingCollection(1, "E1", "UNEXPECTED");
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source),
                "known Count does not match completed traversal cardinality");
            Require(source.CurrentReads == 1,
                "Locate root Count overrun observed Current for an entry beyond the admitted Count.");
            Require(source.MoveNextCalls == 2,
                "Locate root Count overrun must detect the first unexpected successful MoveNext.");
        }

        private static void UnderYieldRejects()
        {
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(
                CreateProject(), new FixedCountEnumerable(2, "E1")),
                "known Count does not match completed traversal cardinality");
        }

        private static void GenericCountDriftRejects()
        {
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(
                CreateProject(), new GenericDriftCollection("E1", 1, 2)),
                "known Count changed during traversal");
        }

        private static void ReadOnlyCountDriftRejects()
        {
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(
                CreateProject(), new ReadOnlyDriftCollection("E1", 1, 2)),
                "known Count changed during traversal");
        }

        private static void NonGenericCountDriftRejects()
        {
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(
                CreateProject(), new NonGenericDriftCollection("E1", 1, 2)),
                "known Count changed during traversal");
        }

        private static void NegativeAdmissionRejectsBeforeEnumeration()
        {
            var source = new CurrentTrackingCollection(-1, "E1");
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source),
                "invalid negative known Count value");
            Require(source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Negative Locate root Count evidence must reject before enumeration.");
        }

        private static void ConflictingAdmissionRejectsBeforeEnumeration()
        {
            var source = new ConflictingCountCollection("E1", false);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source),
                "conflicting known Count values");
            Require(source.MoveNextCalls == 0,
                "Conflicting Locate root Count evidence must reject before enumeration.");
        }

        private static void NegativePostTraversalCountRejects()
        {
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(
                CreateProject(), new GenericDriftCollection("E1", 1, -1)),
                "negative known Count value after traversal");
        }

        private static void ConflictingPostTraversalCountsReject()
        {
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(
                CreateProject(), new ConflictingCountCollection("E1", true)),
                "conflicting known Count values after traversal");
        }

        private static void StableMultiInterfaceCountResolves()
        {
            var handles = SourceHandleResolver.Resolve(CreateProject(), new StableMultiCountCollection("E1"));
            Require(handles.Count == 1 && string.Equals(handles[0], "A", StringComparison.Ordinal),
                "Stable multi-interface Locate root Count input did not preserve expected resolution.");
        }

        private static void CanonicalValidationStillWinsInsideAdmittedCount()
        {
            try
            {
                SourceHandleResolver.Resolve(CreateProject(), new FixedCountEnumerable(1, " E1 "));
            }
            catch (InvalidOperationException ex)
            {
                Require(ex.Message.IndexOf("non-canonical semantic element id", StringComparison.Ordinal) >= 0,
                    "Canonical Locate root validation precedence changed inside admitted Count.");
                return;
            }
            throw new InvalidOperationException("Expected non-canonical Locate root id rejection.");
        }

        private static void PureStreamingInputResolves()
        {
            var handles = SourceHandleResolver.Resolve(CreateProject(), Streaming("E1"));
            Require(handles.Count == 1 && string.Equals(handles[0], "A", StringComparison.Ordinal),
                "Pure streaming Locate root input did not preserve expected resolution.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("LOCATE-ROOT-COUNT", "Locate root Count integrity");
            var element = new ProjectElement("E1", ElementCategory.CustomQuantity);
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
                    throw new InvalidOperationException("Unexpected Locate root Count-integrity error: " + ex.Message, ex);
                return;
            }
            throw new InvalidOperationException("Expected Locate root Count-integrity rejection containing: " + expectedFragment);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CurrentTrackingCollection : IReadOnlyCollection<string>
        {
            private readonly int _count;
            private readonly string[] _values;

            internal CurrentTrackingCollection(int count, params string[] values)
            {
                _count = count;
                _values = values;
            }

            public int Count => _count;
            internal int CurrentReads { get; private set; }
            internal int MoveNextCalls { get; private set; }
            public IEnumerator<string> GetEnumerator() => new TrackingEnumerator(this, _values);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class TrackingEnumerator : IEnumerator<string>
            {
                private readonly CurrentTrackingCollection _owner;
                private readonly string[] _values;
                private int _index = -1;

                internal TrackingEnumerator(CurrentTrackingCollection owner, string[] values)
                {
                    _owner = owner;
                    _values = values;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _values[_index];
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _values.Length;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
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
                for (var i = 0; i < _values.Length; i++) yield return _values[i];
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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

        private sealed class ConflictingCountCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly bool _conflictAfterTraversal;
            private bool _completed;
            internal int MoveNextCalls { get; private set; }
            internal ConflictingCountCollection(string value, bool conflictAfterTraversal)
            {
                _value = value;
                _conflictAfterTraversal = conflictAfterTraversal;
            }
            public int Count => 1;
            int IReadOnlyCollection<string>.Count => _conflictAfterTraversal && !_completed ? 1 : 2;
            public bool IsReadOnly => true;
            public IEnumerator<string> GetEnumerator()
            {
                MoveNextCalls++;
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

        private sealed class StableMultiCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string _value;
            internal StableMultiCountCollection(string value) => _value = value;
            public int Count => 1;
            int IReadOnlyCollection<string>.Count => 1;
            int ICollection.Count => 1;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            public IEnumerator<string> GetEnumerator()
            {
                yield return _value;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => string.Equals(item, _value, StringComparison.Ordinal);
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            void ICollection.CopyTo(Array array, int index) => array.SetValue(_value, index);
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }
}
