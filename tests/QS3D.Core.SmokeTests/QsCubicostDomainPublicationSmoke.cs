using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostDomainPublicationSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            PublishesReviewedConcreteAndFormworkTogether();
            RejectsGenerationMismatch();
            RejectsMissingPeer();
        }

        private static void PublishesReviewedConcreteAndFormworkTogether()
        {
            var evidence = new QuantityEvidence("PUB-1", "A101.pdf#B1", "R7", "reviewed-component", 0.99d);
            var component = new RecognizedQsComponent("B1", "Beam", "STR.BEAM", "L01", 4d, 0.3d, 0.5d, ComponentSourceKind.DrawingRecognition, evidence);
            var review = new ComponentReviewDecision("B1", ComponentRecognitionStatus.Accepted, "qs", "verified", null, null, null);
            var bundle = new CubicostConcreteFormworkDomainOrchestrator().Quantify(new[] { component }, new[] { review }, true);
            var binding = new CubicostDownstreamBinding("STR.BEAM", "EST.CONCRETE.BEAM", "EST.FORMWORK.BEAM");

            var publication = new CubicostConcreteFormworkPublicationWorkflow().Publish(bundle, new[] { binding });

            Equal(2, publication.Lines.Count, "domain line count");
            Equal(2, publication.Inventory.Count, "inventory count");
            Equal(6, publication.CommercialHandoffs.Count, "three destinations per domain line");
            Equal(3, publication.CommercialHandoffs.Select(x => x.Destination).Distinct(StringComparer.Ordinal).Count(), "destination count");
            True(publication.Lines.All(x => ReferenceEquals(evidence, x.Evidence)), "evidence identity preserved");
            True(publication.CommercialHandoffs.All(x => x.Line.ReviewStatus == ComponentRecognitionStatus.Accepted), "only reviewed handoff");
        }

        private static void RejectsGenerationMismatch()
        {
            var concreteEvidence = new QuantityEvidence("PUB-2A", "model.ifc#C1", "R1", "ifc", 1d);
            var formworkEvidence = new QuantityEvidence("PUB-2B", "model.ifc#C1", "R1", "ifc", 1d);
            var combined = new CubicostQuantityLine("C1", "STR.COLUMN", "L02", 1d, 2d, ComponentRecognitionStatus.Accepted, concreteEvidence);
            var concrete = new CubicostDomainQuantityRow(CubicostQuantityDomain.Concrete, "C1", "STR.COLUMN", "L02", "m3", 1d, ComponentRecognitionStatus.Accepted, concreteEvidence);
            var formwork = new CubicostDomainQuantityRow(CubicostQuantityDomain.Formwork, "C1", "STR.COLUMN", "L02", "m2", 2d, ComponentRecognitionStatus.Accepted, formworkEvidence);
            var bundle = new CubicostDomainQuantityBundle(new[] { combined }, new[] { concrete }, new[] { formwork });
            var binding = new CubicostDownstreamBinding("STR.COLUMN", "EST.CONCRETE.COLUMN", "EST.FORMWORK.COLUMN");

            Throws<InvalidOperationException>(() => new CubicostConcreteFormworkPublicationWorkflow().Publish(bundle, new[] { binding }), "evidence generation mismatch");
        }

        private static void RejectsMissingPeer()
        {
            var evidence = new QuantityEvidence("PUB-3", "manual#S1", "R1", "manual", 1d);
            var combined = new CubicostQuantityLine("S1", "STR.SLAB", "L03", 1d, 5d, ComponentRecognitionStatus.Accepted, evidence);
            var concrete = new CubicostDomainQuantityRow(CubicostQuantityDomain.Concrete, "S1", "STR.SLAB", "L03", "m3", 1d, ComponentRecognitionStatus.Accepted, evidence);
            var bundle = new CubicostDomainQuantityBundle(new[] { combined }, new[] { concrete }, Array.Empty<CubicostDomainQuantityRow>());
            var binding = new CubicostDownstreamBinding("STR.SLAB", "EST.CONCRETE.SLAB", "EST.FORMWORK.SLAB");

            Throws<InvalidOperationException>(() => new CubicostConcreteFormworkPublicationWorkflow().Publish(bundle, new[] { binding }), "missing formwork peer");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }

        private static void Throws<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException(label + ": expected " + typeof(T).Name + ".");
        }
    }
}
