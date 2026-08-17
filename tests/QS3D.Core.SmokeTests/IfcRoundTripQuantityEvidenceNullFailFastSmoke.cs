using System;
using System.Collections.Generic;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class IfcRoundTripQuantityEvidenceNullFailFastSmoke
    {
        internal static void Run()
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
    }
}
