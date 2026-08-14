using System;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripExchangeResultSmoke
    {
        internal static void Run()
        {
            SupportedRetainsTrustedProjectionAndRelations();
            UnmappedRetainsExternalEvidenceWithoutQs3dIdentity();
            LossyStateCannotMasqueradeAsLossless();
            RejectsInvalidStateAndIdentityContracts();
            CanonicalizesResultSetOrderingAndRejectsDuplicates();
        }

        private static void SupportedRetainsTrustedProjectionAndRelations()
        {
            var projection = CreateProjection("BEAM-01", "ifc-beam-01", "IfcBeam");
            var result = new IfcRoundTripExchangeResult(
                "ifc-beam-01",
                IfcRoundTripResultState.Supported,
                projection,
                classificationIdentity: "class:beam",
                mappingRelationIdentity: "mapping:beam-wbs",
                costItemRelationIdentity: "cost:beam-01");

            Require(result.State == IfcRoundTripResultState.Supported, "Supported IFC result lost its state.");
            Require(result.HasTrustedQs3dIdentity, "Supported IFC result lost its trusted QS3D identity relation.");
            Require(result.IsLosslessSupported, "Supported IFC result was not reported as lossless supported.");
            Require(ReferenceEquals(result.Projection, projection), "Supported IFC result replaced its canonical projection.");
            Require(result.ClassificationIdentity == "class:beam", "Supported IFC result lost classification identity.");
            Require(result.MappingRelationIdentity == "mapping:beam-wbs", "Supported IFC result lost mapping relation identity.");
            Require(result.CostItemRelationIdentity == "cost:beam-01", "Supported IFC result lost cost relation identity.");
        }

        private static void UnmappedRetainsExternalEvidenceWithoutQs3dIdentity()
        {
            var result = new IfcRoundTripExchangeResult(
                "ifc-unknown-01",
                IfcRoundTripResultState.Unmapped,
                null,
                stateDetail: "No trusted QS3D identity",
                classificationIdentity: "external:unknown-class");

            Require(result.State == IfcRoundTripResultState.Unmapped, "Unknown external object did not remain explicitly unmapped.");
            Require(!result.HasTrustedQs3dIdentity, "Unmapped external object fabricated a trusted QS3D identity.");
            Require(result.Projection == null, "Unmapped external object fabricated a canonical QS3D projection.");
            Require(result.ClassificationIdentity == "external:unknown-class", "Unmapped external object lost supported classification evidence.");
            Require(result.MappingRelationIdentity == null, "Unmapped external object invented a mapping relation.");
            Require(result.CostItemRelationIdentity == null, "Unmapped external object invented a cost relation.");
        }

        private static void LossyStateCannotMasqueradeAsLossless()
        {
            var projection = CreateProjection("PLATE-01", "ifc-plate-01", "IfcPlate");
            var lossy = new IfcRoundTripExchangeResult(
                "ifc-plate-01",
                IfcRoundTripResultState.SupportedLossy,
                projection,
                stateDetail: "Adjustment provenance not representable");

            Require(lossy.State == IfcRoundTripResultState.SupportedLossy, "Lossy IFC result lost its explicit state.");
            Require(!lossy.IsLosslessSupported, "Lossy IFC result was reported as lossless.");
            Require(lossy.StateDetail == "Adjustment provenance not representable", "Lossy IFC result lost its loss reason.");

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-plate-01",
                IfcRoundTripResultState.SupportedLossy,
                projection));

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-plate-01",
                IfcRoundTripResultState.Supported,
                projection,
                stateDetail: "Unexpected loss detail"));
        }

        private static void RejectsInvalidStateAndIdentityContracts()
        {
            var projection = CreateProjection("COLUMN-01", "ifc-column-01", "IfcColumn");

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-other-01",
                IfcRoundTripResultState.Supported,
                projection));

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-column-01",
                IfcRoundTripResultState.Unsupported,
                projection,
                stateDetail: "Unsupported class"));

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-unmapped-01",
                IfcRoundTripResultState.Unmapped,
                null,
                mappingRelationIdentity: "mapping:invented"));

            Throws<ArgumentOutOfRangeException>(() => new IfcRoundTripExchangeResult(
                "ifc-invalid-state",
                (IfcRoundTripResultState)999,
                null));

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                " ifc-padded ",
                IfcRoundTripResultState.Unmapped,
                null));

            Throws<ArgumentException>(() => new IfcRoundTripExchangeResult(
                "ifc-blank-class",
                IfcRoundTripResultState.Unmapped,
                null,
                classificationIdentity: string.Empty));

            var unsupported = new IfcRoundTripExchangeResult(
                "ifc-unsupported-01",
                IfcRoundTripResultState.Unsupported,
                null,
                stateDetail: "Unsupported external class",
                classificationIdentity: "external:IfcProxy");
            Require(unsupported.State == IfcRoundTripResultState.Unsupported, "Unsupported external object did not remain explicit.");
            Require(!unsupported.HasTrustedQs3dIdentity, "Unsupported external object fabricated QS3D identity.");

            var invalid = new IfcRoundTripExchangeResult(
                "ifc-ambiguous-01",
                IfcRoundTripResultState.InvalidOrAmbiguous,
                null,
                stateDetail: "Duplicate external identity");
            Require(invalid.State == IfcRoundTripResultState.InvalidOrAmbiguous, "Ambiguous external identity did not remain explicit.");
        }

        private static void CanonicalizesResultSetOrderingAndRejectsDuplicates()
        {
            var supported = new IfcRoundTripExchangeResult(
                "ifc-z-supported",
                IfcRoundTripResultState.Supported,
                CreateProjection("BEAM-Z", "ifc-z-supported", "IfcBeam"));
            var unmapped = new IfcRoundTripExchangeResult(
                "ifc-a-unmapped",
                IfcRoundTripResultState.Unmapped,
                null,
                stateDetail: "No trusted identity");

            var set = IfcRoundTripExchangeResultSet.Create(new[] { supported, unmapped });
            Require(set.Items.Count == 2, "IFC exchange result set lost items.");
            Require(set.Items[0].ExternalObjectId == "ifc-a-unmapped", "IFC exchange result set ordering is not deterministic.");
            Require(set.Items[1].ExternalObjectId == "ifc-z-supported", "IFC exchange result set ordering is not deterministic.");

            Throws<InvalidOperationException>(() => IfcRoundTripExchangeResultSet.Create(new[]
            {
                new IfcRoundTripExchangeResult(
                    "ifc-duplicate",
                    IfcRoundTripResultState.Unmapped,
                    null,
                    stateDetail: "No identity"),
                new IfcRoundTripExchangeResult(
                    "ifc-duplicate",
                    IfcRoundTripResultState.InvalidOrAmbiguous,
                    null,
                    stateDetail: "Duplicate identity")
            }));

            Throws<ArgumentException>(() => IfcRoundTripExchangeResultSet.Create(new IfcRoundTripExchangeResult[] { null! }));
        }

        private static IfcRoundTripProjection CreateProjection(string qs3dElementId, string ifcGlobalId, string semanticClassification)
        {
            return new IfcRoundTripProjection(
                qs3dElementId,
                ifcGlobalId,
                semanticClassification,
                new[] { new IfcRoundTripNumericProperty("Length", 1d, "m") },
                1d,
                "m",
                new[] { "source:smoke" });
        }

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
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

            throw new InvalidOperationException("Expected exception " + typeof(TException).Name + ".");
        }
    }
}
