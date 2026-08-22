using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseSignatureBase64CanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var verifier = new LicenseVerifier();
            var path = Path.Combine(Path.GetTempPath(), "qs3d-license-b64-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                Write(path, "AQIDBA==");
                var canonical = verifier.Load(path);
                Equal(4, canonical.Signature.Length, "canonical signature length");
                Equal((byte)1, canonical.Signature[0], "canonical signature first byte");
                Equal((byte)4, canonical.Signature[3], "canonical signature last byte");

                Write(path, " AQIDBA== ");
                Throws<InvalidDataException>(() => verifier.Load(path), "surrounding whitespace");

                Write(path, "AQID BA==");
                Throws<InvalidDataException>(() => verifier.Load(path), "embedded whitespace");
            }
            finally
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch { }
            }
        }

        private static void Write(string path, string signatureText)
        {
            var xml =
                "<qs3dLicense schema=\"1\" id=\"LIC-1\" customer=\"CUSTOMER-1\" product=\"QS3D\" nonce=\"NONCE-1\">" +
                "<valid notBeforeUtc=\"2026-01-01T00:00:00.0000000Z\" expiresUtc=\"2027-01-01T00:00:00.0000000Z\" />" +
                "<signature algorithm=\"RSA-SHA256\">" + signatureText + "</signature>" +
                "</qs3dLicense>";
            File.WriteAllText(path, xml, new UTF8Encoding(false));
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("LicenseSignatureBase64CanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("LicenseSignatureBase64CanonicalitySmoke " + label + ": expected " + typeof(TException).Name + ".");
        }
    }
}
