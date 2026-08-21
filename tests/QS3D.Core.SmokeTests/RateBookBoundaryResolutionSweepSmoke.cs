using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookBoundaryResolutionSweepSmoke
    {
        private static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [ModuleInitializer]
        internal static void Run()
        {
            ConstructorAndTokenBoundariesFailClosed();
            ExactItemBoundaryAndKnownOversizeFailClosed();
            KnownCountContractsArePreflightedAndTraversalBound();
            DuplicateAndAmbiguousRateIdentityFailsClosed();
            CanonicalItemsOrderingIsDeterministic();
            ResolveHonorsEffectiveRevisionBoundaries();
        }

        private static void ConstructorAndTokenBoundariesFailClosed()
        {
            Throws<ArgumentException>(() => new CostCode(" "), "blank cost code");
            Throws<ArgumentException>(() => new CostCode(" C01"), "padded cost code");
            Throws<ArgumentException>(() => new CostCode("C 01"), "embedded cost-code whitespace");
            Throws<ArgumentException>(() => new CostCode("C\t01"), "cost-code control character");

            Throws<ArgumentException>(() => Item("R1", "C01", "M3", "USD", 1m, T0),
                "unit must be canonical lower-case");
            Throws<ArgumentException>(() => Item("R1", "C01", "m3", "usd", 1m, T0),
                "currency must be canonical upper-case");
            Throws<ArgumentException>(() => Item("R1", "C01", "m3", "US", 1m, T0),
                "currency length");
            Throws<ArgumentException>(() => Item("R1", "C01", "m3", "U1D", 1m, T0),
                "currency ASCII letters");
            Throws<ArgumentOutOfRangeException>(() => Item("R1", "C01", "m3", "USD", -0.01m, T0),
                "negative unit rate");
            Throws<ArgumentException>(() => Item("R1", "C01", "m3", "USD", 1m,
                DateTime.SpecifyKind(T0, DateTimeKind.Local)), "local effective timestamp");
            Throws<ArgumentException>(() => Item("R1", "C01", "m3", "USD", 1m,
                DateTime.SpecifyKind(T0, DateTimeKind.Unspecified)), "unspecified effective timestamp");
            Throws<ArgumentException>(() => new RateBook(" RATEBOOK", Array.Empty<RateItem>()),
                "padded rate-book id");

            var zero = Item("R0", "C0", "m3", "USD", -0m, T0);
            Equal(0m, zero.UnitRate, "signed zero must canonicalize to decimal zero");
            Equal("m3", zero.Unit, "canonical lower-case unit");
            Equal("USD", zero.Currency, "canonical currency");
            Equal(DateTimeKind.Utc, zero.EffectiveFromUtc.Kind, "effective timestamp UTC kind");
        }

        private static void ExactItemBoundaryAndKnownOversizeFailClosed()
        {
            var exact = new RateItem[10000];
            for (var i = 0; i < exact.Length; i++)
                exact[i] = Item("R" + i.ToString("D5"), "C" + i.ToString("D5"), "m3", "USD", i, T0);

            var book = new RateBook("EXACT", exact);
            Equal(10000, book.Items.Count, "exact 10,000 item boundary must remain accepted");

            var oversize = new MultiCountSource(
                Array.Empty<RateItem>(),
                genericCount: 10001,
                readOnlyCount: 10001,
                nonGenericCount: 10001,
                throwOnEnumeration: true);
            Throws<InvalidOperationException>(() => new RateBook("OVERSIZE", oversize),
                "known 10,001 item source");
            True(!oversize.EnumeratorRequested,
                "known oversize source must fail before GetEnumerator");

            var streamed = new CountingEnumerable(10001);
            Throws<InvalidOperationException>(() => new RateBook("STREAMED", streamed),
                "unknown-count streamed boundary+1");
            Equal(10001, streamed.Yielded,
                "unknown-count source must stop exactly at item 10,001");
        }

        private static void KnownCountContractsArePreflightedAndTraversalBound()
        {
            var one = new[] { Item("R1", "C1", "m3", "USD", 1m, T0) };

            var negative = new MultiCountSource(
                one, genericCount: -1, readOnlyCount: -1, nonGenericCount: -1, throwOnEnumeration: true);
            Throws<InvalidOperationException>(() => new RateBook("NEG", negative), "negative known count");
            True(!negative.EnumeratorRequested, "negative known count must fail before enumeration");

            var conflicting = new MultiCountSource(
                one, genericCount: 1, readOnlyCount: 2, nonGenericCount: 1, throwOnEnumeration: true);
            Throws<InvalidOperationException>(() => new RateBook("CONFLICT", conflicting),
                "conflicting known counts");
            True(!conflicting.EnumeratorRequested, "conflicting known count must fail before enumeration");

            var advertisedTooLarge = new MultiCountSource(
                one, genericCount: 2, readOnlyCount: 2, nonGenericCount: 2, throwOnEnumeration: false);
            Throws<InvalidOperationException>(() => new RateBook("SHORT", advertisedTooLarge),
                "advertised count greater than traversal");
            True(advertisedTooLarge.EnumeratorRequested, "in-bound source should be traversed");

            var two = new[]
            {
                Item("R1", "C1", "m3", "USD", 1m, T0),
                Item("R2", "C2", "m3", "USD", 2m, T0)
            };
            var advertisedTooSmall = new MultiCountSource(
                two, genericCount: 1, readOnlyCount: 1, nonGenericCount: 1, throwOnEnumeration: false);
            Throws<InvalidOperationException>(() => new RateBook("LONG", advertisedTooSmall),
                "advertised count smaller than traversal");
            Equal(2, advertisedTooSmall.Yielded,
                "mismatch must be detected at first item beyond advertised count without overread");

            var pureEnumerable = PureEnumerable(two);
            var uncounted = new RateBook("PURE", pureEnumerable);
            Equal(2, uncounted.Items.Count, "pure IEnumerable without known count remains accepted");
        }

        private static void DuplicateAndAmbiguousRateIdentityFailsClosed()
        {
            Throws<ArgumentException>(() => new RateBook("DUP-ID", new[]
            {
                Item("RATE-A", "C1", "m3", "USD", 1m, T0),
                Item("rate-a", "C2", "m3", "USD", 2m, T0.AddDays(1))
            }), "case-insensitive duplicate rate item id");

            Throws<ArgumentException>(() => new RateBook("AMBIG", new[]
            {
                Item("R1", "COST-A", "m3", "USD", 10m, T0),
                Item("R2", "cost-a", "m3", "USD", 11m, T0)
            }), "same semantic scope and effective timestamp ambiguity");

            var valid = new RateBook("REVISIONS", new[]
            {
                Item("R2", "cost-a", "m3", "USD", 11m, T0.AddDays(1)),
                Item("R1", "COST-A", "m3", "USD", 10m, T0)
            });
            Equal(2, valid.Items.Count, "distinct effective revisions in same semantic scope remain accepted");
        }

        private static void CanonicalItemsOrderingIsDeterministic()
        {
            var source = new[]
            {
                Item("z-late", "cost-b", "m3", "USD", 30m, T0.AddDays(2)),
                Item("b-late", "COST-A", "m3", "USD", 20m, T0.AddDays(2)),
                Item("a-early", "COST-A", "m3", "USD", 10m, T0),
                Item("a-eur", "COST-A", "m3", "EUR", 9m, T0),
                Item("a-m2", "COST-A", "m2", "USD", 8m, T0),
                Item("a-case", "cost-a", "m3", "USD", 15m, T0.AddDays(1))
            };

            var forward = new RateBook("ORDER-A", source);
            var reverse = new RateBook("ORDER-B", source.AsEnumerable().Reverse());

            Equal(forward.Items.Count, reverse.Items.Count, "ordering cardinality");
            for (var i = 0; i < forward.Items.Count; i++)
                Equal(forward.Items[i].RateItemId, reverse.Items[i].RateItemId,
                    "Items ordering must not depend on caller enumeration order at index " + i);

            var ids = forward.Items.Select(x => x.RateItemId).ToArray();
            Equal("a-m2", ids[0], "unit ordering control");
            Equal("a-eur", ids[1], "currency ordering control");
            Equal("a-early", ids[2], "effective-time ordering control");
            Equal("b-late", ids[3], "later same-casing revision ordering control");
            Equal("a-case", ids[4], "ordinal cost-code tie-break control");
            Equal("z-late", ids[5], "cost-code ordering control");
        }

        private static void ResolveHonorsEffectiveRevisionBoundaries()
        {
            var book = new RateBook("RESOLVE", new[]
            {
                Item("R3", "COST-A", "m3", "USD", 30m, T0.AddDays(20)),
                Item("R1", "cost-a", "m3", "USD", 10m, T0),
                Item("R2", "COST-A", "m3", "USD", 20m, T0.AddDays(10)),
                Item("EUR", "COST-A", "m3", "EUR", 99m, T0),
                Item("M2", "COST-A", "m2", "USD", 77m, T0)
            });

            var before = book.Resolve(new CostCode("COST-A"), "m3", "USD", T0.AddTicks(-1));
            True(!before.IsMatched && before.Item == null, "lookup before first effective rate must be unmatched");

            var atFirst = book.Resolve(new CostCode("COST-A"), "m3", "USD", T0);
            Matched(atFirst, "R1", 10m, "exact first effective boundary");

            var between = book.Resolve(new CostCode("cost-a"), "m3", "USD", T0.AddDays(15));
            Matched(between, "R2", 20m, "between revisions");

            var atLatest = book.Resolve(new CostCode("CoSt-A"), "m3", "USD", T0.AddDays(20));
            Matched(atLatest, "R3", 30m, "exact latest effective boundary");

            var after = book.Resolve(new CostCode("COST-A"), "m3", "USD", T0.AddYears(5));
            Matched(after, "R3", 30m, "after latest revision");

            var wrongCurrency = book.Resolve(new CostCode("COST-A"), "m3", "VND", T0.AddYears(5));
            True(!wrongCurrency.IsMatched, "unmatched currency scope");
            var wrongUnit = book.Resolve(new CostCode("COST-A"), "kg", "USD", T0.AddYears(5));
            True(!wrongUnit.IsMatched, "unmatched unit scope");
            var wrongCode = book.Resolve(new CostCode("COST-X"), "m3", "USD", T0.AddYears(5));
            True(!wrongCode.IsMatched, "unmatched cost-code scope");

            Throws<ArgumentException>(() => book.Resolve(new CostCode("COST-A"), "M3", "USD", T0),
                "lookup unit canonicality");
            Throws<ArgumentException>(() => book.Resolve(new CostCode("COST-A"), "m3", "usd", T0),
                "lookup currency canonicality");
            Throws<ArgumentException>(() => book.Resolve(new CostCode("COST-A"), "m3", "USD",
                DateTime.SpecifyKind(T0, DateTimeKind.Local)), "lookup UTC requirement");
        }

        private static RateItem Item(
            string id,
            string code,
            string unit,
            string currency,
            decimal rate,
            DateTime effective)
        {
            return new RateItem(id, new CostCode(code), unit, currency, rate, effective, "v1");
        }

        private static IEnumerable<RateItem> PureEnumerable(IEnumerable<RateItem> source)
        {
            foreach (var item in source)
                yield return item;
        }

        private static void Matched(RateBookResolution result, string expectedId, decimal expectedRate, string message)
        {
            True(result.IsMatched && result.Item != null, message + " must be matched");
            Equal(expectedId, result.Item!.RateItemId, message + " item id");
            Equal(expectedRate, result.Item.UnitRate, message + " rate");
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
                throw new Exception("RateBook boundary regression: expected " + typeof(T).Name + " for " + message + ".");
            }
            catch (T)
            {
            }
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new Exception("RateBook boundary regression: " + message + ".");
        }

        private static void Equal(int expected, int actual, string message)
        {
            if (expected != actual)
                throw new Exception("RateBook boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(decimal expected, decimal actual, string message)
        {
            if (expected != actual)
                throw new Exception("RateBook boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(string expected, string actual, string message)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new Exception("RateBook boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Equal(DateTimeKind expected, DateTimeKind actual, string message)
        {
            if (expected != actual)
                throw new Exception("RateBook boundary regression: " + message + ". Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CountingEnumerable : IEnumerable<RateItem>
        {
            private readonly int _count;

            internal CountingEnumerable(int count)
            {
                _count = count;
            }

            internal int Yielded { get; private set; }

            public IEnumerator<RateItem> GetEnumerator()
            {
                for (var i = 0; i < _count; i++)
                {
                    Yielded++;
                    yield return Item("S" + i.ToString("D5"), "SC" + i.ToString("D5"), "m3", "USD", i, T0);
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        private sealed class MultiCountSource :
            ICollection<RateItem>,
            IReadOnlyCollection<RateItem>,
            ICollection
        {
            private readonly IReadOnlyList<RateItem> _items;
            private readonly int _genericCount;
            private readonly int _readOnlyCount;
            private readonly int _nonGenericCount;
            private readonly bool _throwOnEnumeration;

            internal MultiCountSource(
                IReadOnlyList<RateItem> items,
                int genericCount,
                int readOnlyCount,
                int nonGenericCount,
                bool throwOnEnumeration)
            {
                _items = items;
                _genericCount = genericCount;
                _readOnlyCount = readOnlyCount;
                _nonGenericCount = nonGenericCount;
                _throwOnEnumeration = throwOnEnumeration;
            }

            internal bool EnumeratorRequested { get; private set; }
            internal int Yielded { get; private set; }

            int ICollection<RateItem>.Count => _genericCount;
            int IReadOnlyCollection<RateItem>.Count => _readOnlyCount;
            int ICollection.Count => _nonGenericCount;
            bool ICollection<RateItem>.IsReadOnly => true;
            bool ICollection.IsSynchronized => false;
            object ICollection.SyncRoot => this;

            public IEnumerator<RateItem> GetEnumerator()
            {
                EnumeratorRequested = true;
                if (_throwOnEnumeration)
                    throw new Exception("Enumeration should not have been requested.");
                return Enumerate().GetEnumerator();
            }

            private IEnumerable<RateItem> Enumerate()
            {
                for (var i = 0; i < _items.Count; i++)
                {
                    Yielded++;
                    yield return _items[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            void ICollection<RateItem>.Add(RateItem item) => throw new NotSupportedException();
            void ICollection<RateItem>.Clear() => throw new NotSupportedException();
            bool ICollection<RateItem>.Contains(RateItem item) => throw new NotSupportedException();
            void ICollection<RateItem>.CopyTo(RateItem[] array, int arrayIndex) => throw new NotSupportedException();
            bool ICollection<RateItem>.Remove(RateItem item) => throw new NotSupportedException();
            void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
        }
    }
}
