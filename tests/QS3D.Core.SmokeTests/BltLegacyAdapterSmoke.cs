using System;
using QS3D.Core.Domain;
using QS3D.Core.Legacy;
using QS3D.Core.Model;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BltLegacyAdapterSmoke
    {
        public static void Run()
        {
            ExactProxySolidCanImport();
            EmbeddedLegacyQuantitiesArePreserved();
            AdapterMetricUnderflowDoesNotBecomeExactEvidence();
            AmbiguousCategoryFailsClosed();
            GenericProxyIsNotClaimedAsBlt();
            LegacyEvidenceSurvivesMeasuredQuantityPass();
            LegacyQuantityUnderflowFailsClosed();
            MalformedLegacyEvidenceDoesNotPartiallyApply();
            MalformedLegacyEvidenceDoesNotPartiallyApplyMeasuredPass();
        }

        private static void ExactProxySolidCanImport()
        {
            var snapshot = new EntitySnapshot("A10", "ProxyEntity", "BLT_COT")
            {
                VolumeDrawingUnitsCubed = 864000000d,
                SurfaceAreaDrawingUnitsSquared = 10000000d
            };
            snapshot.Metadata["LegacyProbe.ProxyOriginalClass"] = "BLT_COLUMN";
            snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();

            var candidate = BltLegacyEntityAdapter.Adapt(snapshot);
            Require(candidate.HasLegacySignal, "BLT proxy signal was not recognized.");
            Require(candidate.Category == ElementCategory.Column, "BLT_COLUMN did not map to Column.");
            Require(candidate.EvidenceMode == BltLegacyEvidenceMode.ExactGeometry, "Exact geometry evidence was lost.");
            Require(candidate.CanImport, "Exact-volume BLT Column proxy should be import-ready.");
            Require(snapshot.Metadata[BltLegacyMetadataKeys.SourceSystem] == "BLT3D", "Canonical BLT source marker was not written.");
        }

        private static void EmbeddedLegacyQuantitiesArePreserved()
        {
            var snapshot = new EntitySnapshot("B20", "ProxyEntity", "LEGACY");
            snapshot.Metadata["LegacyProbe.XData.000.Value"] =
                "BLT3D; Category=Beam; ConcreteM3=1.25; FormworkM2=10.5; Floor=T2; Family=D300x500";

            var candidate = BltLegacyEntityAdapter.Adapt(snapshot);
            Require(candidate.Category == ElementCategory.Beam, "Embedded Beam category was not recognized.");
            Require(candidate.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity, "Explicit unit-labelled legacy quantity must be exact legacy evidence.");
            Require(candidate.LegacyConcreteM3.HasValue && Math.Abs(candidate.LegacyConcreteM3.Value - 1.25d) < 1e-12, "ConcreteM3 was not parsed exactly.");
            Require(candidate.LegacyFormworkM2.HasValue && Math.Abs(candidate.LegacyFormworkM2.Value - 10.5d) < 1e-12, "FormworkM2 was not parsed exactly.");
            Require(candidate.FloorHint == "T2", "Floor hint was not preserved.");
            Require(candidate.FamilyHint == "D300x500", "Family hint was not preserved.");
            Require(candidate.CanImport, "Explicit BLT ConcreteM3 must allow material-volume Proxy capture even when host geometry is unavailable.");
        }

        private static void AdapterMetricUnderflowDoesNotBecomeExactEvidence()
        {
            var underflow = new EntitySnapshot("B21", "ProxyEntity", "LEGACY");
            underflow.Metadata["LegacyProbe.XData.000.Value"] =
                "BLT3D; Category=Beam; ConcreteM3=1e-5000";

            var rejected = BltLegacyEntityAdapter.Adapt(underflow);
            Require(rejected.Category == ElementCategory.Beam, "Underflow control lost its Beam category evidence.");
            Require(!rejected.LegacyConcreteM3.HasValue, "Adapter underflow must not become legacy concrete zero.");
            Require(rejected.EvidenceMode != BltLegacyEvidenceMode.ExactLegacyQuantity, "Adapter underflow must not upgrade evidence to ExactLegacyQuantity.");
            Require(!underflow.Metadata.ContainsKey(BltLegacyMetadataKeys.ConcreteM3), "Adapter underflow must not write canonical concrete zero metadata.");

            var exactZero = new EntitySnapshot("B22", "ProxyEntity", "LEGACY");
            exactZero.Metadata["LegacyProbe.XData.000.Value"] =
                "BLT3D; Category=Beam; ConcreteM3=0; FormworkM2=0,0e-5000";

            var accepted = BltLegacyEntityAdapter.Adapt(exactZero);
            Require(accepted.LegacyConcreteM3.HasValue && accepted.LegacyConcreteM3.Value.Equals(0d), "Exact-zero concrete must remain parseable.");
            Require(accepted.LegacyFormworkM2.HasValue && accepted.LegacyFormworkM2.Value.Equals(0d), "Comma-compatible exact-zero formwork must remain parseable.");
            Require(accepted.EvidenceMode == BltLegacyEvidenceMode.ExactLegacyQuantity, "Exact-zero explicit quantities must remain exact legacy evidence.");
            Require(exactZero.Metadata[BltLegacyMetadataKeys.ConcreteM3] == "0", "Exact-zero concrete canonicalization changed.");
            Require(exactZero.Metadata[BltLegacyMetadataKeys.FormworkM2] == "0", "Exact-zero formwork canonicalization changed.");
        }

        private static void AmbiguousCategoryFailsClosed()
        {
            var snapshot = new EntitySnapshot("C30", "ProxyEntity", "BLT")
            {
                VolumeDrawingUnitsCubed = 1d
            };
            snapshot.Metadata["LegacyProbe.ProxyOriginalClass"] = "BLT_COLUMN_BEAM";
            snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();

            var candidate = BltLegacyEntityAdapter.Adapt(snapshot);
            Require(!candidate.Category.HasValue, "Ambiguous BLT category must not be guessed.");
            Require(!candidate.CanImport, "Ambiguous BLT category must not import.");
            Require(candidate.Reason.IndexOf("more than one", StringComparison.OrdinalIgnoreCase) >= 0, "Ambiguous reason was not surfaced.");
        }

        private static void GenericProxyIsNotClaimedAsBlt()
        {
            var snapshot = new EntitySnapshot("D40", "ProxyEntity", "A-WALL")
            {
                VolumeDrawingUnitsCubed = 10d
            };
            snapshot.Metadata["LegacyProbe.ProxyOriginalClass"] = "THIRD_PARTY_OBJECT";
            snapshot.Metadata[BltLegacyMetadataKeys.ProbeMetricEvidence] = BltLegacyEvidenceMode.ExactGeometry.ToString();

            var candidate = BltLegacyEntityAdapter.Adapt(snapshot);
            Require(!candidate.HasLegacySignal, "Generic third-party proxy must not be claimed as BLT.");
            Require(!candidate.CanImport, "Generic third-party proxy must not import through BLT adapter.");
        }

        private static void LegacyEvidenceSurvivesMeasuredQuantityPass()
        {
            var exact = new ProjectElement("BLT-A", ElementCategory.Beam);
            exact.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            exact.Properties["CAD.BLT.LegacyConcreteM3"] = "1.25";
            exact.Properties["CAD.BLT.LegacyFormworkM2"] = "10.5";
            exact.Properties[MeasuredSolidQuantityPolicy.VolumeProperty] = "99";
            exact.SetQuantity("GrossVolumeM3", 99d);
            exact.SetQuantity("NetVolumeM3", 99d);
            exact.SetQuantity("FormworkM2", 999d);

            Require(MeasuredSolidQuantityPolicy.Apply(exact), "Legacy evidence policy was not handled through measured quantity pass.");
            Require(Math.Abs(exact.Quantities["GrossVolumeM3"] - 1.25d) < 1e-12, "Exact legacy concrete did not override regenerated/measured volume.");
            Require(Math.Abs(exact.Quantities["NetVolumeM3"] - 1.25d) < 1e-12, "Exact legacy net concrete did not persist.");
            Require(Math.Abs(exact.Quantities["FormworkM2"] - 10.5d) < 1e-12, "Exact legacy formwork did not persist.");

            var pendingFormwork = new ProjectElement("BLT-B", ElementCategory.Column);
            pendingFormwork.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            pendingFormwork.SetQuantity("FormworkM2", 123d);
            MeasuredSolidQuantityPolicy.Apply(pendingFormwork);
            Require(!pendingFormwork.Quantities.ContainsKey("FormworkM2"), "Unqualified legacy formwork must remain absent after regeneration policy.");
            Require(pendingFormwork.Properties["CAD.BLT.FormworkStatus"] == "PENDING_EXACT_EVIDENCE", "Pending formwork status was not preserved.");
        }

        private static void LegacyQuantityUnderflowFailsClosed()
        {
            var concreteUnderflow = new ProjectElement("BLT-U1", ElementCategory.Beam);
            concreteUnderflow.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            concreteUnderflow.Properties["CAD.BLT.LegacyConcreteM3"] = "1e-5000";
            ExpectInvalidOperation(() => BltLegacyQuantityEvidencePolicy.Apply(concreteUnderflow),
                "Legacy concrete underflow must fail closed instead of becoming zero.");

            var formworkUnderflow = new ProjectElement("BLT-U2", ElementCategory.Column);
            formworkUnderflow.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            formworkUnderflow.Properties["CAD.BLT.LegacyConcreteM3"] = "0";
            formworkUnderflow.Properties["CAD.BLT.LegacyFormworkM2"] = "1e-5000";
            ExpectInvalidOperation(() => BltLegacyQuantityEvidencePolicy.Apply(formworkUnderflow),
                "Legacy formwork underflow must fail closed instead of becoming zero.");

            var exactZero = new ProjectElement("BLT-U3", ElementCategory.Beam);
            exactZero.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            exactZero.Properties["CAD.BLT.LegacyConcreteM3"] = "0";
            exactZero.Properties["CAD.BLT.LegacyFormworkM2"] = "0e-5000";
            Require(BltLegacyQuantityEvidencePolicy.Apply(exactZero), "Exact zero legacy evidence should remain valid.");
            Require(exactZero.Quantities["GrossVolumeM3"].Equals(0d), "Exact zero concrete was not preserved.");
            Require(exactZero.Quantities["FormworkM2"].Equals(0d), "Exact zero formwork was not preserved.");
            Require(exactZero.Properties["CAD.BLT.FormworkStatus"] == "ExactLegacyQuantity", "Exact zero formwork status was not preserved.");
        }

        private static void MalformedLegacyEvidenceDoesNotPartiallyApply()
        {
            var element = new ProjectElement("BLT-C", ElementCategory.Beam);
            element.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            element.Properties["CAD.BLT.LegacyConcreteM3"] = "1.25";
            element.Properties["CAD.BLT.LegacyFormworkM2"] = "invalid";
            element.Properties["CAD.BLT.FormworkStatus"] = "ExistingStatus";
            element.SetQuantity("MeasuredSolidVolumeM3", 7d);
            element.SetQuantity("GrossVolumeM3", 8d);
            element.SetQuantity("NetVolumeM3", 9d);
            element.SetQuantity("DeductionM3", 0.5d);
            element.SetQuantity("FormworkM2", 11d);

            ExpectInvalidOperation(() => BltLegacyQuantityEvidencePolicy.Apply(element),
                "Malformed legacy formwork must fail closed.");
            Require(element.Quantities["MeasuredSolidVolumeM3"].Equals(7d), "Failed legacy apply changed measured volume.");
            Require(element.Quantities["GrossVolumeM3"].Equals(8d), "Failed legacy apply changed gross volume.");
            Require(element.Quantities["NetVolumeM3"].Equals(9d), "Failed legacy apply changed net volume.");
            Require(element.Quantities["DeductionM3"].Equals(0.5d), "Failed legacy apply changed deduction.");
            Require(element.Quantities["FormworkM2"].Equals(11d), "Failed legacy apply changed formwork.");
            Require(element.Properties["CAD.BLT.FormworkStatus"] == "ExistingStatus", "Failed legacy apply changed formwork status.");
        }

        private static void MalformedLegacyEvidenceDoesNotPartiallyApplyMeasuredPass()
        {
            var element = new ProjectElement("BLT-D", ElementCategory.Column);
            element.Properties["CAD.BLT.SourceSystem"] = "BLT3D";
            element.Properties["CAD.BLT.LegacyConcreteM3"] = "1.25";
            element.Properties["CAD.BLT.LegacyFormworkM2"] = "invalid";
            element.Properties["CAD.BLT.FormworkStatus"] = "ExistingStatus";
            element.Properties[MeasuredSolidQuantityPolicy.SurfaceAreaProperty] = "20";
            element.Properties[MeasuredSolidQuantityPolicy.VolumeProperty] = "99";
            element.SetQuantity("MeasuredSurfaceAreaM2", 4d);
            element.SetQuantity("MeasuredSolidVolumeM3", 5d);
            element.SetQuantity("GrossVolumeM3", 6d);
            element.SetQuantity("NetVolumeM3", 7d);
            element.SetQuantity("DeductionM3", 0.25d);
            element.SetQuantity("FormworkM2", 8d);

            ExpectInvalidOperation(() => MeasuredSolidQuantityPolicy.Apply(element),
                "Malformed legacy evidence must abort the measured pass before mutation.");
            Require(element.Quantities["MeasuredSurfaceAreaM2"].Equals(4d), "Failed measured pass changed measured surface area.");
            Require(element.Quantities["MeasuredSolidVolumeM3"].Equals(5d), "Failed measured pass changed measured volume.");
            Require(element.Quantities["GrossVolumeM3"].Equals(6d), "Failed measured pass changed gross volume.");
            Require(element.Quantities["NetVolumeM3"].Equals(7d), "Failed measured pass changed net volume.");
            Require(element.Quantities["DeductionM3"].Equals(0.25d), "Failed measured pass changed deduction.");
            Require(element.Quantities["FormworkM2"].Equals(8d), "Failed measured pass changed formwork.");
            Require(element.Properties["CAD.BLT.FormworkStatus"] == "ExistingStatus", "Failed measured pass changed formwork status.");
        }

        private static void ExpectInvalidOperation(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new InvalidOperationException("BLT legacy adapter smoke failed: " + message);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("BLT legacy adapter smoke failed: " + message);
        }
    }
}
