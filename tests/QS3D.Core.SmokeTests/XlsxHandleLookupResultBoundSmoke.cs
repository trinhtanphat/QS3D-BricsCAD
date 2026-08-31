using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class XlsxHandleLookupResultBoundSmoke
    {
        private const int MaximumIdentityValues = 16384;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            HandlesRejectFirstOverBoundObservation();
            ElementIdsRejectFirstOverBoundObservation();
            StableInputsPreserveCanonicalizationAndDeduplication();
        }

        private static void HandlesRejectFirstOverBoundObservation()
        {
            var source = new CountingIdentitySequence(MaximumIdentityValues + 1, "AA");
            ExpectBoundFailure(() => _ = new XlsxHandleLookupResult(source, "fp", false), "handles");
            Equal(MaximumIdentityValues + 1, source.CurrentReads, "handles Current reads");
        }

        private static void ElementIdsRejectFirstOverBoundObservation()
        {
            var source = new CountingIdentitySequence(MaximumIdentityValues + 1, "element");
            ExpectBoundFailure(() => _ = new XlsxHandleLookupResult(new[] { "AA" }, source, "fp", false), "element ids");
            Equal(MaximumIdentityValues + 1, source.CurrentReads, "element-id Current reads");
        }

        private static void StableInputsPreserveCanonicalizationAndDeduplication()
        {
            var result = new XlsxHandleLookupResult(
                new[] { "  aa  ", "AA", "bb", " ", string.Empty },
                new[] { " element-1 ", "ELEMENT-1", "element-2" },
                " fp ",
                false);

            Equal(2, result.Handles.Count, "stable handle count");
            Equal("aa", result.Handles[0], "stable first handle");
            Equal("bb", result.Handles[1], "stable second handle");
            Equal(2, result.ElementIds.Count, "stable element-id count");
            Equal("element-1", result.ElementIds[0], "stable first element id");
            Equal("element-2", result.ElementIds[1], "stable second element id");
            Equal("fp", result.DrawingFingerprint, "stable fingerprint");
        }

        private static void ExpectBoundFailure(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (!ex.Message.Contains("identity values", StringComparison.OrdinalIgnoreCase) ||
                    !ex.Message.Contains(MaximumIdentityValues.ToString(), StringComparison.Ordinal))
                    throw new Exception(label + " wrong bound failure: " + ex.Message);
                return;
            }
            throw new Exception(label + " expected bounded identity materialization failure.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new Exception(label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountingIdentitySequence : IEnumerable<string>
        {
            private readonly int _count;
            private readonly string _prefix;

            internal CountingIdentitySequence(int count, string prefix)
            {
                _count = count;
                _prefix = prefix;
            }

            internal int CurrentReads { get; private set; }

            public IEnumerator<string> GetEnumerator()
            {
                for (var index = 0; index < _count; index++)
                {
                    CurrentReads++;
                    yield return _prefix + index;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
