using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Coordination;

namespace QS3D.Core.SmokeTests
{
    internal static class DuplicateDetectionOptionsSnapshotSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ElementTraversalCannotWidenAdmittedTolerance();
            CandidateTraversalCannotEnableSemanticIdentity();
            CandidateTraversalCannotRelaxCategoryPolicy();
            InvalidInitialToleranceFailsBeforeEnumeration();
            StableOptionsRemainAccepted();
        }

        private static void ElementTraversalCannotWidenAdmittedTolerance()
        {
            var options = new DuplicateDetectionOptions { CoordinateToleranceM = 0.001d };
            var source = new MutatingEnumerable<CoordinationElement>(
                () => options.CoordinateToleranceM = 1d,
                Element("A", "Structure", "Wall", Box(0d, 0d, 0d, 1d, 1d, 1d)),
                Element("B", "Structure", "Wall", Box(0.01d, 0d, 0d, 1.01d, 1d, 1d)));

            var result = new DuplicateDetectionService().Detect(source, options);
            Equal(0, result.Pairs.Count, "Traversal-time tolerance mutation must not widen the admitted duplicate policy.");
            Equal(1, source.MutationCalls, "Tolerance mutation fixture must execute exactly once.");
        }

        private static void CandidateTraversalCannotEnableSemanticIdentity()
        {
            var options = new DuplicateDetectionOptions { EnableSemanticIdentity = false };
            var source = new MutatingEnumerable<DuplicateCandidate>(
                () => options.EnableSemanticIdentity = true,
                Candidate("A", "Architecture", "Door", Box(0d, 0d, 0d, 1d, 1d, 1d), "SAME"),
                Candidate("B", "Architecture", "Door", Box(10d, 0d, 0d, 11d, 1d, 1d), "SAME"));

            var result = new DuplicateDetectionService().Detect(source, options);
            Equal(0, result.Pairs.Count, "Traversal-time mutation must not enable semantic duplicate matching after admission.");
        }

        private static void CandidateTraversalCannotRelaxCategoryPolicy()
        {
            var options = new DuplicateDetectionOptions { RequireSameCategoryForGeometry = true };
            var shared = Box(0d, 0d, 0d, 1d, 1d, 1d);
            var source = new MutatingEnumerable<DuplicateCandidate>(
                () => options.RequireSameCategoryForGeometry = false,
                Candidate("A", "Structure", "Wall", shared, string.Empty),
                Candidate("B", "Structure", "Beam", shared, string.Empty));

            var result = new DuplicateDetectionService().Detect(source, options);
            Equal(0, result.Pairs.Count, "Traversal-time mutation must not relax category matching after admission.");
        }

        private static void InvalidInitialToleranceFailsBeforeEnumeration()
        {
            var options = new DuplicateDetectionOptions { CoordinateToleranceM = double.NaN };
            var source = new MutatingEnumerable<CoordinationElement>(
                () => { },
                Element("A", "Structure", "Wall", Box(0d, 0d, 0d, 1d, 1d, 1d)));

            try
            {
                _ = new DuplicateDetectionService().Detect(source, options);
            }
            catch (ArgumentOutOfRangeException)
            {
                Equal(0, source.GetEnumeratorCalls, "Invalid initial options must fail before caller enumeration starts.");
                return;
            }

            throw new InvalidOperationException("Invalid initial duplicate-detection tolerance must fail closed.");
        }

        private static void StableOptionsRemainAccepted()
        {
            var options = new DuplicateDetectionOptions
            {
                CoordinateToleranceM = 0.02d,
                RequireSameCategoryForGeometry = true,
                RequireSameDisciplineForGeometry = true,
                EnableSemanticIdentity = true
            };
            var source = new MutatingEnumerable<CoordinationElement>(
                () => { },
                Element("A", "Structure", "Wall", Box(0d, 0d, 0d, 1d, 1d, 1d)),
                Element("B", "Structure", "Wall", Box(0.01d, 0d, 0d, 1.01d, 1d, 1d)));

            var result = new DuplicateDetectionService().Detect(source, options);
            Equal(1, result.Pairs.Count, "Stable admitted options must preserve ordinary near-duplicate behavior.");
            if (!result.Pairs[0].IsNearGeometry)
                throw new InvalidOperationException("Stable admitted tolerance must still classify near geometry.");
        }

        private static DuplicateCandidate Candidate(string id, string discipline, string category, AxisAlignedBox bounds, string sourceId)
        {
            return new DuplicateCandidate(Element(id, discipline, category, bounds), sourceId);
        }

        private static CoordinationElement Element(string id, string discipline, string category, AxisAlignedBox bounds)
        {
            return new CoordinationElement(id, discipline, category, "Default", "Model", bounds);
        }

        private static AxisAlignedBox Box(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            return new AxisAlignedBox(minX, minY, minZ, maxX, maxY, maxZ);
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + " Expected=" + expected + ", Actual=" + actual + ".");
        }

        private sealed class MutatingEnumerable<T> : IEnumerable<T>
        {
            private readonly Action _mutation;
            private readonly T[] _items;

            internal MutatingEnumerable(Action mutation, params T[] items)
            {
                _mutation = mutation;
                _items = items;
            }

            internal int GetEnumeratorCalls { get; private set; }
            internal int MutationCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<T>
            {
                private readonly MutatingEnumerable<T> _owner;
                private int _index = -1;

                internal Enumerator(MutatingEnumerable<T> owner)
                {
                    _owner = owner;
                }

                public T Current => _owner._items[_index];
                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    if (_index < 0)
                    {
                        _owner.MutationCalls++;
                        _owner._mutation();
                    }
                    _index++;
                    return _index < _owner._items.Length;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
