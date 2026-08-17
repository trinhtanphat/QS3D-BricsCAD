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
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

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
            return StrictUtf8.GetBytes(text);
        }

        public void Validate()
        {
            ValidateToken(LicenseId, nameof(LicenseId), 128);
            ValidateToken(CustomerId, nameof(CustomerId), 256);
            ValidateProductId(ProductId);
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

        internal static void ValidateProductId(string value)
        {
            ValidateToken(value, nameof(ProductId), 128);
        }

        private static void ValidateToken(string value, string name, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("License " + name + " is required.");
            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
                throw new InvalidDataException("License " + name + " must not contain leading or trailing whitespace.");
            if (value.Length > maximumLength) throw new InvalidDataException("License " + name + " is too long.");
            foreach (var ch in value)
                if (char.IsControl(ch) || ch == '\n' || ch == '\r') throw new InvalidDataException("License " + name + " contains control characters.");
            try
            {
                StrictUtf8.GetByteCount(value);
            }
            catch (EncoderFallbackException ex)
            {
                throw new InvalidDataException("License " + name + " must contain well-formed Unicode text.", ex);
            }
        }
    }

    public sealed class LicenseVerificationResult
    {
        private readonly LicenseDocument _license;

        internal LicenseVerificationResult(LicenseStatus status, LicenseDocument license)
        {
            if (!Enum.IsDefined(typeof(LicenseStatus), status))
                throw new ArgumentOutOfRangeException(nameof(status), status, "License verification status is not defined.");
            Status = status;
            _license = CloneLicense(license ?? throw new ArgumentNullException(nameof(license)));
        }

        public LicenseStatus Status { get; }
        public LicenseDocument License => CloneLicense(_license);
        public bool IsValid => Status == LicenseStatus.Valid;

        private static LicenseDocument CloneLicense(LicenseDocument source)
        {
            var clone = new LicenseDocument
            {
                LicenseId = source.LicenseId,
                CustomerId = source.CustomerId,
                ProductId = source.ProductId,
                NotBeforeUtc = source.NotBeforeUtc,
                ExpiresUtc = source.ExpiresUtc,
                Nonce = source.Nonce,
                Signature = source.Signature == null ? Array.Empty<byte>() : (byte[])source.Signature.Clone()
            };
            foreach (var feature in source.Features)
                clone.Features.Add(feature);
            return clone;
        }
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
            try
            {
                LicenseDocument.ValidateProductId(expectedProductId);
            }
            catch (InvalidDataException ex)
            {
                throw new ArgumentException("Expected product id must be a canonical license ProductId.", nameof(expectedProductId), ex);
            }
            if (nowUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("Verification time must be UTC.", nameof(nowUtc));
            license.Validate();
            if (license.Signature == null || license.Signature.Length == 0)
                return new LicenseVerificationResult(LicenseStatus.InvalidSignature, license);

            bool signatureValid;
            using (var rsa = RSA.Create())
            {
                rsa.ImportParameters(publicKey);
                signatureValid = rsa.VerifyData(license.CanonicalPayload(), license.Signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            if (!signatureValid) return new LicenseVerificationResult(LicenseStatus.InvalidSignature, license);
            if (!string.Equals(license.ProductId, expectedProductId, StringComparison.Ordinal))
                return new LicenseVerificationResult(LicenseStatus.ProductMismatch, license);
            if (nowUtc < license.NotBeforeUtc) return new LicenseVerificationResult(LicenseStatus.NotYetValid, license);
            if (nowUtc >= license.ExpiresUtc) return new LicenseVerificationResult(LicenseStatus.Expired, license);
            return new LicenseVerificationResult(LicenseStatus.Valid, license);
        }

        public LicenseDocument Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("License path is required.", nameof(path));
            var full = Path.GetFullPath(path);
            if (!File.Exists(full)) throw new FileNotFoundException("License file was not found.", full);

            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxLicenseBytes
            };
            XDocument document;
            using (var stream = new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length <= 0 || stream.Length > MaxLicenseBytes)
                    throw new InvalidDataException("License file size is invalid.");
                using (var reader = XmlReader.Create(stream, settings))
                    document = XDocument.Load(reader, LoadOptions.None);
            }

            var root = document.Root ?? throw new InvalidDataException("License has no root element.");
            if (!string.IsNullOrEmpty(root.Name.NamespaceName) ||
                !string.Equals(root.Name.LocalName, "qs3dLicense", StringComparison.Ordinal))
                throw new InvalidDataException("Invalid QS3D license root.");
            ValidateAttributes(root, "qs3dLicense", "schema", "id", "customer", "product", "nonce");
            if (!string.Equals(Required(root, "schema"), "1", StringComparison.Ordinal)) throw new InvalidDataException("Unsupported QS3D license schema.");
            ValidateStructuredContent(root, "qs3dLicense", "valid", "features", "signature");

            var valid = RequiredSingleElement(root, "valid");
            var features = OptionalSingleElement(root, "features");
            var signatureElement = RequiredSingleElement(root, "signature");
            ValidateAttributes(valid, "valid", "notBeforeUtc", "expiresUtc");
            ValidateStructuredContent(valid, "valid");
            if (features != null)
            {
                ValidateAttributes(features, "features");
                ValidateStructuredContent(features, "features", "feature");
            }
            ValidateAttributes(signatureElement, "signature", "algorithm");
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
            {
                ValidateAttributes(feature, "feature", "name");
                ValidateStructuredContent(feature, "feature");
                license.Features.Add(Required(feature, "name"));
            }
            if (!string.Equals(Required(signatureElement, "algorithm"), "RSA-SHA256", StringComparison.Ordinal))
                throw new InvalidDataException("Unsupported license signature algorithm.");
            if (signatureElement.HasElements)
                throw new InvalidDataException("License signature must contain text only.");
            ValidateSignatureTextNodes(signatureElement);
            var signatureText = signatureElement.Value ?? string.Empty;
            try
            {
                license.Signature = Convert.FromBase64String(signatureText);
                if (!string.Equals(Convert.ToBase64String(license.Signature), signatureText, StringComparison.Ordinal))
                    throw new InvalidDataException("License signature must use canonical Base64 text.");
            }
            catch (FormatException ex) { throw new InvalidDataException("License signature is not valid Base64.", ex); }
            if (license.Signature.Length > 1024) throw new InvalidDataException("License signature is too large.");
            license.Validate();
            return license;
        }

        private static void ValidateStructuredContent(XElement element, string label, params string[] allowedChildNames)
        {
            var allowed = new HashSet<XName>(allowedChildNames.Select(XName.Get));
            foreach (var node in element.Nodes())
            {
                if (node is XCData)
                    throw new InvalidDataException("Unsupported CDATA content in license <" + label + ">.");
                if (node is XText text)
                {
                    if (!string.IsNullOrWhiteSpace(text.Value))
                        throw new InvalidDataException("Unsupported text content in license <" + label + ">.");
                    continue;
                }
                if (node is XElement child)
                {
                    if (child.Name.Namespace != XNamespace.None || !allowed.Contains(child.Name))
                        throw new InvalidDataException("Unexpected QS3D license child element: <" + label + ">/<" + child.Name + ">.");
                    continue;
                }
                throw new InvalidDataException("Unsupported XML content in license <" + label + ">.");
            }
        }

        private static void ValidateSignatureTextNodes(XElement signatureElement)
        {
            foreach (var node in signatureElement.Nodes())
            {
                if (node is XCData)
                    throw new InvalidDataException("License signature must use ordinary text, not CDATA.");
                if (node is XText) continue;
                throw new InvalidDataException("License signature contains unsupported XML content.");
            }
        }

        private static void ValidateAttributes(XElement element, string label, params string[] allowedNames)
        {
            var allowed = new HashSet<XName>(allowedNames.Select(XName.Get));
            foreach (var attribute in element.Attributes())
            {
                if (attribute.IsNamespaceDeclaration ||
                    attribute.Name.Namespace != XNamespace.None ||
                    !allowed.Contains(attribute.Name))
                    throw new InvalidDataException("Unsupported license attribute on <" + label + ">: " + attribute.Name + ".");
            }
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
            return value!;
        }

        private static DateTime ParseUtc(string value, string label)
        {
            if (!DateTime.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed))
                throw new InvalidDataException("Invalid license UTC timestamp: " + label);
            var utc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            if (!string.Equals(value, utc.ToString("O", CultureInfo.InvariantCulture), StringComparison.Ordinal))
                throw new InvalidDataException("Non-canonical license UTC timestamp: " + label);
            return utc;
        }
    }
}
