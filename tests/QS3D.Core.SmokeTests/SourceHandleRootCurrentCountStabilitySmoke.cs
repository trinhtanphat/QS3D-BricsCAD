using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleRootCurrentCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            CurrentGrowthRejectsBeforeNextMoveNext();
            CurrentShrinkRejectsBeforeNextMoveNext();
            CurrentNegativeRejectsBeforeNextMoveNext();
            CurrentConflictRejectsBeforeNextMoveNext();
            StableCountStillResolves();
        }

        private static void CurrentGrowthRejectsBeforeNextMoveNext()
        {
            var source = new CurrentDriftReadOnlyCollection("E1", 2);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "known Count changed during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 1,
                "Current-induced Locate root Count growth must reject after Current and before the next MoveNext.");
        }

        private static void CurrentShrinkRejectsBeforeNextMoveNext()
        {
            var source = new CurrentDriftReadOnlyCollection("E1", 0);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "known Count changed during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 1,
                "Current-induced Locate root Count shrink must reject after Current and before the next MoveNext.");
        }

        private static void CurrentNegativeRejectsBeforeNextMoveNext()
        {
            var source = new CurrentDriftReadOnlyCollection("E1", -1);
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "invalid negative known Count value during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 1,
                "Current-induced negative Locate root Count must reject after Current and before the next MoveNext.");
        }

        private static void CurrentConflictRejectsBeforeNextMoveNext()
        {
            var source = new CurrentConflictCollection("E1");
            ThrowsCountIntegrity(() => SourceHandleResolver.Resolve(CreateProject(), source), "conflicting known Count values during traversal");
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 1,
                "Current-induced conflicting Locate root Count surfaces must reject after Current and before the next MoveNext.");
        }

        private static void StableCountStillResolves()
        {
            var source = new StableReadOnlyCollection("E1");
            var handles = SourceHandleResolver.Resolve(CreateProject(), source);
            Require(handles.Count == 1 && handles[0] == "A" && source.MoveNextCalls == 2 && source.CurrentReads == 1,
                "Stable counted Locate root input must preserve successful resolution across the stronger Current boundary.");
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("LOCATE-ROOT-CURRENT-COUNT", "Locate root Current Count stability");
            var element = new ProjectElement("E1", ElementCategory.CustomQuantity);
            element.SourceHandles.Add("A");
            project.Elements.Add(element);
            return project;
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
                throw new InvalidOperationException("Unexpected Current-induced Locate root Count-integrity error: " + ex.Message, ex);
            }
            throw new InvalidOperationException("Expected Current-induced Locate root Count-integrity rejection containing: " + expectedFragment);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class CurrentDriftReadOnlyCollection : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _transientCount;
            private bool _afterCurrent;

            internal CurrentDriftReadOnlyCollection(string value, int transientCount)
            {
                _value = value;
                _transientCount = transientCount;
            }

            public int Count => _afterCurrent ? _transientCount : 1;
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly CurrentDriftReadOnlyCollection _owner;
                private bool _moved;

                internal Enumerator(CurrentDriftReadOnlyCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._afterCurrent = true;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved)
                    {
                        _owner._afterCurrent = false;
                        return false;
                    }
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class CurrentConflictCollection : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _afterCurrent;

            internal CurrentConflictCollection(string value) => _value = value;

            public int Count => 1;
            int IReadOnlyCollection<string>.Count => _afterCurrent ? 2 : 1;
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
                private readonly CurrentConflictCollection _owner;
                private bool _moved;

                internal Enumerator(CurrentConflictCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._afterCurrent = true;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved)
                    {
                        _owner._afterCurrent = false;
                        return false;
                    }
                    _moved = true;
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
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StableReadOnlyCollection _owner;
                private bool _moved;

                internal Enumerator(StableReadOnlyCollection owner) => _owner = owner;

                public string Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
