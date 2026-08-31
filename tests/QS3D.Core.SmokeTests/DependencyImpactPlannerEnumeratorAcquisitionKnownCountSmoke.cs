using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class DependencyImpactPlannerEnumeratorAcquisitionKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AcquisitionGrowthRejectsBeforeMoveNext();
            AcquisitionShrinkRejectsBeforeMoveNext();
            AcquisitionNegativeRejectsBeforeMoveNext();
            AcquisitionConflictRejectsBeforeMoveNext();
            StableCountedInputStillPlans();
            StreamingInputStillPlans();
        }

        private static void AcquisitionGrowthRejectsBeforeMoveNext()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, 2);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Dependency-impact enumerator-acquisition Count growth must reject before first MoveNext/Current.");
        }

        private static void AcquisitionShrinkRejectsBeforeMoveNext()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, 0);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Dependency-impact enumerator-acquisition Count shrink must reject before first MoveNext/Current.");
        }

        private static void AcquisitionNegativeRejectsBeforeMoveNext()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, -1);
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Dependency-impact enumerator-acquisition negative Count must reject before first MoveNext/Current.");
        }

        private static void AcquisitionConflictRejectsBeforeMoveNext()
        {
            var source = new AcquisitionConflictingRoots("ROOT");
            Throws<ArgumentException>(() => new DependencyImpactPlanner().Plan(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Dependency-impact enumerator-acquisition conflicting Count must reject before first MoveNext/Current.");
        }

        private static void StableCountedInputStillPlans()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, 1);
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
            var project = new ProjectState("P-IMPACT-ENUM-COUNT", "Dependency impact enumerator Count");
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

        private sealed class AcquisitionReadOnlyRoots : IReadOnlyCollection<string>
        {
            private readonly string _value;
            private readonly int _admittedCount;
            private readonly int _acquisitionCount;
            private bool _afterGetEnumerator;

            internal AcquisitionReadOnlyRoots(string value, int admittedCount, int acquisitionCount)
            {
                _value = value;
                _admittedCount = admittedCount;
                _acquisitionCount = acquisitionCount;
            }

            public int Count => _afterGetEnumerator ? _acquisitionCount : _admittedCount;
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                _afterGetEnumerator = true;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly AcquisitionReadOnlyRoots _owner;
                private bool _moved;
                internal Enumerator(AcquisitionReadOnlyRoots owner) => _owner = owner;
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
                    _owner._afterGetEnumerator = false;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class AcquisitionConflictingRoots : ICollection<string>, IReadOnlyCollection<string>
        {
            private readonly string _value;
            private bool _afterGetEnumerator;
            internal AcquisitionConflictingRoots(string value) => _value = value;
            public int Count => 1;
            int IReadOnlyCollection<string>.Count => _afterGetEnumerator ? 2 : 1;
            public bool IsReadOnly => true;
            internal int GetEnumeratorCalls { get; private set; }
            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                GetEnumeratorCalls++;
                _afterGetEnumerator = true;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => item == _value;
            public void CopyTo(string[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<string>
            {
                private readonly AcquisitionConflictingRoots _owner;
                private bool _moved;
                internal Enumerator(AcquisitionConflictingRoots owner) => _owner = owner;
                public string Current { get { _owner.CurrentReads++; return _owner._value; } }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _owner._afterGetEnumerator = false;
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
