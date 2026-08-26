using System;
using System.Collections.Generic;
using QS3D.Core.Interoperability;

namespace QS3D.Core.SmokeTests
{
    internal static class InteroperabilityAdmissionEnumerationBoundSmoke
    {
        internal static void Run()
        {
            var provenance = new InteroperabilitySourceProvenance(
                InteroperabilitySourceSystem.Ifc,
                InteroperabilityTransport.Ifc,
                "enumeration-bound.ifc",
                "sha256:enumeration-bound",
                "IFC4",
                "batch-enumeration-bound");
            var factSet = InteroperabilityFactSet.Create(
                provenance,
                Array.Empty<InteroperabilityElementRecord>());
            var diagnostic = new InteroperabilityLossDiagnostic(
                "TEST_ADDITIONAL_DIAGNOSTIC_ENUMERATION_BOUND",
                InteroperabilityDiagnosticSeverity.Info,
                "Synthetic diagnostic used to verify bounded enumeration.");

            var yielded = 0;
            IEnumerable<InteroperabilityLossDiagnostic> UnboundedDiagnostics()
            {
                while (true)
                {
                    yielded++;
                    yield return diagnostic;
                }
            }

            try
            {
                InteroperabilityAdmission.Evaluate(factSet, UnboundedDiagnostics());
            }
            catch (InvalidOperationException)
            {
                var expected = InteroperabilityAdmission.MaxAdditionalDiagnostics + 1;
                if (yielded != expected)
                {
                    throw new InvalidOperationException(
                        "Interoperability admission enumerated past its deterministic diagnostic bound. " +
                        "Expected yields=" + expected + ", actual=" + yielded + ".");
                }

                return;
            }

            throw new InvalidOperationException(
                "Interoperability admission accepted an unbounded additional-diagnostics sequence.");
        }
    }
}
