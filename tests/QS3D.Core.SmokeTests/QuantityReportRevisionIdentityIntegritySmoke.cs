using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Revisions;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityReportRevisionIdentityIntegritySmoke
    {
        internal static void Run()
        {
            CaptureRejectsMalformedUtf16SnapshotIdentity();
            CaptureRejectsXmlInvalidSnapshotIdentity();
            ValidSupplementaryUnicodeRemainsAccepted();
        }

        private static void CaptureRejectsMalformedUtf16SnapshotIdentity()
        {
            ExpectInvalidXml("REV\uD800", "malformed UTF-16 snapshot identity");
        }

        private static void CaptureRejectsXmlInvalidSnapshotIdentity()
        {
            ExpectInvalidXml("REV\uFFFF", "XML-invalid snapshot identity");
        }

        private static void ValidSupplementaryUnicodeRemainsAccepted()
        {
            const string snapshotId = "REV\U0001F680";
            var snapshot = new QuantityReportRevisionService().Capture(new ProjectState("PROJECT", "Project"), snapshotId);
            if (!string.Equals(snapshot.SnapshotId, snapshotId, StringComparison.Ordinal))
                throw new Exception("Quantity report revision capture did not preserve valid supplementary-plane snapshot identity exactly.");
        }

        private static void ExpectInvalidXml(string snapshotId, string label)
        {
            try
            {
                new QuantityReportRevisionService().Capture(new ProjectState("PROJECT", "Project"), snapshotId);
                throw new Exception("Quantity report revision capture accepted " + label + ".");
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("invalid in XML", StringComparison.OrdinalIgnoreCase) < 0)
                    throw;
            }
        }
    }

    internal static class QuantityReportRevisionIdentityIntegrityRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityReportRevisionIdentityIntegritySmoke.Run();
    }
}
