using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookKnownCountTraversalSmoke
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            OverYieldFailsAtFirstUnexpectedItem();
            UnderYieldFailsAfterTraversal();
            ExactKnownCountTraversalRemainsAccepted();
        }

        private static void OverYieldFailsAtFirstUnexpectedItem()
        {
            var source = new MisreportedReadOnlyCollection(reportedCount: 1, actualCount: 3);
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-OVER-YIELD", source));

            Equal(
                2,
                source.YieldedCount,
                "RateBook must stop when traversal first exceeds the accepted known Count.");
            Contains(
                "traversal count does not match",
                error.Message,
                "Over-yielding counted input must report a known-count/traversal mismatch.");
        }

        private static void UnderYieldFailsAfterTraversal()
        {
            var source = new MisreportedReadOnlyCollection(reportedCount: 3, actualCount: 1);
            var error = Capture<InvalidOperationException>(
                () => new RateBook("BOOK-KNOWN-COUNT-UNDER-YIELD", source));

            Equal(
                1,
                source.YieldedCount,
                "Under-yielding counted input should be consumed only to its natural end before mismatch detection.");
            Contains(
                "traversal count does not match",
                error.Message,
                "Under-yielding counted input must report a known-count/traversal mismatch.");
        }

        private static void ExactKnownCountTraversalRemainsAccepted()
        {
            var source = new MisreportedReadOnlyCollection(reportedCount: 2, actualCount: 2);
            var book = new RateBook("BOOK-KNOWN-COUNT-EXACT", source);

            Equal(2, source.YieldedCount, "Exact counted input must enumerate every declared item once.");
            Equal(2, book.Items.Count, "Exact known Count/traversal agreement must remain accepted.");
        }

        private static RateItem Item(int index)
        {
            return new RateItem(
                "RATE-COUNT-" + index.ToString("D4", CultureInfo.InvariantCulture),
                new CostCode("CONC"),
                "m3",
                "VND",
                index + 1m,
                StartUtc.AddTicks(index),
                "v1");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException(message + " Actual: " + actual);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(
                    message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class MisreportedReadOnlyCollection : IReadOnlyCollection<RateItem>
        {
            private readonly int _actualCount;

            internal MisreportedReadOnlyCollection(int reportedCount, int actualCount)
            {
                Count = reportedCount;
                _actualCount = actualCount;
            }

            public int Count { get; }
            internal int YieldedCount { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                for (var i = 0; i < _actualCount; i++)
                {
                    YieldedCount++;
                    yield return Item(i);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class RateBookKnownCountTraversalRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookKnownCountTraversalSmoke.Run();
        }
    }
}