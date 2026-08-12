using System;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseXmlContentShapeSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            AcceptsWhitespaceFormatting();
            RejectsRootText();
            RejectsRootComment();
            RejectsValidNestedElement();
            RejectsFeaturesComment();
            RejectsFeatureNestedElement();
            RejectsFeatureCData();
        }

        private static void AcceptsWhitespaceFormatting() =>
            WithLicense(Canonical(), path =>
            {
                var license = new LicenseVerifier().Load(path);
                Equal("LIC-CONTENT", license.LicenseId, "canonical license id");
                Equal(1, license.Features.Count, "canonical feature count");
            });

        private static void RejectsRootText() =>
            Reject(Canonical().Replace("\n  <valid", "\n  ignored\n  <valid"), "root text");

        private static void RejectsRootComment() =>
            Reject(Canonical().Replace("\n  <valid", "\n  <!-- ignored -->\n  <valid"), "root comment");

        private static void RejectsValidNestedElement() =>
            Reject(Canonical().Replace("/>\n  <features>", "><shadow/></valid>\n  <features>"), "valid nested element");

        private static void RejectsFeaturesComment() =>
            Reject(Canonical().Replace("<features>\n    <feature", "<features>\n    <!-- ignored -->\n    <feature"), "features comment");

        private static void RejectsFeatureNestedElement() =>
            Reject(Canonical().Replace("<feature name='quantity'/>", "<feature name='quantity'><shadow/></feature>"), "feature nested element");

        private static void RejectsFeatureCData() =>
            Reject(Canonical().Replace("<feature name='quantity'/>", "<feature name='quantity'><![CDATA[ignored]]></feature>"), "feature CDATA");

        private static void Reject(string xml, string label) =>
            WithLicense(xml, path => Throws<InvalidDataException>(() => new LicenseVerifier().Load(path), label));

        private static string Canonical() =>
            "<qs3dLicense schema='1' id='LIC-CONTENT' customer='c' product='p' nonce='n'>\n" +
            "  <valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>\n" +
            "  <features>\n" +
            "    <feature name='quantity'/>\n" +
            "  </features>\n" +
            "  <signature algorithm='RSA-SHA256'>AA==</signature>\n" +
            "</qs3dLicense>";

        private static void WithLicense(string xml, Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-content-shape-" + Guid.NewGuid().ToString("N"));
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
            throw new Exception("LicenseXmlContentShapeSmoke expected " + typeof(TException).Name + " for " + label + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual))
                throw new Exception("LicenseXmlContentShapeSmoke " + label + ": expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
