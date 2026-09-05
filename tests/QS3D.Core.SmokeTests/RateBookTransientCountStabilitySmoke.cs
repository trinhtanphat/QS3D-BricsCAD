using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookTransientCountStabilitySmoke
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal static void Run()
        {
            PreAdvanceGrowthFailsBeforeMoveNext();
            PostAdvanceGrowthFailsBeforeCurrent();
            PreAdvanceShrinkFailsBeforeMoveNext();
            PostAdvanceNegativeFailsBeforeCurrent();
            StableCountedInputRemainsAccepted();
        }

        private static void PreAdvanceGrowthFailsBeforeMoveNext()
        {
            var source = new ScriptedCountCollection(new[] { 2, 3, 2, 2 }, itemCount: 2);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-TRANSIENT-PRE-GROW", source));

            Equal(0, source.MoveNextCalls, "Pre-advance Count drift must fail before caller traversal advances.");
            Equal(0, source.CurrentReads, "Pre-advance Count drift must fail before Current is observed.");
            Contains("known count changed during traversal", error.Message, "Transient pre-advance growth must fail closed.");
        }

        private static void PostAdvanceGrowthFailsBeforeCurrent()
        {
            var source = new ScriptedCountCollection(new[] { 2, 2, 3, 2 }, itemCount: 2);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-TRANSIENT-POST-GROW", source));

            Equal(1, source.MoveNextCalls, "Post-advance Count drift should allow exactly the first MoveNext call.");
            Equal(0, source.CurrentReads, "Post-advance Count drift must fail before Current is observed.");
            Contains("known count changed during traversal", error.Message, "Transient post-advance growth must fail closed.");
        }

        private static void PreAdvanceShrinkFailsBeforeMoveNext()
        {
            var source = new ScriptedCountCollection(new[] { 2, 1, 2, 2 }, itemCount: 2);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-TRANSIENT-PRE-SHRINK", source));

            Equal(0, source.MoveNextCalls, "Pre-advance Count shrink must fail before caller traversal advances.");
            Equal(0, source.CurrentReads, "Pre-advance Count shrink must fail before Current is observed.");
            Contains("known count changed during traversal", error.Message, "Transient pre-advance shrink must fail closed.");
        }

        private static void PostAdvanceNegativeFailsBeforeCurrent()
        {
            var source = new ScriptedCountCollection(new[] { 2, 2, -1, 2 }, itemCount: 2);
            var error = Capture<InvalidOperationException>(() => new RateBook("BOOK-TRANSIENT-POST-NEGATIVE", source));

            Equal(1, source.MoveNextCalls, "Negative post-advance Count evidence should follow exactly one MoveNext.");
            Equal(0, source.CurrentReads, "Negative post-advance Count evidence must fail before Current is observed.");
            Contains("invalid negative known count", error.Message, "Transient negative Count metadata must retain fail-closed diagnostics.");
        }

        private static void StableCountedInputRemainsAccepted()
        {
            var source = new ScriptedCountCollection(new[] { 2 }, itemCount: 2);
            var book = new RateBook("BOOK-TRANSIENT-STABLE", source);

            Equal(6, source.MoveNextCalls, "Stable counted input must include terminal false MoveNext once for admission and once for semantic replay.");
            Equal(4, source.CurrentReads, "Stable counted input must observe each item once for admission and once for semantic replay.");
            Equal(2, book.Items.Count, "Stable counted input must remain accepted.");
        }

        private static RateItem Item(int index)
        {
            return new RateItem(
                "RATE-TRANSIENT-" + index.ToString("D4", CultureInfo.InvariantCulture),
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
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class ScriptedCountCollection : IReadOnlyCollection<RateItem>
        {
            private readonly int[] _counts;
            private readonly int _itemCount;
            private int _countReads;

            internal ScriptedCountCollection(int[] counts, int itemCount)
            {
                _counts = counts ?? throw new ArgumentNullException(nameof(counts));
                if (_counts.Length == 0) throw new ArgumentException("At least one Count value is required.", nameof(counts));
                _itemCount = itemCount;
            }

            public int Count
            {
                get
                {
                    var index = _countReads < _counts.Length ? _countReads : _counts.Length - 1;
                    _countReads++;
                    return _counts[index];
                }
            }

            internal int MoveNextCalls { get; private set; }
            internal int CurrentReads { get; private set; }

            public IEnumerator<RateItem> GetEnumerator() => new Enumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            private sealed class Enumerator : IEnumerator<RateItem>
            {
                private readonly ScriptedCountCollection _owner;
                private int _index = -1;

                internal Enumerator(ScriptedCountCollection owner)
                {
                    _owner = owner;
                }

                public RateItem Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        if (_index < 0 || _index >= _owner._itemCount)
                            throw new InvalidOperationException("Current was observed outside the valid item range.");
                        return Item(_index);
                    }
                }

                object IEnumerator.Current => Current;

                public bool MoveNext()
                {
                    _owner.MoveNextCalls++;
                    _index++;
                    return _index < _owner._itemCount;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }

    internal static class RateBookTransientCountStabilityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            RateBookTransientCountStabilitySmoke.Run();
        }
    }
}
