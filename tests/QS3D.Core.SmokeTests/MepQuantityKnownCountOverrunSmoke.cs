using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepQuantityKnownCountOverrunSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            KnownCountOverrunWinsBeforeUnexpectedNull();
            KnownCountOverrunWinsBeforeDuplicateValidation();
            KnownCountUnderTraversalStillFails();
            HonestKnownCountRemainsAccepted();
            PureStreamingInputRemainsAccepted();
        }

        private static void KnownCountOverrunWinsBeforeUnexpectedNull()
        {
            var source = new CountedElements(1, Element("A"), null);
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(2, source.YieldedCount, "MEP known-count overrun must stop at the first unexpected yielded item.");
            Contains("known count", error.Message, "MEP known-count overrun must win before unexpected null validation.");
        }

        private static void KnownCountOverrunWinsBeforeDuplicateValidation()
        {
            var duplicate = Element("A");
            var source = new CountedElements(1, duplicate, duplicate);
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(2, source.YieldedCount, "MEP known-count overrun must stop before processing the unexpected duplicate.");
            Contains("known count", error.Message, "MEP known-count overrun must win before duplicate-ID validation.");
        }

        private static void KnownCountUnderTraversalStillFails()
        {
            var source = new CountedElements(2, Element("A"));
            var error = Capture<InvalidOperationException>(() => new MepQuantityService().Aggregate(source));

            Equal(1, source.YieldedCount, "MEP under-traversal control must enumerate its only supplied item exactly once.");
            Contains("known count", error.Message, "MEP under-traversal must preserve the final known-count mismatch contract.");
        }

        private static void HonestKnownCountRemainsAccepted()
        {
            var source = new CountedElements(2, Element("A"), Element("B"));
            var groups = new MepQuantityService().Aggregate(source);

            Equal(2, source.YieldedCount, "Honest counted MEP input must traverse both elements exactly once.");
            Equal(1, groups.Count, "Honest counted MEP grouping changed unexpectedly.");
            Equal(2, groups[0].ElementCount, "Honest counted MEP element count changed unexpectedly.");
            Equal(2, groups[0].QuantityCount, "Honest counted MEP quantity count changed unexpectedly.");
            Equal(2d, groups[0].LengthM, "Honest counted MEP length changed unexpectedly.");
        }

        private static void PureStreamingInputRemainsAccepted()
        {
            var source = new StreamingElements(Element("A"), Element("B"));
            var groups = new MepQuantityService().Aggregate(source);

            Equal(2, source.YieldedCount, "Pure-streaming MEP input must remain single-pass.");
            Equal(1, groups.Count, "Pure-streaming MEP grouping changed unexpectedly.");
            Equal(2, groups[0].ElementCount, "Pure-streaming MEP element count changed unexpectedly.");
        }

        private static MepElement Element(string suffix) => new MepElement(
            "MEP-" + suffix,
            MepElementKind.Pipe,
            "CHW",
            "DN100",
            "L1",
            count: 1,
            lengthM: 1d,
            areaM2: 1d,
            volumeM3: 1d);

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountedElements : IReadOnlyCollection<MepElement>
        {
            private readonly MepElement?[] _items;

            internal CountedElements(int reportedCount, params MepElement?[] items)
            {
                Count = reportedCount;
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            public int Count { get; }
            internal int YieldedCount { get; private set; }

            public IEnumerator<MepElement> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    YieldedCount++;
                    yield return _items[i]!;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class StreamingElements : IEnumerable<MepElement>
        {
            private readonly MepElement[] _items;

            internal StreamingElements(params MepElement[] items)
            {
                _items = items ?? throw new ArgumentNullException(nameof(items));
            }

            internal int YieldedCount { get; private set; }

            public IEnumerator<MepElement> GetEnumerator()
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    YieldedCount++;
                    yield return _items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
