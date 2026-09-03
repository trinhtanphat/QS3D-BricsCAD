using System;
using QS3D.Core.Interoperability;

namespace QS3D.Core.SmokeTests
{
    internal static class InteroperabilityFactSetProvenanceSmoke
    {
        internal static void Run()
        {
            RejectsCrossBatchRecordWithSameScope();
            RejectsCrossSchemaRecordWithSameScope();
            AcceptsEquivalentReconstructedProvenance();
        }

        private static void RejectsCrossBatchRecordWithSameScope()
        {
            var header = Provenance("IFC4", "batch-new");
            var stale = Provenance("IFC4", "batch-old");
            var record = EmptyRecord(stale, "WALL-1");

            Equal(header.ScopeKey, stale.ScopeKey);
            Throws<InvalidOperationException>(() =>
                InteroperabilityFactSet.Create(header, new[] { record }));
        }

        private static void RejectsCrossSchemaRecordWithSameScope()
        {
            var header = Provenance("IFC4X3", "batch-1");
            var stale = Provenance("IFC4", "batch-1");
            var record = EmptyRecord(stale, "WALL-2");

            Equal(header.ScopeKey, stale.ScopeKey);
            Throws<InvalidOperationException>(() =>
                InteroperabilityFactSet.Create(header, new[] { record }));
        }

        private static void AcceptsEquivalentReconstructedProvenance()
        {
            var header = Provenance("IFC4", "batch-equivalent");
            var reconstructed = Provenance("IFC4", "batch-equivalent");
            var record = EmptyRecord(reconstructed, "WALL-3");

            var factSet = InteroperabilityFactSet.Create(header, new[] { record });
            Equal(1, factSet.Records.Count);
            Equal("WALL-3", factSet.Records[0].Identity.SourceElementId);
        }

        private static InteroperabilitySourceProvenance Provenance(string schema, string batch)
        {
            return new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Ifc,
                InteroperabilityTransport.Ifc,
                "model.ifc",
                "sha256:model",
                schema,
                batch);
        }

        private static InteroperabilityElementRecord EmptyRecord(
            InteroperabilitySourceProvenance provenance,
            string sourceElementId)
        {
            var identity = InteroperabilityElementIdentity.ForIfc(
                provenance,
                sourceElementId,
                "QS3D-" + sourceElementId);
            return new InteroperabilityElementRecord(
                identity,
                Array.Empty<InteroperabilityPropertyFact>(),
                Array.Empty<InteroperabilityClassificationReference>(),
                Array.Empty<InteroperabilityQuantityFact>(),
                new[] { "provenance:" + sourceElementId });
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "Interoperability provenance smoke equality failed. Expected=" + expected + ", Actual=" + actual + ".");
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
                "Interoperability provenance smoke expected exception: " + typeof(TException).Name + ".");
        }
    }
}
