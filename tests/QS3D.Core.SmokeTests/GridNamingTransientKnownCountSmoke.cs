using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingTransientKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TransientGrowthRejectsBeforeCurrentAndMutation();
            TransientShrinkRejectsBeforeCurrentAndMutation();
            TransientNegativeRejectsBeforeCurrentAndMutation();
            TransientConflictRejectsBeforeCurrentAndMutation();
            StableCountedInputStillRenumbers();
            StreamingInputStillRenumbers();
        }

        private static void TransientGrowthRejectsBeforeCurrentAndMutation()
        {
            var project = Fixture();
            var version = project.ChangeVersion;
            var source = new TransientReadOnlyIds("G1", 1, 2);
            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Grid renumber transient Count growth must reject before semantic Current.");
            Require(project.ChangeVersion == version && !project.Elements[0].Properties.ContainsKey(GridNamingService.GridLabelKey),
                "Grid renumber transient Count growth must not mutate Grid naming state.");
        }

        private static void TransientShrinkRejectsBeforeCurrentAndMutation()
        {
            var project = Fixture();
            var version = project.ChangeVersion;
            var source = new TransientReadOnlyIds("G1", 1, 0);
            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Grid renumber transient Count shrink must reject before semantic Current.");
            Require(project.ChangeVersion == version && !project.Elements[0].Properties.ContainsKey(GridNamingService.GridLabelKey),
                "Grid renumber transient Count shrink must not mutate Grid naming state.");
        }

        private static void TransientNegativeRejectsBeforeCurrentAndMutation()
        {
            var project = Fixture();
            var version = project.ChangeVersion;
            var source = new TransientReadOnlyIds("G1", 1, -1);
            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Grid renumber transient negative Count must reject before semantic Current.");
            Require(project.ChangeVersion == version && !project.Elements[0].Properties.ContainsKey(GridNamingService.GridLabelKey),
                "Grid renumber transient negative Count must not mutate Grid naming state.");
        }

        private static void TransientConflictRejectsBeforeCurrentAndMutation()
        {
            var project = Fixture();
            var version = project.ChangeVersion;
            var source = new TransientConflictingIds("G1");
            Throws<InvalidOperationException>(() => GridNamingService.Renumber(project, source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Grid renumber transient Count conflict must reject before semantic Current.");
            Require(project.ChangeVersion == version && !project.Elements[0].Properties.ContainsKey(GridNamingService.GridLabelKey),
                "Grid renumber transient Count conflict must not mutate Grid naming state.");
        }

        private static void StableCountedInputStillRenumbers()
        {
            var project = Fixture();
            var source = new StableReadOnlyIds("G1");
            var plan = GridNamingService.Renumber(project, source);
            Require(plan.Count == 1 && plan[0].ElementId == "G1" && plan[0].Label == "1" && source.CurrentReads == 1,
                "Stable counted Grid renumber input must preserve deterministic numbering.");
        }

        private static void StreamingInputStillRenumbers()
        {
            var project = Fixture();
            var plan = GridNamingService.Renumber(project, Streaming("G1"));
            Require(plan.Count == 1 && plan[0].ElementId == "G1" && plan[0].Label == "1",
                "Streaming Grid renumber input must remain supported.");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-GRID-COUNT", "Grid transient Count");
            project.Elements.Add(new ProjectElement("G1", ElementCategory.Grid, string.Empty, string.Empty, string.Empty));
            return project;
        }

        private static IEnumerable<string> Streaming(string value)
        {
            yield return value;
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class TransientReadOnlyIds : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _admittedCount;
            private readonly int _transientCount;
            private bool _afterMoveNext;

            internal TransientReadOnlyIds(string value, int admittedCount, int transientCount)
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
                private readonly TransientReadOnlyIds _owner;
                private bool _moved;
                internal Enumerator(TransientReadOnlyIds owner) => _owner = owner;
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

        private sealed class TransientConflictingIds : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _afterMoveNext;
            internal TransientConflictingIds(string value) => _value = value;
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
                private readonly TransientConflictingIds _owner;
                private bool _moved;
                internal Enumerator(TransientConflictingIds owner) => _owner = owner;
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

        private sealed class StableReadOnlyIds : IReadOnlyCollection<string>
        {
            private readonly string _value;
            internal StableReadOnlyIds(string value) => _value = value;
            public int Count => 1;
            internal int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StableReadOnlyIds _owner;
                private bool _moved;
                internal Enumerator(StableReadOnlyIds owner) => _owner = owner;
                public string Current { get { _owner.CurrentReads++; return _owner._value; } }
                object IEnumerator.Current => Current;
                public bool MoveNext() { if (_moved) return false; _moved = true; return true; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
