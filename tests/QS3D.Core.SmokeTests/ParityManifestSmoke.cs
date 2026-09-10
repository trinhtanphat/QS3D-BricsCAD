using System;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityManifestSmoke
    {
        // Intentionally not named Run while #5981 owns SmokeTestRegistration.cs.
        // The project still compiles this contract test, and an external runner may invoke it
        // until the canonical smoke registration path is released and re-acquired.
        internal static void VerifyUnregisteredContract()
        {
            var record = new ParityFeatureRecord(
                new FeatureId("BIM.Draw.Rectangle"), " BIM ",
                " BLT3D / MÔ HÌNH BIM / Chữ nhật ", " BIM.Draw.Rectangle ",
                ParityApplicability.Applicable, ParityEvidenceStage.ReferenceCaptured);
            Equal("bim.draw.rectangle", record.FeatureId.ToString());
            Equal("BIM", record.Domain);
            Equal("bim.draw.rectangle", record.WorkflowKey);

            Throws<ArgumentException>(() => new ParityFeatureRecord(
                new FeatureId("host.unsupported"), "Host", "reference", "host.unsupported",
                ParityApplicability.NotApplicableByHostBoundary, ParityEvidenceStage.ReferenceCaptured));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}
