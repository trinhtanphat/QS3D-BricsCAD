using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseParsedStreamSizeSmoke
    {
        private const int MaxLicenseBytes = 64 * 1024;

        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            EmptyAndOversizedFilesFailThroughSizeGuard();
            ValidCanonicalLicenseStillLoads();
        }

        private static void EmptyAndOversizedFilesFailThroughSizeGuard()
        {
            var root = TempRoot("bounds");
            var empty = Path.Combine(root, "empty.license.xml");
            var oversized = Path.Combine(root, "oversized.license.xml");
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllBytes(empty, Array.Empty<byte>());
                File.WriteAllBytes(oversized, new byte[MaxLicenseBytes + 1]);

                ThrowsSize(() => new LicenseVerifier().Load(empty), "empty parsed stream");
                ThrowsSize(() => new LicenseVerifier().Load(oversized), "oversized parsed stream");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void ValidCanonicalLicenseStillLoads()
        {
            var root = TempRoot("valid");
            var path = Path.Combine(root, "license.xml");
            try
            {
                Directory.CreateDirectory(root);
                var xml =
                    "<qs3dLicense schema=\"1\" id=\"LIC-STREAM\" customer=\"CUSTOMER-1\" product=\"QS3D\" nonce=\"NONCE-1\">" +
                    "<valid notBeforeUtc=\"2026-01-01T00:00:00.0000000Z\" expiresUtc=\"2027-01-01T00:00:00.0000000Z\" />" +
                    "<signature algorithm=\"RSA-SHA256\"></signature>" +
                    "</qs3dLicense>";
                File.WriteAllText(path, xml, new UTF8Encoding(false));

                var license = new LicenseVerifier().Load(path);
                Require(string.Equals(license.LicenseId, "LIC-STREAM", StringComparison.Ordinal),
                    "Valid canonical license id did not load after parsed-stream size binding.");
                Require(string.Equals(license.ProductId, "QS3D", StringComparison.Ordinal),
                    "Valid canonical license product did not load after parsed-stream size binding.");
                Require(license.Signature.Length == 0,
                    "Empty canonical signature behavior changed after parsed-stream size binding.");
            }
            finally
            {
                DeleteTree(root);
            }
        }

        private static void ThrowsSize(Action action, string label)
        {
            try
            {
                action();
            }
            catch (InvalidDataException ex)
            {
                if (!string.Equals(ex.Message, "License file size is invalid.", StringComparison.Ordinal))
                    throw new InvalidOperationException("Unexpected License size error for " + label + ".", ex);
                return;
            }
            throw new InvalidOperationException("Expected License size rejection for " + label + ".");
        }

        private static string TempRoot(string suffix) =>
            Path.Combine(Path.GetTempPath(), "QS3D-LicenseStreamSize-" + suffix + "-" + Guid.NewGuid().ToString("N"));

        private static void DeleteTree(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
