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
            CanonicalTokenWhitespaceIsRejected();
            EntitlementSnapshotIdentityCanonicalityIsEnforced();
            LoadedCanonicalTokenWhitespaceIsRejected();
            NamespacedLicenseRootsAreRejected();
            DuplicateLicenseSectionsAreRejected();
            NestedSignatureMarkupIsRejected();
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

        private static void CanonicalTokenWhitespaceIsRejected()
        {
            var leadingScalar = License();
            leadingScalar.LicenseId = " LIC-001";
            Throws<InvalidDataException>(() => leadingScalar.CanonicalPayload());

            var trailingScalar = License();
            trailingScalar.Nonce = "nonce-001 ";
            Throws<InvalidDataException>(() => trailingScalar.CanonicalPayload());

            var paddedFeature = License();
            paddedFeature.Features.Add(" admin ");
            Throws<InvalidDataException>(() => paddedFeature.CanonicalPayload());
        }

        private static void EntitlementSnapshotIdentityCanonicalityIsEnforced()
        {
            const string product = "QS3D-BricsCAD-ĐịnhLượng";
            const string version = "V25-β";
            const string machineId = "MÁY-01";
            const string payload = "signed-payload\nline-2";
            var persistedAt = Utc(2026, 8, 17);

            var snapshot = LicenseEntitlementSnapshot.Create(product, version, machineId, payload, persistedAt);
            Equal(product, snapshot.Product);
            Equal(version, snapshot.ProductVersion);
            Equal(machineId, snapshot.MachineId);
            Equal(payload, snapshot.EntitlementPayload);

            LicenseEntitlementSnapshot restored;
            True(LicenseEntitlementSnapshot.TryDeserialize(snapshot.Serialize(), out restored));
            Equal(product, restored.Product);
            Equal(version, restored.ProductVersion);
            Equal(machineId, restored.MachineId);
            Equal(payload, restored.EntitlementPayload);

            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(" " + product, version, machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product + " ", version, machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product, " " + version, machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product, version + " ", machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product, version, " " + machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product, version, machineId + " ", payload, persistedAt));

            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product + "\u0001", version, machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product, version + "\u0001", machineId, payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create(product, version, machineId + "\u0001", payload, persistedAt));
            Throws<ArgumentException>(() => LicenseEntitlementSnapshot.Create("QS3D-\uD800", version, machineId, payload, persistedAt));
        }

        private static void LoadedCanonicalTokenWhitespaceIsRejected()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-token-load-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                const string valid = "<valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>";
                const string signature = "<signature algorithm='RSA-SHA256'>AA==</signature>";

                var paddedIdPath = Path.Combine(directory, "padded-id.qslic");
                File.WriteAllText(paddedIdPath, "<qs3dLicense schema='1' id=' LIC-001' customer='c' product='p' nonce='n'>" + valid + signature + "</qs3dLicense>");
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(paddedIdPath));

                var paddedFeaturePath = Path.Combine(directory, "padded-feature.qslic");
                File.WriteAllText(paddedFeaturePath, "<qs3dLicense schema='1' id='LIC-001' customer='c' product='p' nonce='n'>" + valid + "<features><feature name=' quantity '/></features>" + signature + "</qs3dLicense>");
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(paddedFeaturePath));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void NamespacedLicenseRootsAreRejected()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-namespace-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var defaultNamespacePath = Path.Combine(directory, "default-namespace.qslic");
                File.WriteAllText(defaultNamespacePath, "<qs3dLicense xmlns='urn:unexpected' schema='1' id='x' customer='c' product='p' nonce='n'><valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/><signature algorithm='RSA-SHA256'>AA==</signature></qs3dLicense>");
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(defaultNamespacePath));

                var prefixedNamespacePath = Path.Combine(directory, "prefixed-namespace.qslic");
                File.WriteAllText(prefixedNamespacePath, "<x:qs3dLicense xmlns:x='urn:unexpected' schema='1' id='x' customer='c' product='p' nonce='n'><valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/><signature algorithm='RSA-SHA256'>AA==</signature></x:qs3dLicense>");
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(prefixedNamespacePath));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void DuplicateLicenseSectionsAreRejected()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-cardinality-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                const string rootStart = "<qs3dLicense schema='1' id='x' customer='c' product='p' nonce='n'>";
                const string valid = "<valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/>";
                const string features = "<features><feature name='quantity'/></features>";
                const string signature = "<signature algorithm='RSA-SHA256'>AA==</signature>";
                const string rootEnd = "</qs3dLicense>";

                var noFeaturesPath = Path.Combine(directory, "no-features.qslic");
                File.WriteAllText(noFeaturesPath, rootStart + valid + signature + rootEnd);
                Equal(0, new LicenseVerifier().Load(noFeaturesPath).Features.Count);

                var duplicateValidPath = Path.Combine(directory, "duplicate-valid.qslic");
                File.WriteAllText(duplicateValidPath, rootStart + valid + valid + signature + rootEnd);
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(duplicateValidPath));

                var duplicateFeaturesPath = Path.Combine(directory, "duplicate-features.qslic");
                File.WriteAllText(duplicateFeaturesPath, rootStart + valid + features + features + signature + rootEnd);
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(duplicateFeaturesPath));

                var duplicateSignaturePath = Path.Combine(directory, "duplicate-signature.qslic");
                File.WriteAllText(duplicateSignaturePath, rootStart + valid + signature + signature + rootEnd);
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(duplicateSignaturePath));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
        }

        private static void NestedSignatureMarkupIsRejected()
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-license-signature-shape-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "nested-signature.qslic");
            try
            {
                File.WriteAllText(path, "<qs3dLicense schema='1' id='x' customer='c' product='p' nonce='n'><valid notBeforeUtc='2026-01-01T00:00:00.0000000Z' expiresUtc='2027-01-01T00:00:00.0000000Z'/><signature algorithm='RSA-SHA256'><shadow>AA==</shadow></signature></qs3dLicense>");
                Throws<InvalidDataException>(() => new LicenseVerifier().Load(path));
            }
            finally
            {
                try { Directory.Delete(directory, true); } catch { }
            }
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
