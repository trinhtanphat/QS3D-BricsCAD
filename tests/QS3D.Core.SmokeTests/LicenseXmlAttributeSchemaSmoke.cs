using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseXmlAttributeSchemaSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsCanonicalAttributes();
            RejectsUnknownRootAttribute();
            RejectsNamespacedRootAttribute();
            RejectsUnknownValidAttribute();
            RejectsFeaturesAttribute();
            RejectsUnknownFeatureAttribute();
            RejectsUnknownSignatureAttribute();
        }

        private static void AcceptsCanonicalAttributes() =>
            WithLicense(Canonical(), path =>
            {
                var license = new LicenseVerifier().Load(path);
                Equal("LIC-001", license.LicenseId, "canonical license id");
                Equal(1, license.Features.Count, "canonical feature count");
            });

        private static void RejectsUnknownRootAttribute() =>
            Reject(Canonical().Replace(" nonce='n'", " nonce='n' ignored='x'"), "unknown root attribute");

        private static void RejectsNamespacedRootAttribute() =>
            Reject(Canonical().Replace("<qs3dLicense ", "<qs3dLicense xmlns:x='urn:extra' x:ignored='x' "), "namespaced root attribute");

        private static void RejectsUnknownValidAttribute() =>
            Reject(Canonical().Replace(" expiresUtc='2027-01-01T00:00:00.0000000Z'", " expiresUtc='2027-01-01T00:00:00.0000000Z' ignored='x'"), "unknown valid attribute");

        private static void RejectsFeaturesAttribute() =>
            Reject(Canonical().Replace("<features>", "<features ignored='x'>"), "features attribute");

        private static void RejectsUnknownFeatureAttribute() =>
            Reject(Canonical().Replace("name='quantity'", "name='quantity' ignored='x'"), "unknown feature attribute");

        private static void RejectsUnknownSignatureAttribute() =>
            Reject(Canonical().Replace("algorithm='RSA-SHA256'", "algorithm='RSA-SHA256' ignored='x'"), "unknown signature attribute");

        private static void Reject(string xml, string label) =>
            WithLicense(xml, path => Throws<InvalidDataException>(() => new LicenseVerifier().Load(path), label));

        private static string Canonical() =>
            "<qs3dLicense schema='1' id='LIC-001' customer='c' product='p' nonce='n'>" +
            "<valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>" +
            "<features><feature name='quantity'/></features>" +
            "<signature algorithm='RSA-SHA256'>AA==</signature>" +
            "</qs3dLicense>";

        private static void WithLicense(string xml, Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-attributes-" + Guid.NewGuid().ToString("N"));
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
            throw new Exception("LicenseXmlAttributeSchemaSmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("LicenseXmlAttributeSchemaSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
