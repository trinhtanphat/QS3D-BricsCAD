using System;
using System.Linq;
using QS3D.Core.Export;
using QS3D.Core.Interoperability;

namespace QS3D.Core.SmokeTests
{
    internal static class InteroperabilityContractsSmoke
    {
        internal static void Run()
        {
            DrawingSourceIdentityRequiresFingerprint();
            BooleanPropertyValuesAreStrictAndCanonical();
            NumericPropertyValuesAreStrictAndCanonical();
            MeasuredPropertiesRequireNumericValueKind();
            IfcNormalizationPreservesIdentityAndQuantityOrigin();
            AmbiguousIfcEvidenceBlocksAdmission();
            UnresolvedQuantityUnitBlocksAdmission();
            DuplicateSourceIdentityBlocksAdmission();
            AdditionalDiagnosticsBoundIsEnforced();
            FactSetOrderingIsDeterministic();
        }

        private static void DrawingSourceIdentityRequiresFingerprint()
        {
            var unscoped = new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Dwg,
                InteroperabilityTransport.Dwg,
                "source.dwg",
                null,
                "AC1032",
                "batch-1");

            Throws<InvalidOperationException>(() =>
                InteroperabilityElementIdentity.ForDrawingSource(unscoped, "AB12"));

            var scoped = new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Dwg,
                InteroperabilityTransport.Dwg,
                "source.dwg",
                "sha256:001122",
                "AC1032",
                "batch-1");

            var identity = InteroperabilityElementIdentity.ForDrawingSource(scoped, "AB12");
            Equal("AB12", identity.SourceElementId);
            Equal("AB12", identity.DwgHandle);
            True(identity.Qs3dElementId == null);
            True(!identity.CanClaimTargetNativeOwnership);
        }

        private static void BooleanPropertyValuesAreStrictAndCanonical()
        {
            var truthy = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Flags",
                "Enabled",
                "TRUE",
                InteroperabilityPropertyValueKind.Boolean);
            var falsey = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Flags",
                "Visible",
                "False",
                InteroperabilityPropertyValueKind.Boolean);

            Equal("true", truthy.Value);
            Equal("false", falsey.Value);

            Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Flags",
                "InvalidWord",
                "banana",
                InteroperabilityPropertyValueKind.Boolean));
            Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Flags",
                "InvalidNumeric",
                "1",
                InteroperabilityPropertyValueKind.Boolean));

            var text = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Controls",
                "Text",
                "banana",
                InteroperabilityPropertyValueKind.Text);
            var number = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Controls",
                "Number",
                "1.25",
                InteroperabilityPropertyValueKind.Number);

            Equal("banana", text.Value);
            Equal("1.25", number.Value);
            Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Controls",
                "InvalidNumber",
                "NaN",
                InteroperabilityPropertyValueKind.Number));
        }

        private static void NumericPropertyValuesAreStrictAndCanonical()
        {
            var exactZero = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Numbers",
                "Zero",
                "0",
                InteroperabilityPropertyValueKind.Number);
            Equal("0", exactZero.Value);

            var positive = InteroperabilityPropertyFact.Number(
                "QS3D.Test",
                "Numbers",
                "Positive",
                1.25,
                "m");
            var negativeExponent = InteroperabilityPropertyFact.Number(
                "QS3D.Test",
                "Numbers",
                "NegativeExponent",
                -1.5e100,
                null,
                isMeasured: false);

            var positiveRoundTrip = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Numbers",
                "PositiveRoundTrip",
                positive.Value,
                InteroperabilityPropertyValueKind.Number,
                "m",
                isMeasured: true);
            var exponentRoundTrip = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Numbers",
                "ExponentRoundTrip",
                negativeExponent.Value,
                InteroperabilityPropertyValueKind.Number);

            Equal(positive.Value, positiveRoundTrip.Value);
            Equal(negativeExponent.Value, exponentRoundTrip.Value);

            foreach (var alias in new[] { "1.0", "1e0", "+1", "01" })
            {
                Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                    "QS3D.Test",
                    "Numbers",
                    "Alias",
                    alias,
                    InteroperabilityPropertyValueKind.Number));
            }

            foreach (var underflow in new[] { "1e-5000", "-1e-5000" })
            {
                Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                    "QS3D.Test",
                    "Numbers",
                    "Underflow",
                    underflow,
                    InteroperabilityPropertyValueKind.Number));
            }

            var textAlias = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "NumberControls",
                "TextAlias",
                "1e0",
                InteroperabilityPropertyValueKind.Text);
            var booleanControl = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "NumberControls",
                "Boolean",
                "TRUE",
                InteroperabilityPropertyValueKind.Boolean);
            Equal("1e0", textAlias.Value);
            Equal("true", booleanControl.Value);
        }

        private static void MeasuredPropertiesRequireNumericValueKind()
        {
            Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Measured",
                "TextKind",
                "12.5",
                InteroperabilityPropertyValueKind.Text,
                unit: "m",
                isMeasured: true));
            Throws<ArgumentException>(() => new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Measured",
                "BooleanKind",
                "true",
                InteroperabilityPropertyValueKind.Boolean,
                unit: "m",
                isMeasured: true));

            var measuredNumber = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "Measured",
                "NumericKind",
                "12.5",
                InteroperabilityPropertyValueKind.Number,
                unit: "m",
                isMeasured: true);
            True(measuredNumber.IsMeasured);
            Equal(InteroperabilityPropertyValueKind.Number, measuredNumber.ValueKind);
            Equal("m", measuredNumber.Unit);

            var nonMeasuredText = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "MeasuredControls",
                "Text",
                "12.5",
                InteroperabilityPropertyValueKind.Text);
            var nonMeasuredBoolean = new InteroperabilityPropertyFact(
                "QS3D.Test",
                "MeasuredControls",
                "Boolean",
                "TRUE",
                InteroperabilityPropertyValueKind.Boolean);
            True(!nonMeasuredText.IsMeasured);
            True(!nonMeasuredBoolean.IsMeasured);
            Equal("12.5", nonMeasuredText.Value);
            Equal("true", nonMeasuredBoolean.Value);
        }

        private static void IfcNormalizationPreservesIdentityAndQuantityOrigin()
        {
            var evidence = new[]
            {
                new IfcRoundTripQuantityEvidence(
                    "GrossVolume",
                    3.2,
                    "m3",
                    "Qto_WallBaseQuantities",
                    "IfcElementQuantity:QTO-1")
            };
            var projection = new IfcRoundTripProjection(
                "WALL-1",
                "3fIFC-global-id",
                "ArchitecturalWall",
                new[]
                {
                    new IfcRoundTripNumericProperty("Length", 5.0, "m"),
                    new IfcRoundTripNumericProperty("Height", 3.0, "m")
                },
                3.0,
                "m3",
                new[] { "qs3d-element:WALL-1", "ifc-object:3fIFC-global-id" },
                evidence);
            var resultSet = IfcRoundTripExchangeResultSet.Create(new[]
            {
                new IfcRoundTripExchangeResult(
                    "3fIFC-global-id",
                    IfcRoundTripResultState.Supported,
                    projection,
                    classificationIdentity: "Uniclass:EF_25")
            });
            var provenance = new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Ifc,
                InteroperabilityTransport.Ifc,
                "model.ifc",
                "sha256:ifc-model",
                "IFC4",
                "batch-ifc-1");

            var normalized = IfcRoundTripInteroperabilityNormalizer.Normalize(resultSet, provenance);
            True(normalized.IsAdmissible);
            Equal(1, normalized.FactSet.Records.Count);

            var record = normalized.FactSet.Records[0];
            Equal("3fIFC-global-id", record.Identity.SourceElementId);
            Equal("3fIFC-global-id", record.Identity.IfcGlobalId);
            Equal("WALL-1", record.Identity.Qs3dElementId);
            True(!record.Identity.CanClaimTargetNativeOwnership);
            True(record.Classifications.Any(x =>
                x.System == "QS3D.Semantic" &&
                x.Code == "ArchitecturalWall"));
            True(record.Classifications.Any(x =>
                x.System == "IFC.ExternalClassification" &&
                x.Code == "Uniclass:EF_25"));
            True(record.Quantities.Any(x =>
                x.Name == "GrossVolume" &&
                x.Origin == InteroperabilityQuantityOrigin.DeclaredSource &&
                x.SourceIdentity == "Qto_WallBaseQuantities" &&
                x.ProvenanceIdentity == "IfcElementQuantity:QTO-1"));
            True(record.Quantities.Any(x =>
                x.Name == "PrimaryQuantity" &&
                x.Origin == InteroperabilityQuantityOrigin.DerivedQs3d &&
                x.CalculationRuleId == "QS3D.IfcRoundTrip.PrimaryQuantity"));
            True(record.Properties.Any(x =>
                x.SetName == "Dimensions" &&
                x.Name == "Length" &&
                x.Unit == "m" &&
                x.IsMeasured));
            Equal(2, record.ProvenanceTokens.Count);
        }

        private static void AmbiguousIfcEvidenceBlocksAdmission()
        {
            var evidence = new[]
            {
                new IfcRoundTripQuantityEvidence(
                    "NetVolume",
                    2.9,
                    "m3",
                    "Qto_WallBaseQuantities",
                    "IfcElementQuantity:QTO-A"),
                new IfcRoundTripQuantityEvidence(
                    "NetVolume",
                    3.1,
                    "m3",
                    "Qto_WallBaseQuantities",
                    "IfcElementQuantity:QTO-B")
            };
            var projection = new IfcRoundTripProjection(
                "WALL-2",
                "3fIFC-ambiguous",
                "ArchitecturalWall",
                Array.Empty<IfcRoundTripNumericProperty>(),
                3.0,
                "m3",
                new[] { "qs3d-element:WALL-2" },
                evidence);
            var resultSet = IfcRoundTripExchangeResultSet.Create(new[]
            {
                new IfcRoundTripExchangeResult(
                    "3fIFC-ambiguous",
                    IfcRoundTripResultState.Supported,
                    projection)
            });
            var provenance = IfcProvenance("batch-ifc-ambiguous");

            var normalized = IfcRoundTripInteroperabilityNormalizer.Normalize(resultSet, provenance);
            True(!normalized.IsAdmissible);
            True(normalized.Diagnostics.Any(x =>
                x.Code == "IFC_QUANTITY_EVIDENCE_AMBIGUOUS" &&
                x.Severity == InteroperabilityDiagnosticSeverity.Blocking));
            Throws<InvalidOperationException>(() => normalized.ThrowIfBlocked());
        }

        private static void UnresolvedQuantityUnitBlocksAdmission()
        {
            var provenance = IfcProvenance("batch-unit");
            var identity = InteroperabilityElementIdentity.ForIfc(
                provenance,
                "3fIFC-unit",
                "WALL-UNIT");
            var record = new InteroperabilityElementRecord(
                identity,
                Array.Empty<InteroperabilityPropertyFact>(),
                Array.Empty<InteroperabilityClassificationReference>(),
                new[]
                {
                    new InteroperabilityQuantityFact(
                        "PrimaryQuantity",
                        4.2,
                        null,
                        InteroperabilityQuantityOrigin.DerivedQs3d,
                        calculationRuleId: "rule:unit-test")
                },
                new[] { "unit-test" });
            var factSet = InteroperabilityFactSet.Create(provenance, new[] { record });

            var admission = InteroperabilityAdmission.Evaluate(factSet);
            True(!admission.IsAdmissible);
            True(admission.Diagnostics.Any(x =>
                x.Code == "QUANTITY_UNIT_UNRESOLVED" &&
                x.SourceElementId == "3fIFC-unit"));
        }

        private static void DuplicateSourceIdentityBlocksAdmission()
        {
            var provenance = IfcProvenance("batch-duplicate");
            var identity = InteroperabilityElementIdentity.ForIfc(
                provenance,
                "3fIFC-duplicate",
                "WALL-DUP");
            var first = EmptyRecord(identity, "first");
            var second = EmptyRecord(identity, "second");
            var factSet = InteroperabilityFactSet.Create(provenance, new[] { first, second });

            var admission = InteroperabilityAdmission.Evaluate(factSet);
            True(!admission.IsAdmissible);
            True(admission.Diagnostics.Any(x =>
                x.Code == "DUPLICATE_SOURCE_IDENTITY" &&
                x.Severity == InteroperabilityDiagnosticSeverity.Blocking));
        }

        private static void AdditionalDiagnosticsBoundIsEnforced()
        {
            var provenance = IfcProvenance("batch-additional-diagnostic-bound");
            var factSet = InteroperabilityFactSet.Create(
                provenance,
                Array.Empty<InteroperabilityElementRecord>());
            var diagnostic = new InteroperabilityLossDiagnostic(
                "TEST_ADDITIONAL_DIAGNOSTIC",
                InteroperabilityDiagnosticSeverity.Info,
                "Synthetic interoperability diagnostic.");

            var accepted = InteroperabilityAdmission.Evaluate(
                factSet,
                Enumerable.Repeat(
                    diagnostic,
                    InteroperabilityAdmission.MaxAdditionalDiagnostics));
            Equal(InteroperabilityAdmission.MaxAdditionalDiagnostics, accepted.Diagnostics.Count);

            Throws<InvalidOperationException>(() =>
                InteroperabilityAdmission.Evaluate(
                    factSet,
                    Enumerable.Repeat(
                        diagnostic,
                        InteroperabilityAdmission.MaxAdditionalDiagnostics + 1)));
        }

        private static void FactSetOrderingIsDeterministic()
        {
            var provenance = IfcProvenance("batch-order");
            var b = EmptyRecord(
                InteroperabilityElementIdentity.ForIfc(provenance, "B-source", "B-QS3D"),
                "b");
            var a = EmptyRecord(
                InteroperabilityElementIdentity.ForIfc(provenance, "A-source", "A-QS3D"),
                "a");

            var factSet = InteroperabilityFactSet.Create(provenance, new[] { b, a });
            Equal("A-source", factSet.Records[0].Identity.SourceElementId);
            Equal("B-source", factSet.Records[1].Identity.SourceElementId);
        }

        private static InteroperabilityElementRecord EmptyRecord(
            InteroperabilityElementIdentity identity,
            string provenanceToken)
        {
            return new InteroperabilityElementRecord(
                identity,
                Array.Empty<InteroperabilityPropertyFact>(),
                Array.Empty<InteroperabilityClassificationReference>(),
                Array.Empty<InteroperabilityQuantityFact>(),
                new[] { provenanceToken });
        }

        private static InteroperabilitySourceProvenance IfcProvenance(string batch)
        {
            return new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Ifc,
                InteroperabilityTransport.Ifc,
                "model.ifc",
                "sha256:model",
                "IFC4",
                batch);
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("Interoperability smoke assertion failed.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "Interoperability smoke equality failed. Expected=" + expected + ", Actual=" + actual + ".");
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

            throw new InvalidOperationException(
                "Interoperability smoke expected exception: " + typeof(TException).Name + ".");
        }
    }
}
