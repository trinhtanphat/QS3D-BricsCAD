using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Cost
{
    public sealed class CostCode : IEquatable<CostCode>
    {
        public CostCode(string value)
        {
            Value = RateBookContract.RequireToken(value, nameof(value));
        }

        public string Value { get; }

        public bool Equals(CostCode? other) =>
            other != null && StringComparer.OrdinalIgnoreCase.Equals(Value, other.Value);

        public override bool Equals(object? obj) => Equals(obj as CostCode);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public override string ToString() => Value;
    }

    public sealed class RateItem
    {
        public RateItem(
            string rateItemId,
            CostCode costCode,
            string unit,
            string currency,
            decimal unitRate,
            DateTime effectiveFromUtc,
            string version)
        {
            RateItemId = RateBookContract.RequireToken(rateItemId, nameof(rateItemId));
            CostCode = costCode ?? throw new ArgumentNullException(nameof(costCode));
            Unit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            Currency = RateBookContract.RequireCurrency(currency, nameof(currency));
            if (unitRate < 0m)
                throw new ArgumentOutOfRangeException(nameof(unitRate), "Rate item unit rate must be non-negative.");
            UnitRate = unitRate == 0m ? 0m : unitRate;
            EffectiveFromUtc = RateBookContract.RequireUtc(effectiveFromUtc, nameof(effectiveFromUtc));
            Version = RateBookContract.RequireToken(version, nameof(version));
        }

        public string RateItemId { get; }
        public CostCode CostCode { get; }
        public string Unit { get; }
        public string Currency { get; }
        public decimal UnitRate { get; }
        public DateTime EffectiveFromUtc { get; }
        public string Version { get; }
    }

    public enum RateBookResolutionKind
    {
        Unmatched = 0,
        Matched = 1
    }

    public sealed class RateBookResolution
    {
        private RateBookResolution(
            RateBookResolutionKind kind,
            CostCode costCode,
            string unit,
            string currency,
            DateTime asOfUtc,
            RateItem? item)
        {
            Kind = kind;
            CostCode = costCode;
            Unit = unit;
            Currency = currency;
            AsOfUtc = asOfUtc;
            Item = item;
        }

        public RateBookResolutionKind Kind { get; }
        public bool IsMatched => Kind == RateBookResolutionKind.Matched;
        public CostCode CostCode { get; }
        public string Unit { get; }
        public string Currency { get; }
        public DateTime AsOfUtc { get; }
        public RateItem? Item { get; }

        internal static RateBookResolution Matched(
            CostCode costCode,
            string unit,
            string currency,
            DateTime asOfUtc,
            RateItem item) =>
            new RateBookResolution(RateBookResolutionKind.Matched, costCode, unit, currency, asOfUtc, item);

        internal static RateBookResolution Unmatched(
            CostCode costCode,
            string unit,
            string currency,
            DateTime asOfUtc) =>
            new RateBookResolution(RateBookResolutionKind.Unmatched, costCode, unit, currency, asOfUtc, null);
    }

    public sealed class RateBook
    {
        internal const int MaxItems = 10000;

        private readonly Dictionary<string, List<RateItem>> _byScope;

        public RateBook(string rateBookId, IEnumerable<RateItem> items)
        {
            RateBookId = RateBookContract.RequireToken(rateBookId, nameof(rateBookId));
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (TryGetKnownCount(items, out var knownCount) && knownCount > MaxItems)
                ThrowTooManyItems();

            var snapshot = new List<RateItem>();
            var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _byScope = new Dictionary<string, List<RateItem>>(StringComparer.OrdinalIgnoreCase);
            var effectiveTimesByScope = new Dictionary<string, HashSet<DateTime>>(StringComparer.OrdinalIgnoreCase);

            var index = 0;
            foreach (var item in items)
            {
                if (index == MaxItems)
                    ThrowTooManyItems();
                if (item == null)
                    throw new ArgumentException("Rate book contains a null item at index " + index + ".", nameof(items));
                if (!itemIds.Add(item.RateItemId))
                    throw new ArgumentException("Duplicate rate item id: " + item.RateItemId + ".", nameof(items));

                var scopeKey = RateBookContract.ScopeKey(item.CostCode, item.Unit, item.Currency);
                if (!_byScope.TryGetValue(scopeKey, out var scopedItems))
                {
                    scopedItems = new List<RateItem>();
                    _byScope.Add(scopeKey, scopedItems);
                    effectiveTimesByScope.Add(scopeKey, new HashSet<DateTime>());
                }

                if (!effectiveTimesByScope[scopeKey].Add(item.EffectiveFromUtc))
                    throw new ArgumentException(
                        "Ambiguous rate items share the same cost code, unit, currency and effective timestamp: " +
                        item.CostCode.Value + "/" + item.Unit + "/" + item.Currency + "/" +
                        item.EffectiveFromUtc.ToString("O") + ".",
                        nameof(items));

                scopedItems.Add(item);
                snapshot.Add(item);
                index++;
            }

            foreach (var pair in _byScope)
                pair.Value.Sort(CompareEffectiveItems);

            snapshot.Sort(CompareItems);
            Items = new ReadOnlyCollection<RateItem>(snapshot.ToArray());
        }

        public string RateBookId { get; }
        public IReadOnlyList<RateItem> Items { get; }

        public RateBookResolution Resolve(CostCode costCode, string unit, string currency, DateTime asOfUtc)
        {
            if (costCode == null) throw new ArgumentNullException(nameof(costCode));
            var canonicalUnit = RateBookContract.RequireLowerToken(unit, nameof(unit));
            var canonicalCurrency = RateBookContract.RequireCurrency(currency, nameof(currency));
            var canonicalAsOf = RateBookContract.RequireUtc(asOfUtc, nameof(asOfUtc));
            var scopeKey = RateBookContract.ScopeKey(costCode, canonicalUnit, canonicalCurrency);

            if (!_byScope.TryGetValue(scopeKey, out var scopedItems))
                return RateBookResolution.Unmatched(costCode, canonicalUnit, canonicalCurrency, canonicalAsOf);

            RateItem? match = null;
            for (var i = 0; i < scopedItems.Count; i++)
            {
                var candidate = scopedItems[i];
                if (candidate.EffectiveFromUtc > canonicalAsOf) break;
                match = candidate;
            }

            return match == null
                ? RateBookResolution.Unmatched(costCode, canonicalUnit, canonicalCurrency, canonicalAsOf)
                : RateBookResolution.Matched(match.CostCode, canonicalUnit, canonicalCurrency, canonicalAsOf, match);
        }

        private static bool TryGetKnownCount(IEnumerable<RateItem> items, out int count)
        {
            if (items is ICollection<RateItem> collection)
            {
                count = collection.Count;
                return true;
            }

            if (items is IReadOnlyCollection<RateItem> readOnlyCollection)
            {
                count = readOnlyCollection.Count;
                return true;
            }

            if (items is ICollection nonGenericCollection)
            {
                count = nonGenericCollection.Count;
                return true;
            }

            count = 0;
            return false;
        }

        private static void ThrowTooManyItems()
        {
            throw new InvalidOperationException(
                "Rate book supports at most " + MaxItems + " rate items.");
        }

        private static int CompareEffectiveItems(RateItem left, RateItem right)
        {
            var compare = left.EffectiveFromUtc.CompareTo(right.EffectiveFromUtc);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.RateItemId, right.RateItemId);
            return compare != 0 ? compare : StringComparer.Ordinal.Compare(left.RateItemId, right.RateItemId);
        }

        private static int CompareItems(RateItem left, RateItem right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.CostCode.Value, right.CostCode.Value);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.CostCode.Value, right.CostCode.Value);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Unit, right.Unit);
            if (compare != 0) return compare;
            compare = StringComparer.Ordinal.Compare(left.Currency, right.Currency);
            if (compare != 0) return compare;
            return CompareEffectiveItems(left, right);
        }
    }

    internal static class RateBookContract
    {
        internal static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Rate identity token is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Rate identity token must not contain surrounding whitespace.", parameterName);

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]) || char.IsWhiteSpace(value[i]))
                    throw new ArgumentException("Rate identity token must not contain whitespace or control characters.", parameterName);
            }
            return value;
        }

        internal static string RequireLowerToken(string value, string parameterName)
        {
            value = RequireToken(value, parameterName);
            if (!string.Equals(value, value.ToLowerInvariant(), StringComparison.Ordinal))
                throw new ArgumentException("Rate unit token must use canonical lower-case text.", parameterName);
            return value;
        }

        internal static string RequireCurrency(string value, string parameterName)
        {
            value = RequireToken(value, parameterName);
            if (value.Length != 3)
                throw new ArgumentException("Rate currency must contain exactly three upper-case ASCII letters.", parameterName);
            for (var i = 0; i < value.Length; i++)
            {
                if (value[i] < 'A' || value[i] > 'Z')
                    throw new ArgumentException("Rate currency must contain exactly three upper-case ASCII letters.", parameterName);
            }
            return value;
        }

        internal static DateTime RequireUtc(DateTime value, string parameterName)
        {
            if (value.Kind != DateTimeKind.Utc)
                throw new ArgumentException("Rate effective and lookup timestamps must be UTC.", parameterName);
            return value;
        }

        internal static string ScopeKey(CostCode costCode, string unit, string currency) =>
            costCode.Value + "\u001f" + unit + "\u001f" + currency;
    }
}
