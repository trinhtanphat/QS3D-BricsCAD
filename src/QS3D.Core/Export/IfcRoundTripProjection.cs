using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Export
{
    public sealed class IfcRoundTripNumericProperty
    {
        public IfcRoundTripNumericProperty(string name, double value, string unit)
        {
            Name = IfcRoundTripProjectionContract.RequireCanonicalToken(name, nameof(name));
            Value = IfcRoundTripProjectionContract.RequireFinite(value, nameof(value));
            Unit = IfcRoundTripProjectionContract.RequireCanonicalToken(unit, nameof(unit));
        }

        public string Name { get; }
        public double Value { get; }
        public string Unit { get; }
    }

    public sealed class IfcRoundTripProjection
    {
        public IfcRoundTripProjection(
            string qs3dElementId,
            string ifcGlobalId,
            string semanticClassification,
            IEnumerable<IfcRoundTripNumericProperty> dimensions,
            double primaryQuantity,
            string primaryQuantityUnit,
            IEnumerable<string> provenance)
            : this(
                qs3dElementId,
                ifcGlobalId,
                semanticClassification,
                dimensions,
                primaryQuantity,
                primaryQuantityUnit,
                provenance,
                Array.Empty<IfcRoundTripQuantityEvidence>())
        {
        }

        public IfcRoundTripProjection(
            string qs3dElementId,
            string ifcGlobalId,
            string semanticClassification,
            IEnumerable<IfcRoundTripNumericProperty> dimensions,
            double primaryQuantity,
            string primaryQuantityUnit,
            IEnumerable<string> provenance,
            IEnumerable<IfcRoundTripQuantityEvidence> quantityEvidence)
        {
            Qs3dElementId = IfcRoundTripProjectionContract.RequireCanonicalToken(qs3dElementId, nameof(qs3dElementId));
            IfcGlobalId = IfcRoundTripProjectionContract.RequireCanonicalToken(ifcGlobalId, nameof(ifcGlobalId));
            SemanticClassification = IfcRoundTripProjectionContract.RequireCanonicalToken(semanticClassification, nameof(semanticClassification));
            Dimensions = CanonicalizeDimensions(dimensions);
            PrimaryQuantity = IfcRoundTripProjectionContract.RequireFinite(primaryQuantity, nameof(primaryQuantity));
            PrimaryQuantityUnit = IfcRoundTripProjectionContract.RequireCanonicalToken(primaryQuantityUnit, nameof(primaryQuantityUnit));
            Provenance = CanonicalizeProvenance(provenance);
            QuantityEvidence = IfcRoundTripQuantityEvidenceSet.Create(quantityEvidence);
        }

        public string Qs3dElementId { get; }
        public string IfcGlobalId { get; }
        public string SemanticClassification { get; }
        public IReadOnlyList<IfcRoundTripNumericProperty> Dimensions { get; }
        public double PrimaryQuantity { get; }
        public string PrimaryQuantityUnit { get; }
        public IReadOnlyList<string> Provenance { get; }
        public IfcRoundTripQuantityEvidenceSet QuantityEvidence { get; }

        private static IReadOnlyList<IfcRoundTripNumericProperty> CanonicalizeDimensions(IEnumerable<IfcRoundTripNumericProperty> dimensions)
        {
            if (dimensions == null) throw new ArgumentNullException(nameof(dimensions));
            var items = dimensions.ToList();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new ArgumentException("Dimension collection cannot contain null entries.", nameof(dimensions));
                if (!seenNames.Add(item.Name)) throw new ArgumentException("Duplicate dimension name: " + item.Name, nameof(dimensions));
            }
            items.Sort(IfcRoundTripNumericPropertyComparer.Instance);
            return Array.AsReadOnly(items.ToArray());
        }

        private static IReadOnlyList<string> CanonicalizeProvenance(IEnumerable<string> provenance)
        {
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            var items = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var value in provenance)
            {
                var token = IfcRoundTripProjectionContract.RequireCanonicalToken(value, nameof(provenance));
                if (!seen.Add(token)) throw new ArgumentException("Duplicate provenance token: " + token, nameof(provenance));
                items.Add(token);
            }
            if (items.Count == 0) throw new ArgumentException("At least one provenance token is required.", nameof(provenance));
            items.Sort(StringComparer.Ordinal);
            return Array.AsReadOnly(items.ToArray());
        }
    }

    public sealed class IfcRoundTripProjectionSet
    {
        internal const int MaxProjections = 10000;

        private IfcRoundTripProjectionSet(IReadOnlyList<IfcRoundTripProjection> items) { Items = items; }
        public IReadOnlyList<IfcRoundTripProjection> Items { get; }

        public static IfcRoundTripProjectionSet Create(IEnumerable<IfcRoundTripProjection> projections)
        {
            if (projections == null) throw new ArgumentNullException(nameof(projections));

            var knownCount = TryGetKnownCount(projections, out var conflictingKnownCounts, out var negativeKnownCount);
            if (knownCount.HasValue && knownCount.Value > MaxProjections)
                ThrowTooManyProjections();
            if (negativeKnownCount)
                throw new InvalidOperationException("IFC round-trip projection source exposes an invalid negative known Count value.");
            if (conflictingKnownCounts)
                throw new InvalidOperationException("IFC round-trip projection source exposes conflicting known Count values.");

            var items = new List<IfcRoundTripProjection>();
            foreach (var projection in projections)
            {
                if (items.Count == MaxProjections)
                    ThrowTooManyProjections();
                items.Add(projection);
            }

            var ifcGlobalIds = new HashSet<string>(StringComparer.Ordinal);
            var qs3dElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null) throw new ArgumentException("Projection collection cannot contain null entries.", nameof(projections));
                if (!ifcGlobalIds.Add(item.IfcGlobalId)) throw new InvalidOperationException("Duplicate IFC global identity: " + item.IfcGlobalId);
                if (!qs3dElementIds.Add(item.Qs3dElementId)) throw new InvalidOperationException("Duplicate QS3D element identity: " + item.Qs3dElementId);
            }
            items.Sort(IfcRoundTripProjectionComparer.CanonicalOrder);
            return new IfcRoundTripProjectionSet(Array.AsReadOnly(items.ToArray()));
        }

        private static int? TryGetKnownCount(
            IEnumerable<IfcRoundTripProjection> projections,
            out bool conflictingKnownCounts,
            out bool negativeKnownCount)
        {
            conflictingKnownCounts = false;
            negativeKnownCount = false;
            int? knownCount = null;

            if (projections is ICollection<IfcRoundTripProjection> collection)
                knownCount = ObserveKnownCount(knownCount, collection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (projections is IReadOnlyCollection<IfcRoundTripProjection> readOnlyCollection)
                knownCount = ObserveKnownCount(knownCount, readOnlyCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (projections is ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(knownCount, nonGenericCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);

            return knownCount;
        }

        private static int ObserveKnownCount(
            int? current,
            int observed,
            ref bool conflictingKnownCounts,
            ref bool negativeKnownCount)
        {
            if (observed < 0)
                negativeKnownCount = true;
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }

        private static void ThrowTooManyProjections()
        {
            throw new InvalidOperationException(
                "IFC round-trip projection set supports at most " + MaxProjections + " projections.");
        }
    }

    public static class IfcRoundTripProjectionComparer
    {
        internal static readonly IComparer<IfcRoundTripProjection> CanonicalOrder = new ProjectionCanonicalComparer();

        public static bool AreEquivalent(IfcRoundTripProjection expected, IfcRoundTripProjection actual, double absoluteTolerance)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            if (actual == null) throw new ArgumentNullException(nameof(actual));
            if (double.IsNaN(absoluteTolerance) || double.IsInfinity(absoluteTolerance) || absoluteTolerance < 0d)
                throw new ArgumentOutOfRangeException(nameof(absoluteTolerance), "Tolerance must be finite and non-negative.");
            if (!string.Equals(expected.Qs3dElementId, actual.Qs3dElementId, StringComparison.Ordinal)) return false;
            if (!string.Equals(expected.IfcGlobalId, actual.IfcGlobalId, StringComparison.Ordinal)) return false;
            if (!string.Equals(expected.SemanticClassification, actual.SemanticClassification, StringComparison.Ordinal)) return false;
            if (!string.Equals(expected.PrimaryQuantityUnit, actual.PrimaryQuantityUnit, StringComparison.Ordinal)) return false;
            if (!WithinTolerance(expected.PrimaryQuantity, actual.PrimaryQuantity, absoluteTolerance)) return false;
            if (expected.Dimensions.Count != actual.Dimensions.Count) return false;
            for (var index = 0; index < expected.Dimensions.Count; index++)
            {
                var left = expected.Dimensions[index];
                var right = actual.Dimensions[index];
                if (!string.Equals(left.Name, right.Name, StringComparison.Ordinal)) return false;
                if (!string.Equals(left.Unit, right.Unit, StringComparison.Ordinal)) return false;
                if (!WithinTolerance(left.Value, right.Value, absoluteTolerance)) return false;
            }
            if (expected.Provenance.Count != actual.Provenance.Count) return false;
            for (var index = 0; index < expected.Provenance.Count; index++)
                if (!string.Equals(expected.Provenance[index], actual.Provenance[index], StringComparison.Ordinal)) return false;
            if (!IfcRoundTripQuantityEvidenceSetComparer.AreEquivalent(expected.QuantityEvidence, actual.QuantityEvidence, absoluteTolerance)) return false;
            return true;
        }

        private static bool WithinTolerance(double left, double right, double absoluteTolerance)
        {
            if (left.Equals(right)) return true;
            return Math.Abs(left - right) <= absoluteTolerance;
        }

        private sealed class ProjectionCanonicalComparer : IComparer<IfcRoundTripProjection>
        {
            public int Compare(IfcRoundTripProjection? x, IfcRoundTripProjection? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                var byIfc = StringComparer.Ordinal.Compare(x.IfcGlobalId, y.IfcGlobalId);
                if (byIfc != 0) return byIfc;
                var byQs3dIgnoreCase = StringComparer.OrdinalIgnoreCase.Compare(x.Qs3dElementId, y.Qs3dElementId);
                if (byQs3dIgnoreCase != 0) return byQs3dIgnoreCase;
                return StringComparer.Ordinal.Compare(x.Qs3dElementId, y.Qs3dElementId);
            }
        }
    }

    internal static class IfcRoundTripProjectionContract
    {
        internal static string RequireCanonicalToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A non-empty token is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal)) throw new ArgumentException("Token must not contain surrounding whitespace.", parameterName);
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (char.IsControl(character))
                    throw new ArgumentException("Token must not contain control characters.", parameterName);

                if (char.IsHighSurrogate(character))
                {
                    if (index + 1 >= value.Length || !char.IsLowSurrogate(value[index + 1]))
                        throw new ArgumentException("Token must contain well-formed UTF-16.", parameterName);
                    index++;
                    continue;
                }

                if (char.IsLowSurrogate(character))
                    throw new ArgumentException("Token must contain well-formed UTF-16.", parameterName);
            }
            return value;
        }

        internal static double RequireFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(parameterName, "Numeric values must be finite.");
            return value == 0d ? 0d : value;
        }
    }

    internal sealed class IfcRoundTripNumericPropertyComparer : IComparer<IfcRoundTripNumericProperty>
    {
        internal static readonly IfcRoundTripNumericPropertyComparer Instance = new IfcRoundTripNumericPropertyComparer();
        private IfcRoundTripNumericPropertyComparer() { }

        public int Compare(IfcRoundTripNumericProperty? x, IfcRoundTripNumericProperty? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            var byNameIgnoreCase = StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name);
            if (byNameIgnoreCase != 0) return byNameIgnoreCase;
            return StringComparer.Ordinal.Compare(x.Name, y.Name);
        }
    }
}
