using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.LocalQualification
{
    // Independent analytic oracle for the two isolated, homothetic footings
    // produced by RunPhase: 4x2 base, 2x1 top, 1m base + 1m frustum.
    // Do not call the product quantity builder to compute the expected answer.
    public static class Local022QuantityOracle
    {
        public const double EachVolume = 38d / 3d;
        public const double TotalVolume = 76d / 3d;

        public sealed class Row
        {
            public string FamilyId = string.Empty;
            public string Category = string.Empty;
            public string Fingerprint = string.Empty;
            public string[] ElementIds = Array.Empty<string>();
            public string[] SourceHandles = Array.Empty<string>();
            public int Count;
            public double Gross;
            public double Net;
            public double Deduction;
            public bool GrossEvidence;
            public bool NetEvidence;
            public bool DeductionEvidence;
        }

        public static void Verify(IReadOnlyList<Row> rows, bool detail, string familyId,
            string fingerprint, IReadOnlyDictionary<string, string> sourceByElement)
        {
            if (string.IsNullOrWhiteSpace(familyId) || string.IsNullOrWhiteSpace(fingerprint) ||
                sourceByElement.Count != 2 || sourceByElement.Any(x => string.IsNullOrWhiteSpace(x.Key) || string.IsNullOrWhiteSpace(x.Value)) ||
                sourceByElement.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 2)
                throw new InvalidOperationException("quantity_fixture_identity");
            if (rows.Count != (detail ? 2 : 1)) throw new InvalidOperationException("quantity_row_cardinality");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows)
            {
                if (row.FamilyId != familyId || row.Category != "Foundation" || row.Fingerprint != fingerprint)
                    throw new InvalidOperationException("quantity_row_identity");
                if (row.Count != (detail ? 1 : 2) || row.ElementIds.Length != row.Count || row.SourceHandles.Length != row.Count)
                    throw new InvalidOperationException("quantity_element_cardinality");
                foreach (var id in row.ElementIds)
                    if (!sourceByElement.ContainsKey(id) || !seen.Add(id)) throw new InvalidOperationException("quantity_element_identity");
                var expectedHandles = row.ElementIds.Select(id => sourceByElement[id]).OrderBy(x => x, StringComparer.OrdinalIgnoreCase);
                if (!row.SourceHandles.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).SequenceEqual(expectedHandles, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException("quantity_source_identity");
                if (!row.GrossEvidence || !row.NetEvidence || !row.DeductionEvidence)
                    throw new InvalidOperationException("quantity_evidence_missing");
                var expected = row.Count * EachVolume;
                RequireNear(row.Gross, expected, "quantity_gross_volume");
                RequireNear(row.Net, expected, "quantity_net_volume");
                RequireNear(row.Deduction, 0d, "quantity_deduction");
            }
            if (seen.Count != 2) throw new InvalidOperationException("quantity_missing_element");
        }

        private static void RequireNear(double actual, double expected, string code)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(actual - expected) > 1e-7d)
                throw new InvalidOperationException(code);
        }
    }
}
