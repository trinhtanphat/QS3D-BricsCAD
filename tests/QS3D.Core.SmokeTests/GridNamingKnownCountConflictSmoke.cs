using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class GridNamingKnownCountConflictSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("grid-known-count-conflict", "Grid known-count conflict");
            var source = new ConflictingKnownCountCollection();
            var beforeVersion = project.ChangeVersion;

            try
            {
                GridNamingService.Renumber(project, source);
            }
            catch (InvalidOperationException ex)
            {
                Equal("Grid renumber target source exposes conflicting known Count values.", ex.Message);
                True(source.GenericCountRead, "generic Count was not inspected");
                True(source.ReadOnlyCountRead, "read-only Count was not inspected");
                True(source.NonGenericCountRead, "non-generic Count was not inspected");
                True(!source.EnumeratorRequested, "conflicting source was enumerated");
                Equal(beforeVersion, project.ChangeVersion);
                return;
            }

            throw new Exception("Expected conflicting in-capacity Grid Count contracts to fail before enumeration.");
        }

        private sealed class ConflictingKnownCountCollection : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            public int Count
            {
                get
                {
                    GenericCountRead = true;
                    return 1;
                }
            }

            int IReadOnlyCollection<string>.Count
            {
                get
                {
                    ReadOnlyCountRead = true;
                    return 2;
                }
            }

            int ICollection.Count
            {
                get
                {
                    NonGenericCountRead = true;
                    return 2;
                }
            }

            public bool GenericCountRead { get; private set; }
            public bool ReadOnlyCountRead { get; private set; }
            public bool NonGenericCountRead { get; private set; }
            public bool EnumeratorRequested { get; private set; }
            public bool IsReadOnly => true;
            public bool IsSynchronized => false;
            public object SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                throw new Exception("Conflicting Grid Count source should not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("GridNamingKnownCountConflictSmoke expected=" + expected + ", actual=" + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new Exception("GridNamingKnownCountConflictSmoke " + message + ".");
        }
    }
}
