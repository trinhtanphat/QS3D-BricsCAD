using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimatePreTraversalCountStabilitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            EnumeratorInducedCountDriftFailsBeforeMoveNext();
            StableCountedEmptySourceRemainsAccepted();
            StreamingEmptySourceRemainsAccepted();
            Console.WriteLine("PASS frozen estimate pre-traversal Count stability");
        }

        private static void EnumeratorInducedCountDriftFailsBeforeMoveNext()
        {
            var source = new EnumeratorDriftCollection();
            try
            {
                FrozenEstimateProjection.Create(source);
            }
            catch (InvalidOperationException ex)
            {
                Require(
                    ex.Message.IndexOf("Count changed during enumeration", StringComparison.OrdinalIgnoreCase) >= 0,
                    "enumerator-induced Count drift must fail through the canonical stability contract");
                Require(source.MoveNextCalls == 0,
                    "enumerator-induced Count drift must be rejected before the first MoveNext traversal step");
                return;
            }

            throw new InvalidOperationException("enumerator-induced frozen estimate Count drift was accepted unexpectedly");
        }

        private static void StableCountedEmptySourceRemainsAccepted()
        {
            var projection = FrozenEstimateProjection.Create(Array.Empty<EstimateLine>());
            Require(projection.Rows.Count == 0, "stable counted empty projection changed");
        }

        private static void StreamingEmptySourceRemainsAccepted()
        {
            var projection = FrozenEstimateProjection.Create(EmptyStreaming());
            Require(projection.Rows.Count == 0, "streaming empty projection changed");
        }

        private static IEnumerable<EstimateLine> EmptyStreaming()
        {
            yield break;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private sealed class EnumeratorDriftCollection : ICollection<EstimateLine>
        {
            private bool _drifted;

            internal int MoveNextCalls { get; private set; }

            public int Count => _drifted ? 1 : 0;
            public bool IsReadOnly => true;

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                _drifted = true;
                return new Enumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public void Add(EstimateLine item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Contains(EstimateLine item) => false;
            public void CopyTo(EstimateLine[] array, int arrayIndex) { }
            public bool Remove(EstimateLine item) => throw new NotSupportedException();

            private sealed class Enumerator : IEnumerator<EstimateLine>
            {
                private readonly EnumeratorDriftCollection _owner;

                internal Enumerator(EnumeratorDriftCollection owner) => _owner = owner;

                public EstimateLine Current => throw new InvalidOperationException("Current must never be read.");
                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    return false;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}