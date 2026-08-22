using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseXmlChildShapeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-child-shape-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                const string rootStart = "<qs3dLicense schema='1' id='LIC-001' customer='CUSTOMER-001' product='QS3D-BricsCAD-V25' nonce='nonce-001'>";
                const string valid = "<valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>";
                const string signature = "<signature algorithm='RSA-SHA256'>AA==</signature>";
                const string rootEnd = "</qs3dLicense>";

                var ordinaryPath = Path.Combine(directory, "ordinary.qslic");
                File.WriteAllText(ordinaryPath, rootStart + valid + signature + rootEnd);
                var ordinary = new LicenseVerifier().Load(ordinaryPath);
                if (!string.Equals(ordinary.LicenseId, "LIC-001", StringComparison.Ordinal))
                    throw new Exception("Ordinary license child shape no longer loads.");

                var unexpectedPath = Path.Combine(directory, "unexpected-child.qslic");
                File.WriteAllText(unexpectedPath, rootStart + valid + "<shadow product='OTHER'/>" + signature + rootEnd);
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(unexpectedPath));

                var namespacedPath = Path.Combine(directory, "namespaced-child.qslic");
                File.WriteAllText(namespacedPath, rootStart + valid + "<x:valid xmlns:x='urn:shadow' notBeforeUtc='2020-01-01T00:00:00.0000000Z' expiresUtc='2030-01-01T00:00:00.0000000Z'/>" + signature + rootEnd);
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(namespacedPath));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
