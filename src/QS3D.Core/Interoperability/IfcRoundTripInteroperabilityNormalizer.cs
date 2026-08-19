using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Export;

namespace QS3D.Core.Interoperability
{
    /// <summary>
    /// Converts the existing IFC round-trip seam into the host-neutral interoperability fact model.
    /// This adapter preserves source identity/evidence and never creates target-DWG native ownership.
    /// </summary>
    public static class IfcRoundTripInteroperabilityNormalizer
    {
        public static InteroperabilityAdmissionResult Normalize(
            IfcRoundTripExchangeResultSet results,
            InteroperabilitySourceProvenance provenance)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (provenance == null) throw new ArgumentNullException(nameof(provenance));
            if (provenance.Transport != InteroperabilityTransport.Ifc &&
                provenance.Transport != InteroperabilityTransport.NeutralSnapshot)
                throw new InvalidOperationException(
                    "IFC round-trip normalization requires IFC or neutral-snapshot transport.");

            var records = new List<InteroperabilityElementRecord>();
            var diagnostics = new List<InteroperabilityLossDiagnostic>();

            foreach (var result in results.Items)
            {
                switch (result.State)
                {
                    case IfcRoundTripResultState.Supported:
                    case IfcRoundTripResultState.SupportedLossy:
                        NormalizeSupported(result, provenance, records, diagnostics);
                        break;

                    case IfcRoundTripResultState.Unmapped:
                        diagnostics.Add(StateDiagnostic(
                            result,
                            "IFC_UNMAPPED",
                            InteroperabilityDiagnosticSeverity.Warning,
                            "IFC source object has no trusted QS3D semantic mapping."));
                        break;

                    case IfcRoundTripResultState.Unsupported:
                        diagnostics.Add(StateDiagnostic(
                            result,
                            "IFC_UNSUPPORTED",
                            InteroperabilityDiagnosticSeverity.Warning,
                            "IFC source object uses a representation or semantic mapping that QS3D does not currently support."));
                        break;

                    case IfcRoundTripResultState.InvalidOrAmbiguous:
                        diagnostics.Add(StateDiagnostic(
                            result,
                            "IFC_INVALID_OR_AMBIGUOUS",
                            InteroperabilityDiagnosticSeverity.Blocking,
                            "IFC source identity or mapping is invalid or ambiguous and cannot be admitted as trusted quantity data."));
                        break;

                    default:
                        throw new InvalidOperationException("Unexpected IFC round-trip result state: " + result.State + ".");
                }
            }

            var factSet = InteroperabilityFactSet.Create(provenance, records);
            return InteroperabilityAdmission.Evaluate(factSet, diagnostics);
        }

        private static void NormalizeSupported(
            IfcRoundTripExchangeResult result,
            InteroperabilitySourceProvenance provenance,
            ICollection<InteroperabilityElementRecord> records,
            ICollection<InteroperabilityLossDiagnostic> globalDiagnostics)
        {
            var projection = result.Projection
                ?? throw new InvalidOperationException("Supported IFC result is missing its required projection.");

            var identity = InteroperabilityElementIdentity.ForIfc(
                provenance,
                projection.IfcGlobalId,
                projection.Qs3dElementId);

            var properties = new List<InteroperabilityPropertyFact>();
            foreach (var dimension in projection.Dimensions)
            {
                properties.Add(InteroperabilityPropertyFact.Number(
                    "QS3D.IfcRoundTrip",
                    "Dimensions",
                    dimension.Name,
                    dimension.Value,
                    dimension.Unit));
            }

            if (result.MappingRelationIdentity != null)
            {
                properties.Add(new InteroperabilityPropertyFact(
                    "QS3D.IfcRoundTrip",
                    "Relations",
                    "MappingRelationIdentity",
                    result.MappingRelationIdentity,
                    InteroperabilityPropertyValueKind.Text));
            }

            if (result.CostItemRelationIdentity != null)
            {
                properties.Add(new InteroperabilityPropertyFact(
                    "QS3D.IfcRoundTrip",
                    "Relations",
                    "CostItemRelationIdentity",
                    result.CostItemRelationIdentity,
                    InteroperabilityPropertyValueKind.Text));
            }

            var classifications = new List<InteroperabilityClassificationReference>
            {
                new InteroperabilityClassificationReference(
                    "QS3D.Semantic",
                    projection.SemanticClassification)
            };

            if (result.ClassificationIdentity != null)
            {
                classifications.Add(new InteroperabilityClassificationReference(
                    "IFC.ExternalClassification",
                    result.ClassificationIdentity));
            }

            var quantities = new List<InteroperabilityQuantityFact>
            {
                new InteroperabilityQuantityFact(
                    "PrimaryQuantity",
                    projection.PrimaryQuantity,
                    projection.PrimaryQuantityUnit,
                    InteroperabilityQuantityOrigin.DerivedQs3d,
                    calculationRuleId: "QS3D.IfcRoundTrip.PrimaryQuantity")
            };

            var recordDiagnostics = new List<InteroperabilityLossDiagnostic>();
            foreach (var group in projection.QuantityEvidence.Groups)
            {
                foreach (var candidate in group.Candidates)
                {
                    quantities.Add(new InteroperabilityQuantityFact(
                        candidate.QuantityKey,
                        candidate.Value,
                        candidate.Unit,
                        InteroperabilityQuantityOrigin.DeclaredSource,
                        sourceIdentity: candidate.ExternalSourceIdentity,
                        provenanceIdentity: candidate.ProvenanceIdentity));
                }

                if (group.IsAmbiguous)
                {
                    recordDiagnostics.Add(new InteroperabilityLossDiagnostic(
                        "IFC_QUANTITY_EVIDENCE_AMBIGUOUS",
                        InteroperabilityDiagnosticSeverity.Blocking,
                        "IFC quantity " + group.QuantityKey +
                        " contains multiple distinct candidates for source identity " +
                        group.ExternalSourceIdentity + ".",
                        projection.IfcGlobalId,
                        group.QuantityKey));
                }
            }

            if (result.State == IfcRoundTripResultState.SupportedLossy)
            {
                recordDiagnostics.Add(new InteroperabilityLossDiagnostic(
                    "IFC_MAPPING_LOSS",
                    InteroperabilityDiagnosticSeverity.Warning,
                    result.StateDetail ?? "IFC mapping is supported with explicit semantic loss.",
                    projection.IfcGlobalId));
            }

            if (projection.QuantityEvidence.HasAmbiguity &&
                !recordDiagnostics.Any(x => x.Code == "IFC_QUANTITY_EVIDENCE_AMBIGUOUS"))
            {
                globalDiagnostics.Add(new InteroperabilityLossDiagnostic(
                    "IFC_QUANTITY_EVIDENCE_AMBIGUOUS",
                    InteroperabilityDiagnosticSeverity.Blocking,
                    "IFC quantity evidence is ambiguous.",
                    projection.IfcGlobalId));
            }

            records.Add(new InteroperabilityElementRecord(
                identity,
                properties,
                classifications,
                quantities,
                projection.Provenance,
                recordDiagnostics));
        }

        private static InteroperabilityLossDiagnostic StateDiagnostic(
            IfcRoundTripExchangeResult result,
            string code,
            InteroperabilityDiagnosticSeverity severity,
            string fallbackMessage)
        {
            return new InteroperabilityLossDiagnostic(
                code,
                severity,
                result.StateDetail ?? fallbackMessage,
                result.ExternalObjectId);
        }
    }
}
