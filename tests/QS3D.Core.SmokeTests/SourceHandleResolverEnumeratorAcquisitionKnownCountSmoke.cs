using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleResolverEnumeratorAcquisitionKnownCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AcquisitionGrowthRejectsBeforeMoveNext();
            AcquisitionShrinkRejectsBeforeMoveNext();
            AcquisitionNegativeRejectsBeforeMoveNext();
            AcquisitionConflictRejectsBeforeMoveNext();
            StableCountStillResolves();
            StreamingInputStillResolves();
        }

        private static void AcquisitionGrowthRejectsBeforeMoveNext()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, 2);
            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Locate acquisition-time Count growth must reject before first MoveNext/Current.");
        }

        private static void AcquisitionShrinkRejectsBeforeMoveNext()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, 0);
            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Locate acquisition-time Count shrink must reject before first MoveNext/Current.");
        }

        private static void AcquisitionNegativeRejectsBeforeMoveNext()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, -1);
            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Locate acquisition-time negative Count must reject before first MoveNext/Current.");
        }

        private static void AcquisitionConflictRejectsBeforeMoveNext()
        {
            var source = new AcquisitionConflictingRoots("ROOT");
            Throws<InvalidOperationException>(() => SourceHandleResolver.Resolve(Fixture(), source));
            Require(source.GetEnumeratorCalls == 1 && source.MoveNextCalls == 0 && source.CurrentReads == 0,
                "Locate acquisition-time conflicting Count must reject before first MoveNext/Current.");
        }

        private static void StableCountStillResolves()
        {
            var source = new AcquisitionReadOnlyRoots("ROOT", 1, 1);
            var handles = SourceHandleResolver.Resolve(Fixture(), source);
            Require(handles.Count == 1 && handles[0] == "1A" && source.CurrentReads == 1,
                "Stable counted Locate roots must still resolve their source handles.");
        }

        private static void StreamingInputStillResolves()
        {
            var handles = SourceHandleResolver.Resolve(Fixture(), Streaming("ROOT"));
            Require(handles.Count == 1 && handles[0] == "1A",
                "Streaming Locate roots must remain supported.");
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-LOCATE-ENUM-COUNT", "Locate enumerator Count");
            var root = new ProjectElement("ROOT", ElementCategory.Beam, string.Empty, string.Empty, string.Empty);
            root.SourceHandles.Add("1A");
            project.Elements.Add(root);
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
