using System;
using System.Collections.Generic;

namespace QS3D.Core.Features
{
    public static class ParityManifestParser
    {
        private const string Header = "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason";
        private const string CatalogPrefix = "# catalog-complete=";

        public static ParityManifest Parse(IEnumerable<string> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            bool? catalogComplete = null;
            var headerSeen = false;
            var records = new List<ParityFeatureRecord>();
            var lineNumber = 0;

            foreach (var rawLine in lines)
            {
                lineNumber++;
                if (rawLine == null) throw Error(lineNumber, "line cannot be null");
                var line = rawLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (catalogComplete == null)
                {
                    if (!line.StartsWith(CatalogPrefix, StringComparison.Ordinal))
                        throw Error(lineNumber, "first nonblank line must declare catalog completeness");
                    var token = line.Substring(CatalogPrefix.Length);
                    if (token == "true") catalogComplete = true;
                    else if (token == "false") catalogComplete = false;
                    else throw Error(lineNumber, "catalog-complete must be exactly true or false");
                    continue;
                }

                if (!headerSeen)
                {
                    if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (!string.Equals(line, Header, StringComparison.Ordinal))
                        throw Error(lineNumber, "manifest header is not the canonical eight-column header");
                    headerSeen = true;
                    continue;
                }

                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                var fields = line.Split(new[] { '\t' }, StringSplitOptions.None);
                if (fields.Length != 8) throw Error(lineNumber, "data row must contain exactly eight tab-separated fields");

                var applicability = ParseEnum<ParityApplicability>(fields[4], lineNumber, "Applicability");
                var evidenceStage = ParseEnum<ParityEvidenceStage>(fields[5], lineNumber, "EvidenceStage");
                try
                {
                    records.Add(new ParityFeatureRecord(
                        new FeatureId(fields[0]), fields[1], fields[2], fields[3],
                        applicability, evidenceStage, fields[6], fields[7]));
                }
                catch (ArgumentException ex)
                {
                    throw Error(lineNumber, ex.Message);
                }
            }

            if (catalogComplete == null) throw new FormatException("Parity manifest is missing catalog-complete metadata.");
            if (!headerSeen) throw new FormatException("Parity manifest is missing the canonical header.");
            try { return new ParityManifest(records, catalogComplete.Value); }
            catch (InvalidOperationException ex) { throw new FormatException("Parity manifest validation failed: " + ex.Message, ex); }
        }

        private static T ParseEnum<T>(string raw, int lineNumber, string fieldName) where T : struct
        {
            if (!Enum.TryParse(raw, false, out T value) || !Enum.IsDefined(typeof(T), value) || !string.Equals(raw, value.ToString(), StringComparison.Ordinal))
                throw Error(lineNumber, fieldName + " contains an invalid enum name: " + raw);
            return value;
        }

        private static FormatException Error(int lineNumber, string detail) =>
            new FormatException("Parity manifest line " + lineNumber + ": " + detail + ".");
    }
}