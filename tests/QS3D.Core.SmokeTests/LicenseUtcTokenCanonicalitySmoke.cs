using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseUtcTokenCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsCanonicalUtcTokens();
            RejectsEquivalentOffsetToken();
            RejectsMissingZoneToken();
            RejectsNonCanonicalExpiryToken();
        }

        private static void AcceptsCanonicalUtcTokens() =>
            WithLicense(Canonical(), path =>
            {
                var license = new LicenseVerifier().Load(path);
                Equal(DateTimeKind.Utc, license.NotBeforeUtc.Kind, "not-before kind");
                Equal("2026-01-01T00:00:00.0000000Z", license.NotBeforeUtc.ToString("O"), "not-before token");
            });

        private static void RejectsEquivalentOffsetToken() =>
            Reject(Canonical().Replace(
                "2026-01-01T00:00:00.0000000Z",
                "2026-01-01T00:00:00.0000000+00:00"),
                "equivalent offset not-before");

        private static void RejectsMissingZoneToken() =>
            Reject(Canonical().Replace(
                "2026-01-01T00:00:00.0000000Z",
                "2026-01-01T00:00:00.0000000"),
                "missing-zone not-before");

        private static void RejectsNonCanonicalExpiryToken() =>
            Reject(Canonical().Replace(
                "2027-01-01T00:00:00.0000000Z",
                "2027-01-01T07:00:00.0000000+07:00"),
                "equivalent offset expiry");

        private static void Reject(string xml, string label) =>
            WithLicense(xml, path => Throws<InvalidDataException>(() => new LicenseVerifier().Load(path), label));

        private static string Canonical() =>
            "<qs3dLicense schema='1' id='LIC-UTC' customer='c' product='p' nonce='n'>" +
            "<valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>" +
            "<signature algorithm='RSA-SHA256'>AA==</signature>" +
            "</qs3dLicense>";

        private static void WithLicense(string xml, Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-utc-token-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(directory, "license.qslic");
            Directory.CreateDirectory(directory);
            try
            {
                File.WriteAllText(path, xml);
                action(path);
            }
            finally
            {
                try { Directory.Delete(directory, true); }
                catch { }
            }
        }

        private static void Throws<TException>(Action action, string label) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("LicenseUtcTokenCanonicalitySmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("LicenseUtcTokenCanonicalitySmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
