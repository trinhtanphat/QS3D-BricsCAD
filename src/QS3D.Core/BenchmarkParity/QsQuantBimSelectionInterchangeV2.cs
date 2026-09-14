using System;
using System.Linq;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Version 2 integrity wrapper for the canonical version 1 QuantBIM selection interchange.
    /// The wrapped V1 payload remains the semantic authority; V2 only binds all V1 metadata and
    /// payload bytes to one deterministic SHA-256 digest.
    /// </summary>
    public static class QuantBimSelectionInterchangeV2Codec
    {
        private const string Header = "QS3D-QUANTBIM-SELECTION-INTERCHANGE/2";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static string Encode(QuantBimSelectionExportBundle bundle)
        {
            if (bundle == null) throw new ArgumentNullException("bundle");
            return EncodeV1Payload(QuantBimSelectionInterchangeCodec.Encode(bundle));
        }

        public static string Encode(QuantBimSelectionInterchangePackage package)
        {
            if (package == null) throw new ArgumentNullException("package");
            return EncodeV1Payload(QuantBimSelectionInterchangeCodec.Encode(package));
        }

        public static QuantBimSelectionInterchangePackage Decode(string encoded)
        {
            if (encoded == null) throw new ArgumentNullException("encoded");
            if (encoded.IndexOf('\r') >= 0)
                throw new InvalidOperationException("QuantBIM interchange V2 must use canonical LF line endings.");
            if (!encoded.EndsWith("\n", StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange V2 is missing its terminal line ending.");

            var lines = encoded.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length != 4 || lines[3].Length != 0)
                throw new InvalidOperationException("Malformed QuantBIM selection interchange V2 envelope.");
            if (!string.Equals(lines[0], Header, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported QuantBIM selection interchange V2 format.");

            var expectedDigest = ReadDigest(lines[1]);
            var payloadText = ReadLiteral(lines[2], "Payload");
            string v1Payload;
            try
            {
                v1Payload = StrictUtf8.GetString(Convert.FromBase64String(payloadText));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Invalid base64 V1 payload in QuantBIM interchange V2.", ex);
            }
            catch (DecoderFallbackException ex)
            {
                throw new InvalidOperationException("Invalid UTF-8 V1 payload in QuantBIM interchange V2.", ex);
            }

            var actualDigest = QuantBimSelectionInterchangeCodec.HashUtf8(v1Payload);
            if (!string.Equals(actualDigest, expectedDigest, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange V2 whole-envelope integrity check failed.");

            var package = QuantBimSelectionInterchangeCodec.Decode(v1Payload);
            var canonicalV1 = QuantBimSelectionInterchangeCodec.Encode(package);
            if (!string.Equals(canonicalV1, v1Payload, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange V2 contains a non-canonical V1 payload.");
            return package;
        }

        private static string EncodeV1Payload(string v1Payload)
        {
            var digest = QuantBimSelectionInterchangeCodec.HashUtf8(v1Payload);
            var payload = Convert.ToBase64String(StrictUtf8.GetBytes(v1Payload));
            return Header + "\n" +
                "EnvelopeSha256=" + digest + "\n" +
                "Payload=" + payload + "\n";
        }

        private static string ReadDigest(string line)
        {
            var digest = ReadLiteral(line, "EnvelopeSha256");
            if (digest.Length != 64 || digest.Any(x => !(x >= '0' && x <= '9') && !(x >= 'a' && x <= 'f')))
                throw new InvalidOperationException("Invalid whole-envelope SHA-256 in QuantBIM interchange V2.");
            return digest;
        }

        private static string ReadLiteral(string line, string key)
        {
            var prefix = key + "=";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid QuantBIM interchange V2 field; expected " + key + ".");
            return line.Substring(prefix.Length);
        }
    }
}
