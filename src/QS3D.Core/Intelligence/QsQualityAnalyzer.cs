using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Intelligence
{
    /// <summary>
    /// Solibri-style model information checks focused on quantity reliability before BOQ/estimate use.
    /// </summary>
    public sealed class QsQualityAnalyzer
    {
        public IReadOnlyList<QsQualityFinding> Analyze(IEnumerable<QsQuantityRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            var snapshot = Snapshot(records);
            var findings = new List<QsQualityFinding>();
            var byElement = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var byGeometry = new Dictionary<string, List<QsQuantityRecord>>(StringComparer.OrdinalIgnoreCase);
            var unitsByScope = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < snapshot.Count; i++)
            {
                var record = snapshot[i];
                if (byElement.TryGetValue(record.ElementId, out var firstIndex))
                {
                    findings.Add(new QsQualityFinding(
                        "QS.DUPLICATE_ELEMENT_ID",
                        QsQualitySeverity.Error,
                        record.ElementId,
                        "Duplicate element id; first occurrence is at index " + firstIndex + "."));
                }
                else
                {
                    byElement.Add(record.ElementId, i);
                }

                if (string.IsNullOrEmpty(record.ClassificationCode))
                {
                    findings.Add(new QsQualityFinding(
                        "QS.MISSING_CLASSIFICATION",
                        QsQualitySeverity.Error,
                        record.ElementId,
                        "Element has no QS classification code."));
                }

                if (string.IsNullOrEmpty(record.WbsCode))
                {
                    findings.Add(new QsQualityFinding(
                        "QS.MISSING_WBS",
                        QsQualitySeverity.Warning,
                        record.ElementId,
                        "Element is not mapped to a WBS code."));
                }

                if (string.IsNullOrEmpty(record.CostCode))
                {
                    findings.Add(new QsQualityFinding(
                        "QS.MISSING_COST_CODE",
                        QsQualitySeverity.Warning,
                        record.ElementId,
                        "Element is not mapped to a cost code; 5D valuation cannot be fully resolved."));
                }

                if (string.IsNullOrEmpty(record.GeometryFingerprint))
                {
                    findings.Add(new QsQualityFinding(
                        "QS.MISSING_GEOMETRY_FINGERPRINT",
                        QsQualitySeverity.Info,
                        record.ElementId,
                        "Geometry fingerprint is missing; geometric duplicate detection is reduced."));
                }
                else
                {
                    if (!byGeometry.TryGetValue(record.GeometryFingerprint, out var sameGeometry))
                    {
                        sameGeometry = new List<QsQuantityRecord>();
                        byGeometry.Add(record.GeometryFingerprint, sameGeometry);
                    }
                    sameGeometry.Add(record);
                }

                var scope = (string.IsNullOrEmpty(record.ClassificationCode) ? "<unclassified>" : record.ClassificationCode) +
                    "|" + record.QuantityType;
                if (!unitsByScope.TryGetValue(scope, out var units))
                {
                    units = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    unitsByScope.Add(scope, units);
                }
                units.Add(record.Unit);
            }

            foreach (var pair in byGeometry)
            {
                if (pair.Value.Count < 2) continue;
                for (var i = 0; i < pair.Value.Count; i++)
                {
                    findings.Add(new QsQualityFinding(
                        "QS.DUPLICATE_GEOMETRY",
                        QsQualitySeverity.Warning,
                        pair.Value[i].ElementId,
                        "Geometry fingerprint is shared by " + pair.Value.Count + " quantity records."));
                }
            }

            foreach (var pair in unitsByScope)
            {
                if (pair.Value.Count <= 1) continue;
                findings.Add(new QsQualityFinding(
                    "QS.INCONSISTENT_UNIT",
                    QsQualitySeverity.Error,
                    "*",
                    "Quantity scope " + pair.Key + " uses multiple units: " + JoinOrdered(pair.Value) + "."));
            }

            findings.Sort(CompareFindings);
            return new ReadOnlyCollection<QsQualityFinding>(findings.ToArray());
        }

        private static IReadOnlyList<QsQuantityRecord> Snapshot(IEnumerable<QsQuantityRecord> records)
        {
            var snapshot = new List<QsQuantityRecord>();
            foreach (var record in records)
            {
                if (record == null) throw new ArgumentException("QS record must not be null.", nameof(records));
                snapshot.Add(record);
            }
            return new ReadOnlyCollection<QsQuantityRecord>(snapshot.ToArray());
        }

        private static string JoinOrdered(HashSet<string> values)
        {
            var ordered = new List<string>(values);
            ordered.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", ordered.ToArray());
        }

        private static int CompareFindings(QsQualityFinding left, QsQualityFinding right)
        {
            var severity = right.Severity.CompareTo(left.Severity);
            if (severity != 0) return severity;
            var rule = StringComparer.Ordinal.Compare(left.RuleId, right.RuleId);
            if (rule != 0) return rule;
            return StringComparer.OrdinalIgnoreCase.Compare(left.ElementId, right.ElementId);
        }
    }
}
