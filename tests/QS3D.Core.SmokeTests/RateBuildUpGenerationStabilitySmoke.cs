using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBuildUpGenerationStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            SameCountReplacementIsRejected();
            SameCountSemanticStateDriftIsRejected();
            SameCountReorderingIsRejected();
            StableCountedSourceReplaysExactlyOnce();
            StreamingSourceRemainsSinglePassCompatible();
            Console.WriteLine("PASS rate build-up generation stability");
        }

        private static void SameCountReplacementIsRejected()
        {
            var original = new CostResourceComponent("LAB-A", "Original labor", "h", 1m, 10m);
            var replacement = new CostResourceComponent("LAB-B", "Replacement labor", "h", 2m, 20m);
            RequireGenerationRejected(
                new[] { original },
                new[] { replacement },
                "same-count rate build-up component replacement must fail closed");
        }

        private static void SameCountSemanticStateDriftIsRejected()
        {
            var original = new CostResourceComponent("LAB-E", "Original labor", "h", 1.25m, 12.5m);
            var replacements = new[]
            {
                new CostResourceComponent("LAB-F", "Original labor", "h", 1.25m, 12.5m),
                new CostResourceComponent("LAB-E", "Changed labor", "h", 1.25m, 12.5m),
                new CostResourceComponent("LAB-E", "Original labor", "d", 1.25m, 12.5m),
                new CostResourceComponent("LAB-E", "Original labor", "h", 1.5m, 12.5m),
                new CostResourceComponent("LAB-E", "Original labor", "h", 1.25m, 13m),
            };

            for (var i = 0; i < replacements.Length; i++)
            {
                RequireGenerationRejected(
                    new[] { original },
                    new[] { replacements[i] },
                    "rate build-up semantic generation drift at field index " + i + " must fail closed");
            }
        }

        private static void SameCountReorderingIsRejected()
        {
            var first = new CostResourceComponent("LAB-G", "First labor", "h", 1m, 10m);
            var second = new CostResourceComponent("MAT-G", "Material", "kg", 2m, 20m);
            RequireGenerationRejected(
                new[] { first, second },
                new[] { second, first },
                "same-count rate build-up component reordering must fail closed");
        }

        private static void StableCountedSourceReplaysExactlyOnce()
        {
            var component = new CostResourceComponent("LAB-C", "Stable labor", "h", 2m, 30m);
            var source = new SameCountGenerationCollection<CostResourceComponent>(
                new[] { component },
                new[] { component });

            var buildUp = CreateBuildUp(source);
            Require(source.GetEnumeratorCalls == 2, "stable counted rate build-up source must be admitted then replayed exactly once");
            Require(buildUp.Components.Count == 1 && buildUp.Components[0].ResourceCode == "LAB-C", "stable counted build-up changed");
            Require(buildUp.DirectUnitCost == 60m, "stable counted build-up direct total changed");
        }

        private static void StreamingSourceRemainsSinglePassCompatible()
        {
            var component = new CostResourceComponent("LAB-D", "Streaming labor", "h", 3m, 40m);
            var source = new SinglePassEnumerable<CostResourceComponent>(component);
            var buildUp = CreateBuildUp(source);

            Require(source.GetEnumeratorCalls == 1, "streaming rate build-up source was replayed unexpectedly");
            Require(buildUp.Components.Count == 1 && buildUp.DirectUnitCost == 120m, "streaming build-up result changed");
        }

        private static void RequireGenerationRejected(
            IReadOnlyList<CostResourceComponent> first,
            IReadOnlyList<CostResourceComponent> second,
            string message)
        {
            var source = new SameCountGenerationCollection<CostResourceComponent>(first, second);
            var threw = false;
            try
            {
                _ = CreateBuildUp(source);
            }
            catch (InvalidOperationException ex)
            {
                threw = ex.Message.Contains("Rate build-up component collection content changed during traversal.", StringComparison.Ordinal);
            }

            Require(threw, message);
        }

        private static CostRateBuildUp CreateBuildUp(IEnumerable<CostResourceComponent> components)
        {
            return new CostRateBuildUp(
                "BUILD-1",
                new CostCode("03-01"),
                "m2",
                "USD",
                components);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class SameCountGenerationCollection<T> : ICollection<T>
        {
            private readonly IReadOnlyList<T> _first;
            private readonly IReadOnlyList<T> _second;

            internal SameCountGenerationCollection(IReadOnlyList<T> first, IReadOnlyList<T> second)
            {
                if (first.Count != second.Count) throw new ArgumentException("Generations must have equal Count.");
                _first = first;
                _second = second;
            }

            public int GetEnumeratorCalls { get; private set; }
            public int Count => _first.Count;
            public bool IsReadOnly => true;

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                return (GetEnumeratorCalls == 1 ? _first : _second).GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => false;
            public void CopyTo(T[] array, int arrayIndex) => throw new NotSupportedException();
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();
        }

        private sealed class SinglePassEnumerable<T> : IEnumerable<T>
        {
            private readonly T _item;
            internal SinglePassEnumerable(T item) => _item = item;
            public int GetEnumeratorCalls { get; private set; }

            public IEnumerator<T> GetEnumerator()
            {
                GetEnumeratorCalls++;
                if (GetEnumeratorCalls != 1) throw new InvalidOperationException("streaming source was enumerated more than once");
                yield return _item;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
