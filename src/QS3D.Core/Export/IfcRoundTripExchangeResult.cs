using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Export
{
    public enum IfcRoundTripResultState
    {
        Supported = 0,
        SupportedLossy = 1,
        Unmapped = 2,
        Unsupported = 3,
        InvalidOrAmbiguous = 4
    }

    public sealed class IfcRoundTripExchangeResult
    {
        public IfcRoundTripExchangeResult(
            string externalObjectId,
            IfcRoundTripResultState state,
            IfcRoundTripProjection? projection,
            string? stateDetail = null,
            string? classificationIdentity = null,
            string? mappingRelationIdentity = null,
            string? costItemRelationIdentity = null)
        {
            ExternalObjectId = RequireCanonicalToken(externalObjectId, nameof(externalObjectId));
            State = RequireDefinedState(state);
            Projection = projection;
            StateDetail = RequireOptionalCanonicalToken(stateDetail, nameof(stateDetail));
            ClassificationIdentity = RequireOptionalCanonicalToken(classificationIdentity, nameof(classificationIdentity));
            MappingRelationIdentity = RequireOptionalCanonicalToken(mappingRelationIdentity, nameof(mappingRelationIdentity));
            CostItemRelationIdentity = RequireOptionalCanonicalToken(costItemRelationIdentity, nameof(costItemRelationIdentity));

            ValidateStateContract();
        }

        public string ExternalObjectId { get; }
        public IfcRoundTripResultState State { get; }
        public IfcRoundTripProjection? Projection { get; }
        public string? StateDetail { get; }
        public string? ClassificationIdentity { get; }
        public string? MappingRelationIdentity { get; }
        public string? CostItemRelationIdentity { get; }

        public bool HasTrustedQs3dIdentity => Projection != null;
        public bool IsLosslessSupported => State == IfcRoundTripResultState.Supported;

        private void ValidateStateContract()
        {
            var supported = State == IfcRoundTripResultState.Supported || State == IfcRoundTripResultState.SupportedLossy;

            if (supported && Projection == null)
                throw new ArgumentException("Supported IFC exchange results require a canonical QS3D round-trip projection.", nameof(Projection));

            if (!supported && Projection != null)
                throw new ArgumentException("Unmapped, unsupported, or invalid IFC exchange results cannot carry a trusted QS3D projection.", nameof(Projection));

            if (Projection != null && !string.Equals(ExternalObjectId, Projection.IfcGlobalId, StringComparison.Ordinal))
                throw new ArgumentException("External object identity must match the canonical projection IFC identity.", nameof(Projection));

            if (State == IfcRoundTripResultState.SupportedLossy && StateDetail == null)
                throw new ArgumentException("Supported-lossy IFC exchange results require an explicit loss reason.", nameof(StateDetail));

            if (State == IfcRoundTripResultState.Supported && StateDetail != null)
                throw new ArgumentException("Lossless supported IFC exchange results cannot carry a lossy state detail.", nameof(StateDetail));

            if (Projection == null && (MappingRelationIdentity != null || CostItemRelationIdentity != null))
                throw new ArgumentException("Mapping or cost relation evidence requires a trusted QS3D projection.", nameof(Projection));
        }

        private static IfcRoundTripResultState RequireDefinedState(IfcRoundTripResultState value)
        {
            if (!Enum.IsDefined(typeof(IfcRoundTripResultState), value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "IFC round-trip result state must be defined.");
            return value;
        }

        private static string RequireCanonicalToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("A non-empty token is required.", parameterName);
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new ArgumentException("Token must not contain surrounding whitespace.", parameterName);
            for (var index = 0; index < value.Length; index++)
                if (char.IsControl(value[index]))
                    throw new ArgumentException("Token must not contain control characters.", parameterName);
            return value;
        }

        private static string? RequireOptionalCanonicalToken(string? value, string parameterName)
        {
            return value == null ? null : RequireCanonicalToken(value, parameterName);
        }
    }

    public sealed class IfcRoundTripExchangeResultSet
    {
        private IfcRoundTripExchangeResultSet(IReadOnlyList<IfcRoundTripExchangeResult> items)
        {
            Items = items;
        }

        public IReadOnlyList<IfcRoundTripExchangeResult> Items { get; }

        public static IfcRoundTripExchangeResultSet Create(IEnumerable<IfcRoundTripExchangeResult> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            var items = results.ToList();
            var externalIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                if (item == null)
                    throw new ArgumentException("IFC exchange result collection cannot contain null entries.", nameof(results));
                if (!externalIds.Add(item.ExternalObjectId))
                    throw new InvalidOperationException(
                        "Duplicate external object identity must be represented as a single InvalidOrAmbiguous result: " + item.ExternalObjectId);
            }

            items.Sort(IfcRoundTripExchangeResultComparer.Instance);
            return new IfcRoundTripExchangeResultSet(Array.AsReadOnly(items.ToArray()));
        }
    }

    internal sealed class IfcRoundTripExchangeResultComparer : IComparer<IfcRoundTripExchangeResult>
    {
        internal static readonly IfcRoundTripExchangeResultComparer Instance = new IfcRoundTripExchangeResultComparer();

        private IfcRoundTripExchangeResultComparer()
        {
        }

        public int Compare(IfcRoundTripExchangeResult? x, IfcRoundTripExchangeResult? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            var byExternalIdentity = StringComparer.Ordinal.Compare(x.ExternalObjectId, y.ExternalObjectId);
            if (byExternalIdentity != 0) return byExternalIdentity;
            return x.State.CompareTo(y.State);
        }
    }
}
