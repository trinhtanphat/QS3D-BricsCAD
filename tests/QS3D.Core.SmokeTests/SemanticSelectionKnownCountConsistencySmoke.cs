using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Selection;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticSelectionKnownCountConsistencySmoke
    {
        private const int MaxSelection = 100000;

        [ModuleInitializer]
        internal static void Initialize()
        {
            RejectsInBoundConflictingCountsBeforeEnumeration();
            OversizedCountKeepsLimitPrecedenceBeforeConflict();
            NegativeCountKeepsMalformedPrecedenceBeforeConflict();
            AcceptsConsistentKnownCounts();
        }

        private static void RejectsInBoundConflictingCountsBeforeEnumeration()
        {
            var source = new MultiCountSource(0, 1, 0, throwOnEnumeration: true);
            RejectBeforeEnumeration(source, "conflicting known counts", "in-bound conflicting Count contracts");
        }

        private static void OversizedCountKeepsLimitPrecedenceBeforeConflict()
        {
            var source = new MultiCountSource(0, MaxSelection + 1, 0, throwOnEnumeration: true);
            RejectBeforeEnumeration(source, "at most " + MaxSelection, "oversized conflicting Count contracts");
        }

        private static void NegativeCountKeepsMalformedPrecedenceBeforeConflict()
        {
            var source = new MultiCountSource(0, -1, 0, throwOnEnumeration: true);
            RejectBeforeEnumeration(source, "negative known count", "negative conflicting Count contracts");
        }

        private static void AcceptsConsistentKnownCounts()
        {
            var project = new ProjectState("P-SELECTION-COUNT", "Selection Count Contract Smoke");
            var version = project.ChangeVersion;
            var source = new MultiCountSource(0, 0, 0, throwOnEnumeration: false);

            var result = SemanticSelectionInspector.Inspect(project, source);

            if (!source.EnumeratorRequested)
                throw new InvalidOperationException("Consistent semantic-selection Count contracts must reach enumeration.");
            if (result.Count != 0)
                throw new InvalidOperationException("Consistent empty semantic-selection source produced unexpected rows.");
            if (project.ChangeVersion != version)
                throw new InvalidOperationException("Semantic-selection known-count inspection must not mutate project state.");
        }

        private static void RejectBeforeEnumeration(MultiCountSource source, string expectedDiagnostic, string label)
        {
            var project = new ProjectState("P-SELECTION-COUNT", "Selection Count Contract Smoke");
            var version = project.ChangeVersion;
            try
            {
                SemanticSelectionInspector.Inspect(project, source);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(expectedDiagnostic, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(
                        "SemanticSelectionKnownCountConsistencySmoke " + label + " returned the wrong diagnostic: " + ex.Message,
                        ex);
                if (source.EnumeratorRequested)
                    throw new InvalidOperationException(
                        "SemanticSelectionKnownCountConsistencySmoke enumerated " + label + ".");
                if (project.ChangeVersion != version)
                    throw new InvalidOperationException(
                        "SemanticSelectionKnownCountConsistencySmoke mutated project state while rejecting " + label + ".");
                return;
            }

            throw new InvalidOperationException(
                "SemanticSelectionKnownCountConsistencySmoke " + label + " did not fail closed.");
        }

        private sealed class MultiCountSource : ICollection<string>, IReadOnlyCollection<string>, ICollection
        {
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountSource(int genericCount, int readOnlyCount, int nonGenericCount, bool throwOnEnumeration)
            {
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumeratorRequested { get; private set; }
            int ICollection<string>.Count => _genericCount;
            int IReadOnlyCollection<string>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            public bool IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<string> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new InvalidOperationException("Malformed semantic-selection Count contracts must fail before enumeration.");
                return ((IEnumerable<string>)Array.Empty<string>()).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(string item) => false;
            public void CopyTo(string[] array, int arrayIndex) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
            public void Add(string item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(string item) => throw new NotSupportedException();
        }
    }
}
