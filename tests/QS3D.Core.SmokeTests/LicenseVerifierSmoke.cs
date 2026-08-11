using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using QS3D.Core.Licensing;

namespace QS3D.Core.SmokeTests
{
    internal static class LicenseVerifierSmoke
    {
        public static void Run()
        {
            SignedLicenseVerifies();
            TamperedLicenseFailsSignature();
            ProductAndTimeWindowsAreEnforced();
            FeatureDelimiterIsRejected();
            DtdLicenseIsRejected();
        }

        private static void SignedLicenseVerifies()
        {
            using (var rsa = RSA.Create(2048))
            {
                var license = License();
                license.Signature = rsa.SignData(license.CanonicalPayload(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var result = new LicenseVerifier().Verify(license, rsa.ExportParameters(false), "QS3D-BricsCAD-V25", Utc(2026, 8, 10));
                Equal(LicenseStatus.Valid, result.Status);
                True(result.IsValid);
                True(result.License.Features.Contains("quantity"));
            }
        }

        private static void TamperedLicenseFailsSignature()
        {
            using (var rsa = RSA.Create(2048))
            {
                var license = License();
                license.Signature = rsa.SignData(license.CanonicalPayload(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                license.Features.Add("tampered");
                var result = new LicenseVerifier().Verify(license, rsa.ExportParameters(false), "QS3D-BricsCAD-V25", Utc(2026, 8, 10));
                Equal(LicenseStatus.InvalidSignature, result.Status);
            }
        }

        private static void ProductAndTimeWindowsAreEnforced()
        {
            using (var rsa = RSA.Create(2048))
            {
                var license = License();
                license.Signature = rsa.SignData(license.CanonicalPayload(), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                var key = rsa.ExportParameters(false);
                Equal(LicenseStatus.ProductMismatch, new LicenseVerifier().Verify(license, key, "OTHER", Utc(2026, 8, 10)).Status);
                Equal(LicenseStatus.NotYetValid, new LicenseVerifier().Verify(license, key, "QS3D-BricsCAD-V25", Utc(2026, 7, 31)).Status);
                Equal(LicenseStatus.Expired, new LicenseVerifier().Verify(license, key, "QS3D-BricsCAD-V25", Utc(2027, 8, 1)).Status);
            }
        }

        private static void FeatureDelimiterIsRejected()
        {
            var license = License();
            license.Features.Add("admin,review");
            Throws<InvalidDataException>(() => license.CanonicalPayload());
        }

        private static void DtdLicenseIsRejected()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "bad.qslic");
            try
            {
                File.WriteAllText(path, "<!DOCTYPE x [<!ENTITY e 'blocked'>]><qs3dLicense schema='1' id='x' customer='c' product='p' nonce='n'><valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/><signature algorithm='RSA-SHA256'>AA==</signature></qs3dLicense>");
                Throws<XmlExceptionOrInvalidData>(() => new LicenseVerifier().Load(path));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static LicenseDocument License()
        {
            var license = new LicenseDocument
            {
                LicenseId = "LIC-001",
                CustomerId = "CUSTOMER-001",
                ProductId = "QS3D-BricsCAD-V25",
                NotBeforeUtc = Utc(2026, 8, 1),
                ExpiresUtc = Utc(2027, 8, 1),
                Nonce = "nonce-001"
            };
            license.Features.Add("quantity");
            license.Features.Add("rebar");
            return license;
        }

        private static DateTime Utc(int year, int month, int day) => new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }

        private sealed class XmlExceptionOrInvalidData : Exception { }
        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (InvalidDataException) when (typeof(T) == typeof(XmlExceptionOrInvalidData)) { return; }
            catch (System.Xml.XmlException) when (typeof(T) == typeof(XmlExceptionOrInvalidData)) { return; }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
