using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectPersistenceCheckpointNonGenericCountSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var project = new ProjectState("P-NONGENERIC-COUNT", "Non-generic checkpoint count");
            var source = new NonGenericOversizeCollection(10001);

            try
            {
                ProjectPersistenceCheckpoint.Capture(project, source);
            }
            catch (InvalidOperationException)
            {
                if (source.EnumerationCount != 0)
                    throw new Exception("Known oversized non-generic checkpoint collection was enumerated before rejection.");
                return;
            }

            throw new Exception("Expected known oversized non-generic checkpoint collection to fail closed before enumeration.");
        }

        private sealed class NonGenericOversizeCollection : IEnumerable<string>, ICollection
        {
            public NonGenericOversizeCollection(int count) { Count = count; }

            public int Count { get; }
            public bool IsSynchronized => false;
            public object SyncRoot => this;
            public int EnumerationCount { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                EnumerationCount++;
                throw new Exception("Known oversized non-generic collection must not be enumerated.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
