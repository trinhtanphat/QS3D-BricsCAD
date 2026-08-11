using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace QS3D.Core.Licensing
{
    public enum LicenseStatus
    {
        Valid,
        InvalidSignature,
        ProductMismatch,
        NotYetValid,
        Expired
    }

    public sealed class LicenseDocument
    {
        public string LicenseId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public DateTime NotBeforeUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public IList<string> Features { get; } = new List<string>();
        public string Nonce { get; set; } = string.Empty;
        public byte[] Signature { get; set; } = Array.Empty<byte>();

        public byte[] CanonicalPayload()
        {
            Validate();
            var features = Features.OrderBy(x => x, StringComparer.Ordinal).ToArray();
            var text = string.Join("\n", new[]
            {
                "QS3D-LICENSE-V1",
                "license=" + LicenseId,
                "customer=" + CustomerId,
                "product=" + ProductId,
                "notBefore=" + NotBeforeUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                "expires=" + ExpiresUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
                "features=" + string.Join(",", features),
                "nonce=" + Nonce
            });
            return Encoding.UTF8.GetBytes(text);
        }

        public void Validate()
        {
            ValidateToken(LicenseId, nameof(LicenseId), 128);
            ValidateToken(CustomerId, nameof(CustomerId), 256);
            ValidateToken(ProductId, nameof(ProductId), 128);
            ValidateToken(Nonce, nameof(Nonce), 256);
            if (NotBeforeUtc.Kind != DateTimeKind.Utc || ExpiresUtc.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("License validity timestamps must be UTC.");
            if (ExpiresUtc <= NotBeforeUtc) throw new InvalidDataException("License expiration must be after not-before time.");
            if (Features.Count > 128) throw new InvalidDataException("License contains too many features.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var feature in Features)
            {
                ValidateToken(feature, "feature", 128);
                if (feature.IndexOf(',') >= 0) throw new InvalidDataException("License feature contains the reserved ',' delimiter.");
                if (!seen.Add(feature)) throw new InvalidDataException("Duplicate license feature: " + feature);
            }
        }

        private static void ValidateToken(string value, string name, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("License " + name + " is required.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("License " + name + " must not contain leading or trailing whitespace.");
            if (value.Length > maximumLength) throw new InvalidDataException("License " + name + " is too long.");
            foreach (var ch in value)
                if (char.IsControl(ch) || ch == '\n' || ch == '\r') throw new InvalidDataException("License " + name + " contains control characters.");
        }
    }

    public sealed class LicenseVerificationResult
    {
        public LicenseVerificationResult(LicenseStatus status, LicenseDocument license)
        {
            Status = status;
            License = license ?? throw new ArgumentNullException(nameof(license));
        }
        public LicenseStatus Status { get; }
        public LicenseDocument License { get; }
        public bool IsValid => Status == LicenseStatus.Valid;
    }

    public sealed class LicenseVerifier
    {
        private const long MaxLicenseBytes = 64L * 1024L;

        public LicenseVerificationResult Verify(
            LicenseDocument license,
            RSAParameters publicKey,
            string expectedProductId,
            DateTime nowUtc)
        {
            if (license == null) throw new ArgumentNullException(nameof(license));
            if (string.IsNullOrWhiteSpace(expectedProductId)) throw new ArgumentException("Expected product id is required.", nameof(expectedProductId));
            if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Verification time must be UTC.", nameof(nowUtc));
            license.Validate();
            if (!string.Equals(license.ProductId, expectedProductId, StringComparison.Ordinal))
                return new LicenseVerificationResult(LicenseStatus.ProductMismatch, license);
            if (license.Signature == null || license.Signature.Length == 0)
                return new LicenseVerificationResult(LicenseStatus.InvalidSignature, license);

            bool signatureValid;
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(publicKey);
                signatureValid = rsa.VerifyData(license.CanonicalPayload(), license.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            if (!signatureValid) return new LicenseVerificationResult(LicenseStatus.InvalidSignature, license);
            if (nowUtc < license.NotBeforeUtc) return new LicenseVerificationResult(LicenseStatus.NotYetValid, license);
            if (nowUtc >= license.ExpiresUtc) return new LicenseVerificationResult(LicenseStatus.Expired, license);
            return new LicenseVerificationResult(LicenseStatus.Valid, license);
        }

        public LicenseDocument Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("License path is required.", nameof(path));
            var full = Path.GetFullPath(path);
            var info = new FileInfo(full);
            if (!info.Exists) throw new FileNotFoundException("License file was not found.", full);
            if (info.Length <= 0 || info.Length > MaxLicenseBytes) throw new InvalidDataException("License file size is invalid.");

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxLicenseBytes
            };
            XDocument document;
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = XmlReader.Create(stream, settings))
                document = XDocument.Load(reader, LoadOptions.None);

            var root = document.Root ?? throw new InvalidDataException("License has no root element.");
            if (!string.IsNullOrEmpty(root.Name.NamespaceName) ||
                !string.Equals(root.Name.LocalName, "qs3dLicense", StringComparison.Ordinal))
                throw new InvalidDataException("Invalid QS3D license root.");
            if (!string.Equals(Required(root, "schema"), "1", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported QS3D license schema.");

            var valid = RequiredSingleElement(root, "valid");
            var features = OptionalSingleElement(root, "features");
            var signatureElement = RequiredSingleElement(root, "signature");
            var license = new LicenseDocument
            {
                LicenseId = Required(root, "id"),
                CustomerId = Required(root, "customer"),
                ProductId = Required(root, "product"),
                NotBeforeUtc = ParseUtc(Required(valid, "notBeforeUtc"), "notBeforeUtc"),
                ExpiresUtc = ParseUtc(Required(valid, "expiresUtc"), "expiresUtc"),
                Nonce = Required(root, "nonce")
            };
            foreach (var feature in features?.Elements("feature") ?? Enumerable.Empty<XElement>())
                license.Features.Add(Required(feature, "name"));
            if (!string.Equals(Required(signatureElement, "algorithm"), "RSA-SHA256", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported license signature algorithm.");
            if (signatureElement.HasElements)
                throw new InvalidDataException("License signature must contain text only.");
            try { license.Signature = Convert.FromBase64String((signatureElement.Value ?? string.Empty).Trim()); }
            catch (FormatException ex) { throw new InvalidDataException("License signature is not valid Base64.", ex); }
            if (license.Signature.Length > 1024) throw new InvalidDataException("License signature is too large.");
            license.Validate();
            return license;
        }

        private static XElement RequiredSingleElement(XElement parent, string name)
        {
            var matches = parent.Elements(name).Take(2).ToArray();
            if (matches.Length != 1)
                throw new InvalidDataException("License must contain exactly one <" + name + "> element.");
            return matches[0];
        }

        private static XElement? OptionalSingleElement(XElement parent, string name)
        {
            var matches = parent.Elements(name).Take(2).ToArray();
            if (matches.Length > 1)
                throw new InvalidDataException("License must contain at most one <" + name + "> element.");
            return matches.Length == 0 ? null : matches[0];
        }

        private static string Required(XElement element, string attribute)
        {
            var value = (string?)element.Attribute(attribute);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("License attribute is required: " + attribute);
            return value!.Trim();
        }

        private static DateTime ParseUtc(string value, string label)
        {
            if (!DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                throw new InvalidDataException("Invalid license UTC timestamp: " + label);
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }
    }
}
