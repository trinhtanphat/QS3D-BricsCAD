using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectMetadataCurrentCountSmoke
    {
        private const string CountChanged = "Project metadata persistence input Count changed during traversal.";

        [ModuleInitializer]
        internal static void Initialize()
        {
            CurrentInducedCountDriftFailsBeforeItemValidation();
        }

        private static void CurrentInducedCountDriftFailsBeforeItemValidation()
        {
            var project = new ProjectState("metadata-current-count", "Metadata Current Count");
            project.Metadata.Add("seed", "original");
            var input = new DriftOnCurrentCollection();

            try
            {
                InvokePersistenceReplacement(project, input);
                throw new InvalidOperationException("Current-induced project metadata Count drift was accepted.");
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException failure)
            {
                Equal(CountChanged, failure.Message, "Current-induced Count drift failure");
            }

            Equal(1, input.MoveNextCalls, "Current-induced Count drift MoveNext calls");
            Equal(1, input.CurrentReads, "Current-induced Count drift Current reads");
            Equal(1, project.Metadata.Count, "Current-induced Count drift atomic metadata count");
            Equal("original", project.Metadata["seed"], "Current-induced Count drift atomic metadata value");
        }

        private static void InvokePersistenceReplacement(
            ProjectState project,
            IEnumerable<KeyValuePair<string, string>> input)
        {
            var method = project.Metadata.GetType().GetMethod(
                "ReplacePersistenceState",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
                throw new InvalidOperationException("Project metadata persistence replacement method was not found.");
            method.Invoke(project.Metadata, new object[] { input });
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private sealed class DriftOnCurrentCollection : ICollection<KeyValuePair<string, string>>
        {
            private bool _drifted;

            public int MoveNextCalls { get; private set; }
            public int CurrentReads { get; private set; }
            public int Count => _drifted ? 2 : 1;
            public bool IsReadOnly => true;

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void Add(KeyValuePair<string, string> item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(KeyValuePair<string, string> item) => false;
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(KeyValuePair<string, string> item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<KeyValuePair<string, string>>
            {
                private readonly DriftOnCurrentCollection _owner;
                private int _state;

                internal Enumerator(DriftOnCurrentCollection owner) => _owner = owner;

                public KeyValuePair<string, string> Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._drifted = true;
                        return new KeyValuePair<string, string>(null!, "must-not-be-validated");
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    if (_state != 0) return false;
                    _state = 1;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
