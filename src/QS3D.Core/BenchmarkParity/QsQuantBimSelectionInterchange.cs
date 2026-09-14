using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.BenchmarkParity
{
    public sealed class QuantBimSelectionInterchangePackage
    {
        internal QuantBimSelectionInterchangePackage(
            string documentPath,
            string revision,
            string selectionName,
            IEnumerable<string> selectionGuids,
            string boqCsv,
            string evidenceCsv)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            SelectionName = QsModelElementSnapshot.Require(selectionName, "selectionName");
            SelectionGuids = new ReadOnlyCollection<string>((selectionGuids ?? throw new ArgumentNullException("selectionGuids"))
                .Select(x => QsModelElementSnapshot.Require(x, "selectionGuid"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList());
            BoqCsv = boqCsv ?? throw new ArgumentNullException("boqCsv");
            EvidenceCsv = evidenceCsv ?? throw new ArgumentNullException("evidenceCsv");
            BoqSha256 = QuantBimSelectionInterchangeCodec.HashUtf8(BoqCsv);
            EvidenceSha256 = QuantBimSelectionInterchangeCodec.HashUtf8(EvidenceCsv);
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public string SelectionName { get; private set; }
        public IReadOnlyList<string> SelectionGuids { get; private set; }
        public string BoqCsv { get; private set; }
        public string EvidenceCsv { get; private set; }
        public string BoqSha256 { get; private set; }
        public string EvidenceSha256 { get; private set; }
    }

    public static class QuantBimSelectionInterchangeCodec
    {
        private const string Header = "QS3D-QUANTBIM-SELECTION-INTERCHANGE/1";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static QuantBimSelectionInterchangePackage FromBundle(QuantBimSelectionExportBundle bundle)
        {
            if (bundle == null) throw new ArgumentNullException("bundle");
            return new QuantBimSelectionInterchangePackage(
                bundle.DocumentPath,
                bundle.Revision,
                bundle.SelectionName,
                bundle.SelectionGuids,
                bundle.BoqCsv,
                bundle.EvidenceCsv);
        }

        public static string Encode(QuantBimSelectionExportBundle bundle)
        {
            return Encode(FromBundle(bundle));
        }

        public static string Encode(QuantBimSelectionInterchangePackage package)
        {
            if (package == null) throw new ArgumentNullException("package");
            var lines = new List<string>
            {
                Header,
                Field("DocumentPath", package.DocumentPath),
                Field("Revision", package.Revision),
                Field("SelectionName", package.SelectionName),
                "GuidCount=" + package.SelectionGuids.Count.ToString(CultureInfo.InvariantCulture)
            };
            foreach (var guid in package.SelectionGuids) lines.Add(Field("Guid", guid));
            lines.Add("BoqSha256=" + package.BoqSha256);
            lines.Add("EvidenceSha256=" + package.EvidenceSha256);
            lines.Add(Field("BoqCsv", package.BoqCsv));
            lines.Add(Field("EvidenceCsv", package.EvidenceCsv));
            return string.Join("\n", lines) + "\n";
        }

        public static QuantBimSelectionInterchangePackage Decode(string encoded)
        {
            if (encoded == null) throw new ArgumentNullException("encoded");
            if (encoded.IndexOf('\r') >= 0)
                throw new InvalidOperationException("QuantBIM interchange must use canonical LF line endings.");
            if (!encoded.EndsWith("\n", StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange is missing its terminal line ending.");

            var lines = encoded.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length < 10 || lines[lines.Length - 1].Length != 0)
                throw new InvalidOperationException("Malformed QuantBIM selection interchange envelope.");
            if (!string.Equals(lines[0], Header, StringComparison.Ordinal))
                throw new InvalidOperationException("Unsupported QuantBIM selection interchange format.");

            var documentPath = ReadField(lines[1], "DocumentPath");
            var revision = ReadField(lines[2], "Revision");
            var selectionName = ReadField(lines[3], "SelectionName");
            var guidCountText = ReadLiteral(lines[4], "GuidCount");
            int guidCount;
            if (!int.TryParse(guidCountText, NumberStyles.None, CultureInfo.InvariantCulture, out guidCount) || guidCount < 0 || guidCount > 100000)
                throw new InvalidOperationException("Invalid QuantBIM selection GUID count.");

            var expectedNonEmptyLines = 9 + guidCount;
            if (lines.Length != expectedNonEmptyLines + 1)
                throw new InvalidOperationException("Unexpected field count in QuantBIM selection interchange envelope.");

            var guids = new List<string>(guidCount);
            var index = 5;
            for (var i = 0; i < guidCount; i++) guids.Add(ReadField(lines[index++], "Guid"));
            var expectedBoqHash = ReadHash(lines[index++], "BoqSha256");
            var expectedEvidenceHash = ReadHash(lines[index++], "EvidenceSha256");
            var boqCsv = ReadField(lines[index++], "BoqCsv");
            var evidenceCsv = ReadField(lines[index], "EvidenceCsv");

            var package = new QuantBimSelectionInterchangePackage(
                documentPath,
                revision,
                selectionName,
                guids,
                boqCsv,
                evidenceCsv);
            if (!string.Equals(package.BoqSha256, expectedBoqHash, StringComparison.Ordinal) ||
                !string.Equals(package.EvidenceSha256, expectedEvidenceHash, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM selection interchange payload integrity check failed.");
            if (package.SelectionGuids.Count != guidCount)
                throw new InvalidOperationException("QuantBIM selection interchange contains duplicate GUID identity.");
            return package;
        }

        internal static string HashUtf8(string value)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = StrictUtf8.GetBytes(value ?? throw new ArgumentNullException("value"));
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                foreach (var item in hash) builder.Append(item.ToString("x2", CultureInfo.InvariantCulture));
                return builder.ToString();
            }
        }

        private static string Field(string key, string value)
        {
            return key + "=" + Convert.ToBase64String(StrictUtf8.GetBytes(value ?? string.Empty));
        }

        private static string ReadField(string line, string key)
        {
            var payload = ReadLiteral(line, key);
            try
            {
                return StrictUtf8.GetString(Convert.FromBase64String(payload));
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Invalid base64 payload for QuantBIM interchange field " + key + ".", ex);
            }
            catch (DecoderFallbackException ex)
            {
                throw new InvalidOperationException("Invalid UTF-8 payload for QuantBIM interchange field " + key + ".", ex);
            }
        }

        private static string ReadHash(string line, string key)
        {
            var hash = ReadLiteral(line, key);
            if (hash.Length != 64 || hash.Any(x => !(x >= '0' && x <= '9') && !(x >= 'a' && x <= 'f')))
                throw new InvalidOperationException("Invalid SHA-256 field in QuantBIM selection interchange envelope: " + key + ".");
            return hash;
        }

        private static string ReadLiteral(string line, string key)
        {
            var prefix = key + "=";
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                throw new InvalidOperationException("Invalid QuantBIM interchange field; expected " + key + ".");
            return line.Substring(prefix.Length);
        }
    }
}
