using System;
using System.Collections.Generic;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceNullFailFastSmoke
    {
        internal static void Run()
        {
            RejectsNullWithoutAdvancing();
            PreservesCandidateBoundPrecedence();
        }

        private static void RejectsNullWithoutAdvancing()
        {
            var advancedPastNull = false;
            var disposed = false;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(Enumerate());
                throw new InvalidOperationException("Expected null IFC quantity evidence to be rejected.");
            }
            catch (ArgumentException ex)
            {
                if (!string.Equals(ex.ParamName, "evidence", StringComparison.Ordinal))
                    throw new InvalidOperationException("Null IFC quantity evidence must identify the evidence parameter.", ex);
                if (ex.Message.IndexOf("cannot contain null entries", StringComparison.Ordinal) < 0)
                    throw new InvalidOperationException("Null IFC quantity evidence must preserve the validation message.", ex);
            }

            if (advancedPastNull)
                throw new InvalidOperationException("IFC quantity evidence enumeration advanced past a null candidate.");
            if (!disposed)
                throw new InvalidOperationException("IFC quantity evidence enumeration was not disposed after null rejection.");

            IEnumerable<IfcRoundTripQuantityEvidence> Enumerate()
            {
                try
                {
                    yield return null!;
                    advancedPastNull = true;
                    throw new InvalidOperationException("Enumeration advanced past null IFC quantity evidence.");
                }
                finally
                {
                    disposed = true;
                }
            }
        }

        private static void PreservesCandidateBoundPrecedence()
        {
            const int maxCandidates = 10000;
            var candidate = new IfcRoundTripQuantityEvidence("Q", 1d, "m", "SRC", "P");
            var advancedPastBoundary = false;
            var disposed = false;
            InvalidOperationException? failure = null;

            try
            {
                IfcRoundTripQuantityEvidenceSet.Create(Enumerate());
            }
            catch (InvalidOperationException ex)
            {
                failure = ex;
            }

            if (failure == null)
                throw new InvalidOperationException("Expected the IFC quantity evidence candidate bound to reject item 10001.");
            if (failure.Message.IndexOf("supports at most 10000 candidates", StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("The candidate bound must keep precedence over null validation at item 10001.", failure);
            if (advancedPastBoundary)
                throw new InvalidOperationException("IFC quantity evidence enumeration advanced beyond the first over-limit candidate.");
            if (!disposed)
                throw new InvalidOperationException("IFC quantity evidence enumeration was not disposed after the candidate bound failed.");

            IEnumerable<IfcRoundTripQuantityEvidence> Enumerate()
            {
                try
                {
                    for (var index = 0; index < maxCandidates; index++)
                        yield return candidate;
                    yield return null!;
                    advancedPastBoundary = true;
                    throw new InvalidOperationException("Enumeration advanced beyond the first over-limit candidate.");
                }
                finally
                {
                    disposed = true;
                }
            }
        }
    }
}
