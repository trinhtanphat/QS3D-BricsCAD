using System;
using QS3D.Core.Legacy;
using QS3D.Core.Model;

namespace QS3D.Core.SmokeTests
{
    internal static class BltLegacyMetricConflictSmoke
    {
        internal static void Run()
        {
            ConflictingConcreteAliasesFailClosed();
            ConflictingFormworkAliasesFailClosed();
            ConflictingEmbeddedAliasesFailClosed();
            ValidAndMalformedAliasesFailClosedRegardlessOfOrder();
            EqualAliasesRemainDeterministic();
            MetricKindsRemainIndependent();
            CanonicalReadaptationRemainsCompatible();
        }

        private static void ConflictingConcreteAliasesFailClosed()
        {
            var first = CreateSnapshot("CONCRETE-CONFLICT-A");
            first.Metadata["ConcreteM3"] = "1";
            first.Metadata["NetConcreteM3"] = "2";
            AssertMetricRejected(BltLegacyEntityAdapter.Adapt(first), concrete: true,
                "conflicting direct concrete aliases were accepted");

            var reversed = CreateSnapshot("CONCRETE-CONFLICT-B");
            reversed.Metadata["NetConcreteM3"] = "2";
            reversed.Metadata["ConcreteM3"] = "1";
            AssertMetricRejected(BltLegacyEntityAdapter.Adapt(reversed), concrete: true,
                "reversed conflicting direct concrete aliases were accepted");
        }

        private static void ConflictingFormworkAliasesFailClosed()
        {
            var snapshot = CreateSnapshot("FORMWORK-CONFLICT");
            snapshot.Metadata["FormworkM2"] = "3";
            snapshot.Metadata["VKM2"] = "4";
            AssertMetricRejected(BltLegacyEntityAdapter.Adapt(snapshot), concrete: false,
                "conflicting direct formwork aliases were accepted");
        }

        private static void ConflictingEmbeddedAliasesFailClosed()
        {
            var snapshot = CreateSnapshot("EMBEDDED-CONFLICT");
            snapshot.Metadata["LegacyProbe.XData.000.Value"] =
                "BLT3D; ConcreteM3=1.5; NetConcreteM3=2.5";
            AssertMetricRejected(BltLegacyEntityAdapter.Adapt(snapshot), concrete: true,
                "conflicting embedded concrete aliases were accepted");
        }

        private static void ValidAndMalformedAliasesFailClosedRegardlessOfOrder()
        {
            var validFirst = CreateSnapshot("VALID-FIRST");
            validFirst.Metadata["ConcreteM3"] = "1.25";
            validFirst.Metadata["NetConcreteM3"] = "1e-5000";
            AssertMetricRejected(BltLegacyEntityAdapter.Adapt(validFirst), concrete: true,
                "later underflow concrete alias was silently ignored");

            var malformedFirst = CreateSnapshot("MALFORMED-FIRST");
            malformedFirst.Metadata["ConcreteM3"] = "NaN";
            malformedFirst.Metadata["NetConcreteM3"] = "1.25";
            AssertMetricRejected(BltLegacyEntityAdapter.Adapt(malformedFirst), concrete: true,
                "earlier malformed concrete alias was silently ignored");
        }

        private static void EqualAliasesRemainDeterministic()
        {
            var concrete = CreateSnapshot("CONCRETE-EQUAL");
            concrete.Metadata["ConcreteM3"] = "1.25";
            concrete.Metadata["NetConcreteM3"] = "1.25";
            var concreteCandidate = BltLegacyEntityAdapter.Adapt(concrete);
            True(concreteCandidate.LegacyConcreteM3.HasValue,
                "equal concrete aliases lost exact quantity evidence");
            Near(1.25d, concreteCandidate.LegacyConcreteM3.GetValueOrDefault(),
                "equal concrete aliases produced the wrong quantity");
            True(concreteCandidate.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity,
                "equal concrete aliases lost ExactLegacyQuantity mode");

            var formwork = CreateSnapshot("FORMWORK-EQUAL");
            formwork.Metadata["FormworkM2"] = "2.5";
            formwork.Metadata[BltLegacyMetadataKeys.FormworkM2] = "2.5";
            var formworkCandidate = BltLegacyEntityAdapter.Adapt(formwork);
            True(formworkCandidate.LegacyFormworkM2.HasValue,
                "equal formwork aliases lost exact quantity evidence");
            Near(2.5d, formworkCandidate.LegacyFormworkM2.GetValueOrDefault(),
                "equal formwork aliases produced the wrong quantity");
        }

        private static void MetricKindsRemainIndependent()
        {
            var concreteConflict = CreateSnapshot("CONCRETE-CONFLICT-FORMWORK-VALID");
            concreteConflict.Metadata["ConcreteM3"] = "1";
            concreteConflict.Metadata["NetConcreteM3"] = "2";
            concreteConflict.Metadata["FormworkM2"] = "3";
            var formworkSurvives = BltLegacyEntityAdapter.Adapt(concreteConflict);
            True(!formworkSurvives.LegacyConcreteM3.HasValue,
                "concrete conflict unexpectedly produced an exact concrete quantity");
            Near(3d, formworkSurvives.LegacyFormworkM2.GetValueOrDefault(),
                "concrete conflict poisoned independent valid formwork evidence");
            True(formworkSurvives.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity,
                "independent valid formwork evidence did not retain ExactLegacyQuantity mode");

            var formworkConflict = CreateSnapshot("FORMWORK-CONFLICT-CONCRETE-VALID");
            formworkConflict.Metadata["ConcreteM3"] = "4";
            formworkConflict.Metadata["FormworkM2"] = "5";
            formworkConflict.Metadata["VKM2"] = "6";
            var concreteSurvives = BltLegacyEntityAdapter.Adapt(formworkConflict);
            Near(4d, concreteSurvives.LegacyConcreteM3.GetValueOrDefault(),
                "formwork conflict poisoned independent valid concrete evidence");
            True(!concreteSurvives.LegacyFormworkM2.HasValue,
                "formwork conflict unexpectedly produced an exact formwork quantity");
            True(concreteSurvives.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity,
                "independent valid concrete evidence did not retain ExactLegacyQuantity mode");
        }

        private static void CanonicalReadaptationRemainsCompatible()
        {
            var snapshot = CreateSnapshot("CANONICAL-READAPT");
            snapshot.Metadata["ConcreteM3"] = "1.75";
            snapshot.Metadata["VKM2"] = "2.25";

            var first = BltLegacyEntityAdapter.Adapt(snapshot);
            True(first.LegacyConcreteM3.HasValue && first.LegacyFormworkM2.HasValue,
                "single legacy aliases lost exact quantity evidence before canonicalization");
            Near(1.75d, first.LegacyConcreteM3.GetValueOrDefault(),
                "single concrete alias produced the wrong quantity");
            Near(2.25d, first.LegacyFormworkM2.GetValueOrDefault(),
                "single formwork alias produced the wrong quantity");

            True(snapshot.Metadata.ContainsKey(BltLegacyMetadataKeys.ConcreteM3) &&
                 snapshot.Metadata.ContainsKey(BltLegacyMetadataKeys.FormworkM2),
                "first adaptation did not publish canonical BLT metric keys");

            var second = BltLegacyEntityAdapter.Adapt(snapshot);
            True(second.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity,
                "canonical BLT metric re-adaptation lost ExactLegacyQuantity mode");
            Near(1.75d, second.LegacyConcreteM3.GetValueOrDefault(),
                "canonical concrete re-adaptation changed the quantity");
            Near(2.25d, second.LegacyFormworkM2.GetValueOrDefault(),
                "canonical formwork re-adaptation changed the quantity");
        }

        private static void AssertMetricRejected(
            BltLegacyElementCandidate candidate,
            bool concrete,
            string message)
        {
            if (concrete)
                True(!candidate.LegacyConcreteM3.HasValue, message);
            else
                True(!candidate.LegacyFormworkM2.HasValue, message);

            True(candidate.EvidenceMode == BltLegacyEvidenceMode.ExactGeometry,
                message + "; conflicting evidence must not promote ExactLegacyQuantity");
        }

        private static EntitySnapshot CreateSnapshot(string id)
        {
            var snapshot = new EntitySnapshot(id, "ProxyEntity", "LEGACY");
            snapshot.Metadata["LegacyProbe.ProxyOriginalClass"] = "BLT_OBJECT";
            snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] =
                BltLegacyEvidenceMode.ExactGeometry.ToString();
            return snapshot;
        }

        private static void True(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("BLT metric conflict regression: " + message + ".");
        }

        private static void Near(double expected, double actual, string message)
        {
            if (Math.Abs(expected - actual) > 1e-12)
                throw new InvalidOperationException("BLT metric conflict regression: " + message + ".");
        }
    }
}