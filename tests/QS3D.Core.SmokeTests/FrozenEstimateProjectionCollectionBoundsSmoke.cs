using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class FrozenEstimateProjectionCollectionBoundsSmoke
    {
        internal static void Run()
        {
            OversizedCountedInputFailsBeforeEnumeration();
            ExactLimitContinuesToExistingValidation();
        }

        private static void OversizedCountedInputFailsBeforeEnumeration()
        {
            Throws<InvalidDataException>(() =>
                FrozenEstimateProjection.Create(new CountedLines(10001)));
        }

        private static void ExactLimitContinuesToExistingValidation()
        {
            Throws<EnumerationSentinelException>(() =>
                FrozenEstimateProjection.Create(new CountedLines(10000)));
        }

        private static void Throws<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private sealed class CountedLines : IReadOnlyCollection<EstimateLine>
        {
            internal CountedLines(int count)
            {
                Count = count;
            }

            public int Count { get; }

            public IEnumerator<EstimateLine> GetEnumerator()
            {
                throw new EnumerationSentinelException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class EnumerationSentinelException : Exception
        {
        }
    }
}