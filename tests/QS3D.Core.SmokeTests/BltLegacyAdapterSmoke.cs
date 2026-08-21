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
            AmbiguousCategoryFailsClosed();
            GenericProxyIsNotClaimedAsBlt();
            LegacyEvidenceSurvivesMeasuredQuantityPass();
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

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("BLT legacy adapter smoke failed: " + message);
        }
    }
}
