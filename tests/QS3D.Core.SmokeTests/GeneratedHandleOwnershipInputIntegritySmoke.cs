using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GeneratedHandleOwnershipInputIntegritySmoke
    {
        private const int MaxHandleCount = 10000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            KnownCountOverrunRejectsBeforeUnexpectedCurrentOrCallback();
            MoveNextCountGrowthRejectsBeforeCurrentOrCallback();
            MoveNextCountShrinkRejectsBeforeCurrentOrCallback();
            MoveNextNegativeCountRejectsBeforeCurrentOrCallback();
            MoveNextCrossInterfaceConflictRejectsBeforeCurrentOrCallback();
            KnownCountUnderYieldRejectsBeforeCallback();
            StreamingHardCapRejectsBeforeExtraCurrentOrCallback();
            StableCountedInputPreservesSortedValidation();
            PureStreamingInputPreservesSortedValidation();
        }

        private static void KnownCountOverrunRejectsBeforeUnexpectedCurrentOrCallback()
        {
            var source = new HostileHandleCollection(new[] { "A", "B" }, 1, 1, 1);
            var callbacks = 0;
            ThrowsIntegrity(() => Validate(source, _ => callbacks++));
            Equal(1, source.CurrentReads, "known-count overrun Current reads");
            Equal(0, callbacks, "known-count overrun callbacks");
        }

        private static void MoveNextCountGrowthRejectsBeforeCurrentOrCallback()
        {
            var source = new HostileHandleCollection(new[] { "A" }, 1, 1, 1, 1, 2, 2, 2);
            AssertPreCurrentFailure(source, "growth");
        }

        private static void MoveNextCountShrinkRejectsBeforeCurrentOrCallback()
        {
            var source = new HostileHandleCollection(new[] { "A", "B" }, 2, 2, 2, 1, 1, 1, 1);
            AssertPreCurrentFailure(source, "shrink");
        }

        private static void MoveNextNegativeCountRejectsBeforeCurrentOrCallback()
        {
            var source = new HostileHandleCollection(new[] { "A" }, 1, 1, 1, 1, -1, -1, -1);
            AssertPreCurrentFailure(source, "negative Count");
        }

        private static void MoveNextCrossInterfaceConflictRejectsBeforeCurrentOrCallback()
        {
            var source = new HostileHandleCollection(new[] { "A" }, 1, 1, 1, 1, 1, 2, 1);
            AssertPreCurrentFailure(source, "cross-interface conflict");
        }

        private static void KnownCountUnderYieldRejectsBeforeCallback()
        {
            var source = new HostileHandleCollection(new[] { "A" }, 2, 2, 2);
            var callbacks = 0;
            ThrowsIntegrity(() => Validate(source, _ => callbacks++));
            Equal(1, source.CurrentReads, "under-yield Current reads");
            Equal(0, callbacks, "under-yield callbacks");
        }

        private static void StreamingHardCapRejectsBeforeExtraCurrentOrCallback()
        {
            var source = new StreamingHandleSequence(MaxHandleCount + 1);
            var callbacks = 0;
            ThrowsIntegrity(() => Validate(source, _ => callbacks++));
            Equal(MaxHandleCount, source.CurrentReads, "streaming hard-cap Current reads");
            Equal(0, callbacks, "streaming hard-cap callbacks");
        }

        private static void StableCountedInputPreservesSortedValidation()
        {
            var source = new HostileHandleCollection(new[] { "B", "A" }, 2, 2, 2);
            var callbacks = new List<string>();
            var result = Validate(source, callbacks.Add);
            Equal(2, result.Count, "stable counted result Count");
            Equal("A", result[0], "stable counted sorted first handle");
            Equal("B", result[1], "stable counted sorted second handle");
            Equal(2, callbacks.Count, "stable counted callback Count");
            Equal("A", callbacks[0], "stable counted callback first handle");
            Equal("B", callbacks[1], "stable counted callback second handle");
            Equal(2, source.CurrentReads, "stable counted Current reads");
        }

        private static void PureStreamingInputPreservesSortedValidation()
        {
            var source = new StreamingHandleSequence(2, reverseFirstTwo: true);
            var callbacks = new List<string>();
            var result = Validate(source, callbacks.Add);
            Equal(2, result.Count, "streaming result Count");
            Equal("1", result[0], "streaming sorted first handle");
            Equal("2", result[1], "streaming sorted second handle");
            Equal(2, callbacks.Count, "streaming callback Count");
            Equal(2, source.CurrentReads, "streaming Current reads");
        }

        private static void AssertPreCurrentFailure(HostileHandleCollection source, string label)
        {
            var callbacks = 0;
            ThrowsIntegrity(() => Validate(source, _ => callbacks++));
            Equal(0, source.CurrentReads, label + " Current reads");
            Equal(0, callbacks, label + " callbacks");
        }

        private static IReadOnlyList<string> Validate(IEnumerable<string> handles, Action<string> callback)
        {
            var project = new ProjectState("GH-PROJECT", "Generated handle integrity");
            var owner = new ProjectElement("GH-OWNER", ElementCategory.Beam);
            owner.Properties["GeneratedRebarHandles"] = "1;2;A;B";
            project.Elements.Add(owner);
            return GeneratedHandleOwnershipPolicy.ValidateAllBeforeErase(
                project,
                owner,
                "GeneratedRebarHandles",
                handles,
                callback);
        }

        private static void ThrowsIntegrity(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("Expected destructive generated-handle input integrity rejection.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class HostileHandleCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly string[] _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly int _mutateOnMoveNextCall;
            private readonly int _mutatedGenericCount;
            private readonly int _mutatedReadOnlyCount;
            private readonly int _mutatedNonGenericCount;
            private bool _mutated;

            internal HostileHandleCollection(
                string[] items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                int mutateOnMoveNextCall = int.MaxValue,
                int mutatedGenericCount = 0,
                int mutatedReadOnlyCount = 0,
                int mutatedNonGenericCount = 0)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _mutateOnMoveNextCall = mutateOnMoveNextCall;
                _mutatedGenericCount = mutatedGenericCount;
                _mutatedReadOnlyCount = mutatedReadOnlyCount;
                _mutatedNonGenericCount = mutatedNonGenericCount;
            }

            int ICollection<string>.Count => _mutated ? _mutatedGenericCount : _genericCount;
            int IReadOnlyCollection<string>.Count => _mutated ? _mutatedReadOnlyCount : _readOnlyCount;
            int ICollection.Count => _mutated ? _mutatedNonGenericCount : _nonGenericCount;
            bool ICollection<string>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;
            internal int CurrentReads { get; private set; }
            internal int MoveNextCalls { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<string>.Add(string item) => throw new NotSupportedException();
            void ICollection<string>.Clear() => throw new NotSupportedException();
            bool ICollection<string>.Contains(string item) => Array.IndexOf(_items, item) >= 0;
            void ICollection<string>.CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            bool ICollection<string>.Remove(string item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => _items.CopyTo(array, index);

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly HostileHandleCollection _owner;
                private int _index = -1;

                internal Enumerator(HostileHandleCollection owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    if (_owner.MoveNextCalls >= _owner._mutateOnMoveNextCall)
                        _owner._mutated = true;
                    return _index < _owner._items.Length;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._items[_index];
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StreamingHandleSequence : IEnumerable<string>
        {
            private readonly int _count;
            private readonly bool _reverseFirstTwo;

            internal StreamingHandleSequence(int count, bool reverseFirstTwo = false)
            {
                _count = count;
                _reverseFirstTwo = reverseFirstTwo;
            }

            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StreamingHandleSequence _owner;
                private int _index;

                internal Enumerator(StreamingHandleSequence owner) => _owner = owner;

                public bool MoveNext()
                {
                    _index++;
                    return _index <= _owner._count;
                }

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        var value = _owner._reverseFirstTwo && _owner._count == 2 ? 3 - _index : _index;
                        return value.ToString("X", CultureInfo.InvariantCulture);
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
