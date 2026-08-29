using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactPlannerTransientKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TransientGrowthRejectsBeforeCurrent();
            TransientShrinkRejectsBeforeCurrent();
            TransientNegativeRejectsBeforeCurrent();
            TransientConflictRejectsBeforeCurrent();
            StableCountedInputStillPlans();
            StreamingInputStillPlans();
        }

        private static void TransientGrowthRejectsBeforeCurrent()
        {
            var source = new TransientReadOnlyRoots("ROOT", 1, 2);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Dependency-impact transient Count growth must reject before semantic Current.");
        }

        private static void TransientShrinkRejectsBeforeCurrent()
        {
            var source = new TransientReadOnlyRoots("ROOT", 1, 0);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Dependency-impact transient Count shrink must reject before semantic Current.");
        }

        private static void TransientNegativeRejectsBeforeCurrent()
        {
            var source = new TransientReadOnlyRoots("ROOT", 1, -1);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Dependency-impact transient negative Count must reject before semantic Current.");
        }

        private static void TransientConflictRejectsBeforeCurrent()
        {
            var source = new TransientConflictingRoots("ROOT");
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.MoveNextCalls == 1 && source.CurrentReads == 0,
                "Dependency-impact transient Count conflict must reject before semantic Current.");
        }

        private static void StableCountedInputStillPlans()
        {
            var source = new StableReadOnlyRoots("ROOT");
            var plan = new DependencyImpactPlanner().Plan(Fixture(), source);
            Require(plan.RootElementIds.Count == 1 && plan.RootElementIds[0] == "ROOT" && source.CurrentReads == 1,
                "Stable counted dependency-impact roots must preserve successful planning.");
        }

        private static void StreamingInputStillPlans()
        {
            var plan = new DependencyImpactPlanner().Plan(Fixture(), Streaming("ROOT"));
            Require(plan.RootElementIds.Count == 1 && plan.RootElementIds[0] == "ROOT",
                "Streaming dependency-impact roots must remain supported.");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-IMPACT-TRANSIENT", "Dependency impact transient Count");
            project.Elements.Add(Element("ROOT"));
            project.Elements.Add(Element("CHILD", "ROOT"));
            return project;
        }

        private static ProjectElement Element(string id, params string[] dependencies)
        {
            var element = new ProjectElement(id, ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            foreach (var dependency in dependencies) element.DependsOn.Add(dependency);
            return element;
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

        private sealed class TransientReadOnlyRoots : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _admittedCount;
            private readonly int _transientCount;
            private bool _afterMoveNext;

            internal TransientReadOnlyRoots(string value, int admittedCount, int transientCount)
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
                private readonly TransientReadOnlyRoots _owner;
                private bool _moved;
                internal Enumerator(TransientReadOnlyRoots owner) => _owner = owner;
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

        private sealed class TransientConflictingRoots : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _afterMoveNext;
            internal TransientConflictingRoots(string value) => _value = value;
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
                private readonly TransientConflictingRoots _owner;
                private bool _moved;
                internal Enumerator(TransientConflictingRoots owner) => _owner = owner;
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

        private sealed class StableReadOnlyRoots : IReadOnlyCollection<string>
        {
            private readonly string _value;
            internal StableReadOnlyRoots(string value) => _value = value;
            public int Count => 1;
            internal int CurrentReads { get; private set; }
            public IEnumerator<string> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly StableReadOnlyRoots _owner;
                private bool _moved;
                internal Enumerator(StableReadOnlyRoots owner) => _owner = owner;
                public string Current { get { _owner.CurrentReads++; return _owner._value; } }
                object IEnumerator.Current => Current;
                public bool MoveNext() { if (_moved) return false; _moved = true; return true; }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
