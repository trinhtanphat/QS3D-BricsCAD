using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleRootTransientKnownCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TransientGrowthRejectsBeforeCurrent();
            TransientShrinkRejectsBeforeCurrent();
            TransientNegativeRejectsBeforeCurrent();
            TransientConflictRejectsBeforeCurrent();
            StableCountStillResolves();
            StreamingInputStillResolves();
        }

        private static void TransientGrowthRejectsBeforeCurrent()
        {
            var source = new TransientReadOnlyCollection("E1", 1, 2);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "known Count changed during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Transient Locate root Count growth must reject after MoveNext and before Current.");
        }

        private static void TransientShrinkRejectsBeforeCurrent()
        {
            var source = new TransientReadOnlyCollection("E1", 1, 0);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "known Count changed during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Transient Locate root Count shrink must reject after MoveNext and before Current.");
        }

        private static void TransientNegativeRejectsBeforeCurrent()
        {
            var source = new TransientReadOnlyCollection("E1", 1, -1);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "invalid negative known Count value during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Transient negative Locate root Count must reject before Current.");
        }

        private static void TransientConflictRejectsBeforeCurrent()
        {
            var source = new TransientConflictingCollection("E1");
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "conflicting known Count values during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Transient conflicting Locate root Count surfaces must reject before Current.");
        }

        private static void StableCountStillResolves()
        {
            var source = new StableReadOnlyCollection("E1");
            var handles = SourceHandleResolver.Resolve(CreateProject(), source);
            Require(handles.Count == 1 && handles[0] == "A" && source.CurrentReads == 1,
                "Stable counted Locate root input must preserve successful resolution.");
        }

        private static void StreamingInputStillResolves()
        {
            var handles = SourceHandleResolver.Resolve(CreateProject(), Streaming("E1"));
            Require(handles.Count == 1 && handles[0] == "A",
                "Pure streaming Locate root input must remain supported.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("LOCATE-ROOT-TRANSIENT-COUNT", "Locate root transient Count stability");
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
                if (ex.Message.IndexOf(expectedFragment, StringComparison.Ordinal) >= 0) return;
                throw new InvalidOperationException("Unexpected transient Locate root Count-integrity error: " + ex.Message, ex);
            }
            throw new InvalidOperationException("Expected transient Locate root Count-integrity rejection containing: " + expectedFragment);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class TransientReadOnlyCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _admittedCount;
            private readonly int _transientCount;
            private bool _afterMoveNext;

            internal TransientReadOnlyCollection(string value, int admittedCount, int transientCount)
            {
                _value = value;
                _admittedCount = admittedCount;
                _transientCount = transientCount;
            }

            public int Count => _afterMoveNext ? _transientCount : _admittedCount;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly TransientReadOnlyCollection _owner;
                private bool _moved;

                internal Enumerator(TransientReadOnlyCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._afterMoveNext = false;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved)
                    {
                        _owner._afterMoveNext = false;
                        return false;
                    }
                    _moved = true;
                    _owner._afterMoveNext = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class TransientConflictingCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _afterMoveNext;

            internal TransientConflictingCollection(string value) => _value = value;

            public int Count => 1;
            int IReadOnlyCollection<string>.Count => _afterMoveNext ? 2 : 1;
            public bool IsReadOnly => true;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => item == _value;
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly TransientConflictingCollection _owner;
                private bool _moved;

                internal Enumerator(TransientConflictingCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._afterMoveNext = false;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved)
                    {
                        _owner._afterMoveNext = false;
                        return false;
                    }
                    _moved = true;
                    _owner._afterMoveNext = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class StableReadOnlyCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            internal StableReadOnlyCollection(string value) => _value = value;
            public int Count => 1;
            internal int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StableReadOnlyCollection _owner;
                private bool _moved;
                internal Enumerator(StableReadOnlyCollection owner) => _owner = owner;
                public string Current { get { _owner.CurrentReads++; return _owner._value; } }
                object IEnumerator.Current => Current;
                public bool MoveNext() { if (_moved) return false; _moved = true; return true; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
