using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataPersistenceSourceReentrancySmoke
    {
        private const string TargetChanged = "Project metadata changed while persistence input was being enumerated.";

        [ModuleInitializer]
        internal static void Initialize()
        {
            FinalCountReentrancyCannotOverwriteNewerMetadata();
            MoveNextReentrancyCannotOverwriteNewerMetadata();
            StableCountedReplacementPreservesContract();
        }

        private static void FinalCountReentrancyCannotOverwriteNewerMetadata()
        {
            var project = SeedProject("metadata-final-count-reentrancy");
            var input = new FinalCountReentrantCollection(project);

            ThrowsTargetChanged(() => InvokePersistenceReplacement(project, input));

            Equal(7, input.CountReads, "final Count reentrancy observations");
            Equal(2, project.Metadata.Count, "final Count reentrancy target count");
            Equal("original", project.Metadata["seed"], "final Count reentrancy seed preservation");
            Equal("nested", project.Metadata["intruder"], "final Count reentrancy nested mutation preservation");
            False(project.Metadata.ContainsKey("outer"), "stale final Count outer publication");
        }

        private static void MoveNextReentrancyCannotOverwriteNewerMetadata()
        {
            var project = SeedProject("metadata-movenext-reentrancy");
            var input = new MoveNextReentrantEnumerable(project);

            ThrowsTargetChanged(() => InvokePersistenceReplacement(project, input));

            Equal(1, input.MoveNextCalls, "MoveNext reentrancy calls");
            Equal(0, input.CurrentReads, "MoveNext reentrancy Current reads");
            Equal(2, project.Metadata.Count, "MoveNext reentrancy target count");
            Equal("nested", project.Metadata["intruder"], "MoveNext reentrancy nested mutation preservation");
            False(project.Metadata.ContainsKey("outer"), "stale MoveNext outer publication");
        }

        private static void StableCountedReplacementPreservesContract()
        {
            var project = SeedProject("metadata-stable-reentrancy-control");
            var input = new StableCountedCollection();

            InvokePersistenceReplacement(project, input);

            Equal(7, input.CountReads, "stable Count observations");
            Equal(2, input.MoveNextCalls, "stable MoveNext calls");
            Equal(1, input.CurrentReads, "stable Current reads");
            Equal(1, project.Metadata.Count, "stable replacement metadata count");
            Equal("value", project.Metadata["outer"], "stable replacement metadata value");
            False(project.Metadata.ContainsKey("seed"), "stable replacement removes prior state");
        }

        private static ProjectState SeedProject(string id)
        {
            var project = new ProjectState(id, "Metadata Persistence Source Reentrancy");
            project.Metadata.Add("seed", "original");
            return project;
        }

        private static void InvokePersistenceReplacement(ProjectState project, IEnumerable<KeyValuePair<string, string>> input)
        {
            var method = project.Metadata.GetType().GetMethod("ReplacePersistenceState", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new InvalidOperationException("Project metadata persistence replacement method was not found.");
            method.Invoke(project.Metadata, new object[] { input });
        }

        private static void ThrowsTargetChanged(Action action)
        {
            try
            {
                action();
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException failure)
            {
                if (failure.Message.StartsWith(TargetChanged, StringComparison.Ordinal)) return;
                throw new InvalidOperationException("Unexpected persistence reentrancy failure: " + failure.Message, failure);
            }
            throw new InvalidOperationException("Expected project metadata persistence target reentrancy failure.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void False(bool value, string label)
        {
            if (value) throw new InvalidOperationException(label + ": expected false.");
        }

        private abstract class SingleItemEnumerator : IEnumerator<KeyValuePair<string, string>>
        {
            private int _state;
            protected int CurrentReadCount;
            public abstract KeyValuePair<string, string> Current { get; }
            object IEnumerator.Current => Current;
            public virtual bool MoveNext()
            {
                _state++;
                return _state == 1;
            }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }

        private sealed class FinalCountReentrantCollection : ICollection<KeyValuePair<string, string>>
        {
            private readonly ProjectState _project;
            private bool _reentered;
            internal FinalCountReentrantCollection(ProjectState project) => _project = project;
            public int CountReads { get; private set; }
            public int Count
            {
                get
                {
                    CountReads++;
                    if (!_reentered && CountReads == 7)
                    {
                        _reentered = true;
                        _project.Metadata["intruder"] = "nested";
                    }
                    return 1;
                }
            }
            public bool IsReadOnly => true;
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new ValueEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();
        }

        private sealed class StableCountedCollection : ICollection<KeyValuePair<string, string>>
        {
            public int CountReads { get; private set; }
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count { get { CountReads++; return 1; } }
            public bool IsReadOnly => true;
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new StableEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();

            private sealed class StableEnumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private readonly StableCountedCollection _owner;
                private int _state;
                internal StableEnumerator(StableCountedCollection owner) => _owner = owner;
                public KeyValuePair<string, string> Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return new KeyValuePair<string, string>("outer", "value");
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _state++;
                    return _state == 1;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class MoveNextReentrantEnumerable : IEnumerable<KeyValuePair<string, string>>
        {
            private readonly ProjectState _project;
            internal MoveNextReentrantEnumerable(ProjectState project) => _project = project;
            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new ReentrantEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class ReentrantEnumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private readonly MoveNextReentrantEnumerable _owner;
                private bool _moved;
                internal ReentrantEnumerator(MoveNextReentrantEnumerable owner) => _owner = owner;
                public KeyValuePair<string, string> Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        return new KeyValuePair<string, string>("outer", "value");
                    }
                }
                object IEnumerator.Current => Current;
                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_moved) return false;
                    _moved = true;
                    _owner._project.Metadata["intruder"] = "nested";
                    return true;
                }
                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }

        private sealed class ValueEnumerator : IEnumerator<KeyValuePair<string, string>>
        {
            private int _state;
            public KeyValuePair<string, string> Current => new KeyValuePair<string, string>("outer", "value");
            object IEnumerator.Current => Current;
            public bool MoveNext() { _state++; return _state == 1; }
            public void Reset() => throw new NotSupportedException();
            public void Dispose() { }
        }
    }
}
