using System;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class ProxyCaptureEligibilitySmoke
    {
        public static void Run()
        {
            var unmeasured = new EntitySnapshot("A", "ProxyEntity", "blt beam");
            var result = new RecognitionEngine().Suggest(unmeasured);
            if (result.TopCandidate == null || result.TopCandidate.Category != ElementCategory.Beam || !result.RequiresReview || result.IsCaptureReady)
                throw new Exception("Metricless ProxyEntity must remain visible as a review-only Beam candidate.");
            if (new RecognitionBatch(new[] { result }).AutoAccepted.Count != 0)
                throw new Exception("Metricless ProxyEntity must not be auto-accepted.");
            Throws<InvalidOperationException>(() => EntitySnapshotCaptureEligibility.EnsureReady(unmeasured, ElementCategory.Beam));

            var paddedProxy = new EntitySnapshot("A2", "  pRoXyEnTiTy  ", "blt beam");
            if (!string.Equals(paddedProxy.EntityType, "pRoXyEnTiTy", StringComparison.Ordinal))
                throw new Exception("EntitySnapshot must canonicalize surrounding entity-type whitespace at construction.");
            var paddedResult = new RecognitionEngine().Suggest(paddedProxy);
            if (paddedResult.TopCandidate == null || paddedResult.TopCandidate.Category != ElementCategory.Beam || !paddedResult.RequiresReview || paddedResult.IsCaptureReady)
                throw new Exception("Padded/case-varied metricless ProxyEntity must remain review-only after canonicalization.");
            if (new RecognitionBatch(new[] { paddedResult }).AutoAccepted.Count != 0)
                throw new Exception("Padded/case-varied metricless ProxyEntity must not be auto-accepted.");
            Throws<InvalidOperationException>(() => EntitySnapshotCaptureEligibility.EnsureReady(paddedProxy, ElementCategory.Beam));

            var project = new ProjectState("p", "Proxy mapping");
            var mappingKey = TemplateProfileStore.LayerMappingPrefix + "BLT-COL";
            project.Metadata[mappingKey] = ElementCategory.Column.ToString();
            var mapped = new ProjectRecognitionService().SuggestBatch(project, new[] { new EntitySnapshot("B", "ProxyEntity", "BLT-COL") });
            if (mapped.AutoAccepted.Count != 0 || mapped.ReviewRequired.Count != 1)
                throw new Exception("Project-mapped metricless ProxyEntity must remain review-only.");
            project.Metadata[mappingKey] = "999";
            Throws<InvalidOperationException>(() => new ProjectRecognitionService().Suggest(project, new EntitySnapshot("B-INVALID", "Line", "BLT-COL")));
            project.Metadata[mappingKey] = ElementCategory.Column.ToString();

            var measured = new EntitySnapshot("C", "ProxyEntity", "blt beam") { LengthDrawingUnits = 2500d };
            var measuredResult = new RecognitionEngine().Suggest(measured);
            if (!measuredResult.IsCaptureReady || measuredResult.RequiresReview || new RecognitionBatch(new[] { measuredResult }).AutoAccepted.Count != 1)
                throw new Exception("Measured ProxyEntity should retain the normal confidence path.");

            foreach (var invalid in new[] { 0d, double.NaN, double.PositiveInfinity })
            {
                var bad = new EntitySnapshot("D", "ProxyEntity", "blt beam") { LengthDrawingUnits = invalid };
                if (EntitySnapshotCaptureEligibility.IsReady(bad, ElementCategory.Beam, out _))
                    throw new Exception("Non-positive/non-finite ProxyEntity metrics must fail closed.");
            }

            var surfaceOnly = new EntitySnapshot("E", "ProxyEntity", "blt slab") { SurfaceAreaDrawingUnitsSquared = 10d };
            if (EntitySnapshotCaptureEligibility.IsReady(surfaceOnly, ElementCategory.Slab, out _))
                throw new Exception("Total surface area alone is not a primary slab takeoff metric.");

            var blockDoor = new ProjectRecognitionService().Suggest(project, new EntitySnapshot("F", "BlockReference", "BLT-COL"));
            if (blockDoor.TopCandidate == null) throw new Exception("Non-proxy recognition behavior must remain available.");

            var invalidCategory = (ElementCategory)int.MaxValue;
            Throws<ArgumentOutOfRangeException>(() => new RecognitionRule("invalid-category", invalidCategory));
            var mutableCandidate = new RecognitionCandidate
            {
                RuleId = "manual-beam",
                Category = ElementCategory.Beam,
                Confidence = 1d
            };
            Throws<ArgumentOutOfRangeException>(() => mutableCandidate.Category = invalidCategory);
            var validManual = new RecognitionResult(
                new EntitySnapshot("F2", "Line", "blt beam"),
                new[] { mutableCandidate });
            if (new RecognitionBatch(new[] { validManual }).AutoAccepted.Count != 1)
                throw new Exception("Defined recognition categories must preserve normal non-proxy auto-accept behavior.");

            var generated = new EntitySnapshot("G", "Solid3d", "blt beam")
            {
                VolumeDrawingUnitsCubed = 1d,
                HasQs3dGeneratedOwnershipMarker = true
            };
            var generatedResult = new RecognitionEngine().Suggest(generated);
            if (generatedResult.TopCandidate == null || generatedResult.IsCaptureReady || !generatedResult.RequiresReview)
                throw new Exception("Native QS3D generated output must remain non-capturable even with valid metrics.");
            if (new RecognitionBatch(new[] { generatedResult }).AutoAccepted.Count != 0)
                throw new Exception("Native QS3D generated output must never be auto-accepted as a source.");
            Throws<InvalidOperationException>(() => EntitySnapshotCaptureEligibility.EnsureReady(generated, ElementCategory.Beam));
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
