using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseSignatureNodeShapeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AcceptsOrdinaryTextSignature();
            RejectsCommentInsideSignature();
            RejectsCDataInsideSignature();
            RejectsProcessingInstructionInsideSignature();
        }

        private static void AcceptsOrdinaryTextSignature() =>
            WithLicense(Canonical("\n  AA==  \n"), path =>
            {
                var license = new LicenseVerifier().Load(path);
                if (license.Signature.Length != 1 || license.Signature[0] != 0)
                    throw new InvalidOperationException("Ordinary text-only Base64 signature parsing must remain unchanged.");
            });

        private static void RejectsCommentInsideSignature() =>
            Reject(Canonical("AA<!--ignored-->=="), "comment-split signature");

        private static void RejectsCDataInsideSignature() =>
            Reject(Canonical("<![CDATA[AA==]]>"), "CDATA signature");

        private static void RejectsProcessingInstructionInsideSignature() =>
            Reject(Canonical("AA<?ignored value?>=="), "processing-instruction signature");

        private static void Reject(string xml, string label) =>
            WithLicense(xml, path =>
            {
                try
                {
                    new LicenseVerifier().Load(path);
                }
                catch (InvalidDataException)
                {
                    return;
                }
                throw new InvalidOperationException("License verifier accepted unsupported " + label + ".");
            });

        private static string Canonical(string signatureContent) =>
            "<qs3dLicense schema='1' id='LIC-SIG-NODE' customer='customer' product='product' nonce='nonce'>" +
            "<valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>" +
            "<signature algorithm='RSA-SHA256'>" + signatureContent + "</signature>" +
            "</qs3dLicense>";

        private static void WithLicense(string xml, Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-signature-node-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "license.qslic");
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(path, xml);
                action(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }
    }
}
