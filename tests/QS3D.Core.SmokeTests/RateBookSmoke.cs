using System;
using System.Collections.Generic;
using System.Globalization;
using QS3D.Core.Cost;

namespace QS3D.Core.SmokeTests
{
    internal static class RateBookSmoke
    {
        internal static void Run()
        {
            DeterministicOrderingAndLatestLookup();
            CanonicalZeroUnitRate();
            ExplicitUnmatchedState();
            SnapshotIsolationAndReadOnlyView();
            DuplicateAndAmbiguousRatesFailClosed();
            LargeSingleScopeUsesIndexedTimestampUniqueness();
            CostRatePercentagePrecisionFailsClosed();
            ProgressRetentionPercentagePrecisionFailsClosed();
            CostMonetaryMultiplicationPrecisionFailsClosed();
            InvalidInputsFailClosed();
        }

        private static void DeterministicOrderingAndLatestLookup()
        {
            var jan = Utc(2026, 1, 1);
            var feb = Utc(2026, 2, 1);
            var concreteEarly = Item("RATE-CONC-1", "CONC", "m3", "VND", 1500000m, jan, "v1");
            var concreteLate = Item("RATE-CONC-2", "CONC", "m3", "VND", 1600000m, feb, "v2");
            var steel = Item("RATE-STEEL-1", "STEEL", "kg", "VND", 18000m, jan, "v1");

            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("vi-VN");
                var left = new RateBook("BOOK-2026", new[] { steel, concreteLate, concreteEarly });
                CultureInfo.CurrentCulture = new CultureInfo("en-US");
                var right = new RateBook("BOOK-2026", new[] { concreteEarly, steel, concreteLate });

                SequenceIds(left.Items, "RATE-CONC-1", "RATE-CONC-2", "RATE-STEEL-1");
                SequenceIds(right.Items, "RATE-CONC-1", "RATE-CONC-2", "RATE-STEEL-1");

                var january = left.Resolve(new CostCode("conc"), "m3", "VND", Utc(2026, 1, 15));
                True(january.IsMatched, "January lookup should match the first concrete rate.");
                Equal("RATE-CONC-1", january.Item!.RateItemId, "January lookup rate id mismatch.");
                Equal("CONC", january.CostCode.Value, "Matched resolution must expose the selected catalog CostCode identity.");
                Equal(1500000m, january.Item.UnitRate, "January lookup unit rate mismatch.");
                Equal("v1", january.Item.Version, "January lookup version mismatch.");

                var february = left.Resolve(new CostCode("CONC"), "m3", "VND", Utc(2026, 2, 15));
                True(february.IsMatched, "February lookup should select the latest eligible concrete rate.");
                Equal("RATE-CONC-2", february.Item!.RateItemId, "February lookup rate id mismatch.");
                Equal(1600000m, february.Item.UnitRate, "February lookup unit rate mismatch.");
                Equal(Utc(2026, 2, 15), february.AsOfUtc, "Resolution must retain the canonical as-of timestamp.");
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        private static void CanonicalZeroUnitRate()
        {
            var negativeZero = new decimal(0, 0, 0, true, 0);
            var item = Item("RATE-ZERO", "ZERO", "ea", "USD", negativeZero, Utc(2026, 1, 1), "v1");
            var expectedBits = decimal.GetBits(0m);
            var actualBits = decimal.GetBits(item.UnitRate);

            Equal(0m, item.UnitRate, "Zero unit rate value mismatch.");
            Equal(expectedBits.Length, actualBits.Length, "Decimal bit-vector length mismatch.");
            for (var i = 0; i < expectedBits.Length; i++)
                Equal(expectedBits[i], actualBits[i], "Zero unit rate must use canonical positive decimal representation at bit index " + i + ".");
        }

        private static void ExplicitUnmatchedState()
        {
            var book = new RateBook("BOOK", new[]
            {
                Item("RATE-1", "CONC", "m3", "VND", 100m, Utc(2026, 2, 1), "v1")
            });

            var beforeEffective = book.Resolve(new CostCode("CONC"), "m3", "VND", Utc(2026, 1, 1));
            True(!beforeEffective.IsMatched, "Lookup before the first effective rate must remain explicitly unmatched.");
            Equal(RateBookResolutionKind.Unmatched, beforeEffective.Kind, "Unmatched resolution kind mismatch.");
            True(beforeEffective.Item == null, "Unmatched resolution must not invent a rate item.");

            var unknown = book.Resolve(new CostCode("STEEL"), "kg", "VND", Utc(2026, 3, 1));
            True(!unknown.IsMatched && unknown.Item == null, "Unknown rate scope must remain explicitly unmatched.");
        }

        private static void SnapshotIsolationAndReadOnlyView()
        {
            var input = new List<RateItem>
            {
                Item("RATE-1", "CONC", "m3", "VND", 100m, Utc(2026, 1, 1), "v1")
            };
            var book = new RateBook("BOOK", input);
            input.Clear();

            Equal(1, book.Items.Count, "RateBook must detach its item snapshot from caller list mutation.");
            var list = book.Items as IList<RateItem>;
            True(list != null && list.IsReadOnly, "RateBook item projection must be read-only.");
            Throws<NotSupportedException>(() => list!.Add(Item("LATE", "CONC", "m3", "VND", 1m, Utc(2027, 1, 1), "v2")));
            Equal(1, book.Items.Count, "Rejected mutation must not change RateBook items.");
        }

        private static void DuplicateAndAmbiguousRatesFailClosed()
        {
            var jan = Utc(2026, 1, 1);
            Throws<ArgumentException>(() => new RateBook("BOOK", new[]
            {
                Item("RATE-1", "CONC", "m3", "VND", 100m, jan, "v1"),
                Item("rate-1", "STEEL", "kg", "VND", 20m, jan, "v1")
            }));

            Throws<ArgumentException>(() => new RateBook("BOOK", new[]
            {
                Item("RATE-A", "CONC", "m3", "VND", 100m, jan, "v1"),
                Item("RATE-B", "conc", "m3", "VND", 110m, jan, "v2")
            }));
        }

        private static void LargeSingleScopeUsesIndexedTimestampUniqueness()
        {
            const int count = 4096;
            var start = Utc(2026, 1, 1);
            var items = new List<RateItem>(count);
            for (var i = count - 1; i >= 0; i--)
            {
                items.Add(Item(
                    "RATE-LARGE-" + i.ToString("D4", CultureInfo.InvariantCulture),
                    "CONC",
                    "m3",
                    "VND",
                    i,
                    start.AddTicks(i),
                    "v1"));
            }

            var book = new RateBook("BOOK-LARGE", items);
            Equal(count, book.Items.Count, "Large single-scope RateBook count mismatch.");
            Equal("RATE-LARGE-0000", book.Items[0].RateItemId, "Large scope ordering must remain effective-time deterministic.");
            Equal("RATE-LARGE-4095", book.Items[count - 1].RateItemId, "Large scope final ordering mismatch.");

            var resolved = book.Resolve(new CostCode("CONC"), "m3", "VND", start.AddTicks(count));
            True(resolved.IsMatched, "Large scope latest lookup should remain matched.");
            Equal("RATE-LARGE-4095", resolved.Item!.RateItemId, "Large scope latest lookup semantics changed.");

            items.Add(Item("RATE-LARGE-DUP", "conc", "m3", "VND", 1m, start.AddTicks(count - 1), "v2"));
            Throws<ArgumentException>(() => new RateBook("BOOK-LARGE-DUP", items));
        }

        private static void CostRatePercentagePrecisionFailsClosed()
        {
            var component = new CostResourceComponent("MAT", "Material", "ea", 1m, 100m);
            var components = new[] { component };
            var minimumPositive = 0.0000000000000000000000000001m;

            Throws<ArgumentOutOfRangeException>(() =>
                new CostRateBuildUp("BUILD-OH", new CostCode("CONC"), "ea", "VND", components, minimumPositive, 0m));
            Throws<ArgumentOutOfRangeException>(() =>
                new CostRateBuildUp("BUILD-PROFIT", new CostCode("CONC"), "ea", "VND", components, 0m, minimumPositive));

            var zero = new CostRateBuildUp("BUILD-ZERO", new CostCode("CONC"), "ea", "VND", components);
            Equal(0m, zero.OverheadUnitCost, "Zero overhead should remain accepted.");
            Equal(0m, zero.ProfitUnitCost, "Zero profit should remain accepted.");
            Equal(100m, zero.UnitRate, "Zero percentage build-up rate changed.");

            var normal = new CostRateBuildUp("BUILD-NORMAL", new CostCode("CONC"), "ea", "VND", components, 10m, 10m);
            Equal(100m, normal.DirectUnitCost, "Normal build-up direct cost mismatch.");
            Equal(10m, normal.OverheadUnitCost, "Normal build-up overhead mismatch.");
            Equal(11m, normal.ProfitUnitCost, "Normal build-up profit mismatch.");
            Equal(121m, normal.UnitRate, "Normal build-up unit rate mismatch.");
        }

        private static void ProgressRetentionPercentagePrecisionFailsClosed()
        {
            var contracts = new[] { new ProgressContractItem("ITEM", "ea", 1m, 100m) };
            var claims = new[] { new ProgressClaimLine("ITEM", 0m, 1m) };
            var minimumPositive = 0.0000000000000000000000000001m;
            var service = new ProgressClaimService();

            Throws<ArgumentOutOfRangeException>(() => service.Evaluate(contracts, claims, minimumPositive));

            var zero = service.Evaluate(contracts, claims, 0m);
            Equal(100m, zero.GrossCertifiedThisPeriod, "Zero-retention gross value mismatch.");
            Equal(0m, zero.RetentionThisPeriod, "Zero retention should remain accepted.");
            Equal(100m, zero.NetCertifiedThisPeriod, "Zero-retention net value mismatch.");

            var normal = service.Evaluate(contracts, claims, 10m);
            Equal(10m, normal.RetentionThisPeriod, "Normal retention value mismatch.");
            Equal(90m, normal.NetCertifiedThisPeriod, "Normal retention net value mismatch.");
        }

        private static void CostMonetaryMultiplicationPrecisionFailsClosed()
        {
            const decimal minimumPositive = 0.0000000000000000000000000001m;

            var underflowComponent = new CostResourceComponent("TINY", "Tiny", "ea", minimumPositive, 0.1m);
            Throws<OverflowException>(() => { var _ = underflowComponent.ExtendedUnitCost; });

            var zeroComponent = new CostResourceComponent("ZERO", "Zero", "ea", 0m, minimumPositive);
            Equal(0m, zeroComponent.ExtendedUnitCost, "Zero resource quantity should remain zero.");
            var normalComponent = new CostResourceComponent("NORMAL", "Normal", "ea", 2m, 3m);
            Equal(6m, normalComponent.ExtendedUnitCost, "Normal resource extended cost changed.");

            var tinyDirect = new[] { new CostResourceComponent("BASE", "Base", "ea", 1m, minimumPositive) };
            Throws<OverflowException>(() =>
                new CostRateBuildUp("BUILD-OH-MUL", new CostCode("CONC"), "ea", "VND", tinyDirect, 10m, 0m));
            Throws<OverflowException>(() =>
                new CostRateBuildUp("BUILD-PROFIT-MUL", new CostCode("CONC"), "ea", "VND", tinyDirect, 0m, 10m));

            var tenderRequirements = new[] { new TenderRequirement("ITEM", "Item", "ea", minimumPositive) };
            var tenderBids = new[]
            {
                new TenderBid("BID", "Bidder", "VND", new[] { new TenderQuoteLine("ITEM", 0.1m) })
            };
            Throws<OverflowException>(() => new TenderEvaluationService().Evaluate(tenderRequirements, tenderBids));

            var normalTender = new TenderEvaluationService().Evaluate(
                new[] { new TenderRequirement("ITEM", "Item", "ea", 2m) },
                new[] { new TenderBid("BID", "Bidder", "VND", new[] { new TenderQuoteLine("ITEM", 3m) }) });
            Equal(6m, normalTender[0].EvaluatedTotal, "Normal tender evaluated total changed.");

            var progress = new ProgressClaimService();
            Throws<OverflowException>(() => progress.Evaluate(
                new[] { new ProgressContractItem("ITEM", "ea", minimumPositive, 0.1m) },
                new[] { new ProgressClaimLine("ITEM", 0m, minimumPositive) },
                0m));

            Throws<OverflowException>(() => progress.Evaluate(
                new[] { new ProgressContractItem("ITEM", "ea", 1m, minimumPositive) },
                new[] { new ProgressClaimLine("ITEM", 0m, 1m) },
                10m));

            var zeroProgress = progress.Evaluate(
                new[] { new ProgressContractItem("ITEM", "ea", 1m, minimumPositive) },
                new[] { new ProgressClaimLine("ITEM", 0m, 0m) },
                10m);
            Equal(0m, zeroProgress.GrossCertifiedThisPeriod, "Zero progress quantity should keep gross zero.");
            Equal(0m, zeroProgress.RetentionThisPeriod, "Zero progress gross should keep retention zero.");
        }

        private static void InvalidInputsFailClosed()
        {
            Throws<ArgumentException>(() => new CostCode(" CONC"));
            Throws<ArgumentException>(() => new CostCode("CON C"));
            Throws<ArgumentNullException>(() => new RateItem("RATE", null!, "m3", "VND", 1m, Utc(2026, 1, 1), "v1"));
            Throws<ArgumentException>(() => Item("RATE", "CONC", "M3", "VND", 1m, Utc(2026, 1, 1), "v1"));
            Throws<ArgumentException>(() => Item("RATE", "CONC", "m3", "vnd", 1m, Utc(2026, 1, 1), "v1"));
            Throws<ArgumentException>(() => Item("RATE", "CONC", "m3", "VN1", 1m, Utc(2026, 1, 1), "v1"));
            Throws<ArgumentOutOfRangeException>(() => Item("RATE", "CONC", "m3", "VND", -1m, Utc(2026, 1, 1), "v1"));
            Throws<ArgumentException>(() => Item("RATE", "CONC", "m3", "VND", 1m, DateTime.SpecifyKind(new DateTime(2026, 1, 1), DateTimeKind.Unspecified), "v1"));
            Throws<ArgumentException>(() => Item("RATE", "CONC", "m3", "VND", 1m, Utc(2026, 1, 1), " v1"));
            Throws<ArgumentException>(() => new RateBook(" BOOK", Array.Empty<RateItem>()));
            Throws<ArgumentNullException>(() => new RateBook("BOOK", null!));
            Throws<ArgumentException>(() => new RateBook("BOOK", new RateItem[] { null! }));

            var book = new RateBook("BOOK", new[] { Item("RATE", "CONC", "m3", "VND", 1m, Utc(2026, 1, 1), "v1") });
            Throws<ArgumentNullException>(() => book.Resolve(null!, "m3", "VND", Utc(2026, 1, 2)));
            Throws<ArgumentException>(() => book.Resolve(new CostCode("CONC"), "M3", "VND", Utc(2026, 1, 2)));
            Throws<ArgumentException>(() => book.Resolve(new CostCode("CONC"), "m3", "vnd", Utc(2026, 1, 2)));
            Throws<ArgumentException>(() => book.Resolve(new CostCode("CONC"), "m3", "VND", new DateTime(2026, 1, 2)));
        }

        private static RateItem Item(
            string id,
            string costCode,
            string unit,
            string currency,
            decimal rate,
            DateTime effectiveFromUtc,
            string version) =>
            new RateItem(id, new CostCode(costCode), unit, currency, rate, effectiveFromUtc, version);

        private static DateTime Utc(int year, int month, int day) =>
            new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);

        private static void SequenceIds(IReadOnlyList<RateItem> items, params string[] expected)
        {
            Equal(expected.Length, items.Count, "RateBook deterministic order count mismatch.");
            for (var i = 0; i < expected.Length; i++)
                Equal(expected[i], items[i].RateItemId, "RateBook deterministic order mismatch at index " + i + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected: " + expected + "; actual: " + actual + ".");
        }

        private static void True(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Throws<TException>(Action action) where TException : Exception
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
    }
}
