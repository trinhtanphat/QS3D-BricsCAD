using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class RevisionSnapshotDetacherCountStabilitySmoke
    {
        internal static void Run()
        {
            RejectsEnumeratorAcquisitionCountDriftBeforeTraversal();
            RejectsMoveNextCountDriftBeforeCurrent();
            RejectsCurrentCountDriftBeforePublication();
            StableDictionaryRemainsAccepted();
        }

        private static void RejectsEnumeratorAcquisitionCountDriftBeforeTraversal()
        {
            var map = new HostileDictionary(DriftPoint.GetEnumerator);
            ExpectCountFailure(map);
            if (map.MoveNextCalls != 0 || map.CurrentReads != 0)
                throw new Exception("Revision detacher traversed a map after acquisition-time Count drift.");
        }

        private static void RejectsMoveNextCountDriftBeforeCurrent()
        {
            var map = new HostileDictionary(DriftPoint.MoveNext);
            ExpectCountFailure(map);
            if (map.MoveNextCalls != 1 || map.CurrentReads != 0)
                throw new Exception("Revision detacher read Current after MoveNext-induced Count drift.");
        }

        private static void RejectsCurrentCountDriftBeforePublication()
        {
            var map = new HostileDictionary(DriftPoint.Current);
            ExpectCountFailure(map);
            if (map.MoveNextCalls != 1 || map.CurrentReads != 1)
                throw new Exception("Revision detacher Current-drift observation budget changed unexpectedly.");
        }

        private static void StableDictionaryRemainsAccepted()
        {
            var map = new HostileDictionary(DriftPoint.None);
            var before = Snapshot(map);
            var after = Snapshot(new HostileDictionary(DriftPoint.None));
            var deltas = new RevisionService().Compare(before, after);
            if (deltas.Count != 0)
                throw new Exception("Revision detacher changed stable dictionary comparison semantics.");
        }

        private static void ExpectCountFailure(HostileDictionary map)
        {
            try
            {
                new RevisionService().Compare(Snapshot(map), Snapshot(new HostileDictionary(DriftPoint.None)));
                throw new Exception("Revision detacher accepted hostile map Count drift.");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("changed during snapshot capture", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }
        }

        private static RevisionSnapshot Snapshot(IDictionary<string, string> properties)
        {
            var snapshot = new RevisionSnapshot
            {
                Id = "REV-1",
                CreatedUtc = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc),
                ProjectId = "PROJECT-1"
            };
            var element = new RevisionElementSnapshot
            {
                ElementId = "E-1",
                Category = "Wall"
            };
            var field = typeof(RevisionElementSnapshot).GetField("<Properties>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new Exception("RevisionElementSnapshot Properties backing field was not found.");
            field.SetValue(element, properties);
            snapshot.Elements.Add(element);
            return snapshot;
        }

        private enum DriftPoint
        {
            None,
            GetEnumerator,
            MoveNext,
            Current
        }

        private sealed class HostileDictionary : IDictionary<string, string>
        {
            private readonly DriftPoint _driftPoint;
            private int _reportedCount = 1;

            internal HostileDictionary(DriftPoint driftPoint) => _driftPoint = driftPoint;

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public int Count => _reportedCount;
            public bool IsReadOnly => true;
            public ICollection<string> Keys => new[] { "Name" };
            public ICollection<string> Values => new[] { "Value" };
            public string this[string key] { get => key == "Name" ? "Value" : throw new KeyNotFoundException(); set => throw new NotSupportedException(); }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                if (_driftPoint == DriftPoint.GetEnumerator) _reportedCount = 2;
                return new HostileEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool ContainsKey(string key) => key == "Name";
            public bool TryGetValue(string key, out string value) { value = key == "Name" ? "Value" : string.Empty; return key == "Name"; }
            public bool Contains(KeyValuePair<string, string> item) => item.Key == "Name" && item.Value == "Value";
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => array[arrayIndex] = new KeyValuePair<string, string>("Name", "Value");
            public void Add(string key, string value) => throw new NotSupportedException();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public bool Remove(string key) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();

            private sealed class HostileEnumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private readonly HostileDictionary _owner;
                private bool _moved;

                internal HostileEnumerator(HostileDictionary owner) => _owner = owner;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_owner._driftPoint == DriftPoint.MoveNext) _owner._reportedCount = 2;
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public KeyValuePair<string, string> Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_owner._driftPoint == DriftPoint.Current) _owner._reportedCount = 2;
                        return new KeyValuePair<string, string>("Name", "Value");
                    }
                }

                object IEnumerator.Current => Current;
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class RevisionSnapshotDetacherCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => RevisionSnapshotDetacherCountStabilitySmoke.Run();
    }
}
