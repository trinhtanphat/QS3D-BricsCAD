using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    public enum QsQaGuardedWorkflow
    {
        Takeoff,
        Boq,
        Estimate
    }

    public sealed class QsQaRuleProfile
    {
        private readonly IReadOnlyDictionary<string, QsQaSeverity> _severityByRule;

        public QsQaRuleProfile(
            IEnumerable<string> requiredPsets,
            IEnumerable<string> requiredRelationships,
            IDictionary<string, QsQaSeverity> severityByRule,
            QsQaSeverity blockingThreshold)
        {
            RequiredPsets = Normalize(requiredPsets, "requiredPsets");
            RequiredRelationships = Normalize(requiredRelationships, "requiredRelationships");
            _severityByRule = new ReadOnlyDictionary<string, QsQaSeverity>(
                new Dictionary<string, QsQaSeverity>(severityByRule ?? new Dictionary<string, QsQaSeverity>(), StringComparer.OrdinalIgnoreCase));
            BlockingThreshold = blockingThreshold;
        }

        public IReadOnlyList<string> RequiredPsets { get; private set; }
        public IReadOnlyList<string> RequiredRelationships { get; private set; }
        public QsQaSeverity BlockingThreshold { get; private set; }

        public QsQaSeverity SeverityFor(string ruleId, QsQaSeverity fallback)
        {
            QsQaSeverity value;
            return _severityByRule.TryGetValue(ruleId, out value) ? value : fallback;
        }

        public static QsQaRuleProfile SolibriQuantityStrict()
        {
            return new QsQaRuleProfile(
                new[] { "Pset_Qto", "Pset_Identity" },
                new[] { "SpatialContainer", "TypeAssignment" },
                new Dictionary<string, QsQaSeverity>(StringComparer.OrdinalIgnoreCase)
                {
                    { "QA2.MISSING_MATERIAL", QsQaSeverity.Error },
                    { "QA2.MISSING_TYPE", QsQaSeverity.Error },
                    { "QA2.INVALID_DIMENSIONS", QsQaSeverity.Error },
                    { "QA2.MISSING_PSET", QsQaSeverity.Error },
                    { "QA2.MISSING_RELATIONSHIP", QsQaSeverity.Critical },
                    { "QA2.SPATIAL_MISMATCH", QsQaSeverity.Critical },
                    { "QA2.TYPE_ASSIGNMENT_MISMATCH", QsQaSeverity.Critical },
                    { "QA2.DUPLICATE_IFC_GUID", QsQaSeverity.Critical }
                },
                QsQaSeverity.Error);
        }

        private static IReadOnlyList<string> Normalize(IEnumerable<string> values, string name)
        {
            return new ReadOnlyCollection<string>((values ?? Enumerable.Empty<string>())
                .Select(x => QsModelElementSnapshot.Require(x, name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }
    }

    public sealed class QsQaWaiver
    {
        public QsQaWaiver(string ruleId, string elementId, string reason, string approvedBy, DateTime approvedUtc, DateTime? expiresUtc)
        {
            RuleId = QsModelElementSnapshot.Require(ruleId, "ruleId");
            ElementId = QsModelElementSnapshot.Require(elementId, "elementId");
            Reason = QsModelElementSnapshot.Require(reason, "reason");
            ApprovedBy = QsModelElementSnapshot.Require(approvedBy, "approvedBy");
            ApprovedUtc = approvedUtc.Kind == DateTimeKind.Utc ? approvedUtc : approvedUtc.ToUniversalTime();
            ExpiresUtc = expiresUtc.HasValue
                ? (expiresUtc.Value.Kind == DateTimeKind.Utc ? expiresUtc.Value : expiresUtc.Value.ToUniversalTime())
                : (DateTime?)null;
            if (ExpiresUtc.HasValue && ExpiresUtc.Value < ApprovedUtc) throw new ArgumentException("Waiver expiry cannot predate approval.", "expiresUtc");
        }

        public string RuleId { get; private set; }
        public string ElementId { get; private set; }
        public string Reason { get; private set; }
        public string ApprovedBy { get; private set; }
        public DateTime ApprovedUtc { get; private set; }
        public DateTime? ExpiresUtc { get; private set; }

        public bool Applies(QsQaFinding finding, DateTime nowUtc)
        {
            if (finding == null) return false;
            if (!string.Equals(RuleId, finding.RuleId, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.Equals(ElementId, finding.ElementId, StringComparison.OrdinalIgnoreCase)) return false;
            return !ExpiresUtc.HasValue || nowUtc <= ExpiresUtc.Value;
        }
    }

    public sealed class QsQaGate2Decision
    {
        public QsQaGate2Decision(
            QsQaGateStatus status,
            IReadOnlyList<QsQaFinding> activeFindings,
            IReadOnlyList<QsQaFinding> waivedFindings)
        {
            Status = status;
            ActiveFindings = activeFindings ?? throw new ArgumentNullException("activeFindings");
            WaivedFindings = waivedFindings ?? throw new ArgumentNullException("waivedFindings");
        }

        public QsQaGateStatus Status { get; private set; }
        public IReadOnlyList<QsQaFinding> ActiveFindings { get; private set; }
        public IReadOnlyList<QsQaFinding> WaivedFindings { get; private set; }
        public bool CanTakeoff { get { return Status != QsQaGateStatus.Blocked; } }
        public bool CanBoq { get { return Status != QsQaGateStatus.Blocked; } }
        public bool CanEstimate { get { return Status != QsQaGateStatus.Blocked; } }

        public void DemandAllowed(QsQaGuardedWorkflow workflow)
        {
            if (Status != QsQaGateStatus.Blocked) return;

            throw new InvalidOperationException(
                "QA Gate 2.0 blocked " + workflow + " because " + ActiveFindings.Count + " active finding(s) meet the configured blocking threshold.");
        }
    }

    public sealed class QsQaGuardedExecutor
    {
        public T Execute<T>(QsQaGate2Decision decision, QsQaGuardedWorkflow workflow, Func<T> work)
        {
            if (decision == null) throw new ArgumentNullException("decision");
            if (work == null) throw new ArgumentNullException("work");

            decision.DemandAllowed(workflow);
            return work();
        }

        public void Execute(QsQaGate2Decision decision, QsQaGuardedWorkflow workflow, Action work)
        {
            if (decision == null) throw new ArgumentNullException("decision");
            if (work == null) throw new ArgumentNullException("work");

            decision.DemandAllowed(workflow);
            work();
        }
    }

    public sealed class QsQaGate2
    {
        public QsQaGate2Decision Evaluate(
            IEnumerable<QsModelElementSnapshot> elements,
            QsQaRuleProfile profile,
            IEnumerable<QsQaWaiver> waivers,
            DateTime nowUtc)
        {
            if (elements == null) throw new ArgumentNullException("elements");
            if (profile == null) throw new ArgumentNullException("profile");
            if (nowUtc.Kind != DateTimeKind.Utc) nowUtc = nowUtc.ToUniversalTime();

            var materialized = elements.ToList();
            if (materialized.Any(x => x == null)) throw new ArgumentException("Element collection contains null.", "elements");

            var findings = Analyze(materialized, profile);
            var waiverList = (waivers ?? Enumerable.Empty<QsQaWaiver>()).ToList();
            if (waiverList.Any(x => x == null)) throw new ArgumentException("Waiver collection contains null.", "waivers");

            var active = new List<QsQaFinding>();
            var waived = new List<QsQaFinding>();
            foreach (var finding in findings)
            {
                if (waiverList.Any(x => x.Applies(finding, nowUtc))) waived.Add(finding);
                else active.Add(finding);
            }

            var blocked = active.Any(x => Rank(x.Severity) >= Rank(profile.BlockingThreshold));
            var warnings = active.Any(x => x.Severity == QsQaSeverity.Warning);
            var status = blocked ? QsQaGateStatus.Blocked : warnings ? QsQaGateStatus.PassWithWarnings : QsQaGateStatus.Pass;
            return new QsQaGate2Decision(
                status,
                new ReadOnlyCollection<QsQaFinding>(active),
                new ReadOnlyCollection<QsQaFinding>(waived));
        }

        private static IReadOnlyList<QsQaFinding> Analyze(IReadOnlyList<QsModelElementSnapshot> elements, QsQaRuleProfile profile)
        {
            var result = new List<QsQaFinding>();
            var ifcGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in elements)
            {
                AddIf(result, element.Material.Length == 0, profile, "QA2.MISSING_MATERIAL", QsQaSeverity.Error, element.Id, "Material is required.");
                AddIf(result, element.Type.Length == 0, profile, "QA2.MISSING_TYPE", QsQaSeverity.Error, element.Id, "Type assignment is required.");
                AddIf(result, element.Length <= 0d || element.Width <= 0d || element.Height <= 0d, profile, "QA2.INVALID_DIMENSIONS", QsQaSeverity.Error, element.Id, "Positive length, width and height are required.");

                string guid;
                if (element.Properties.TryGetValue("IfcGuid", out guid) && !string.IsNullOrWhiteSpace(guid))
                {
                    AddIf(result, !ifcGuids.Add(guid.Trim()), profile, "QA2.DUPLICATE_IFC_GUID", QsQaSeverity.Critical, element.Id, "IFC GUID must be unique.");
                }

                foreach (var pset in profile.RequiredPsets)
                {
                    string value;
                    var key = "IfcPset." + pset;
                    AddIf(result,
                        !element.Properties.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value),
                        profile,
                        "QA2.MISSING_PSET",
                        QsQaSeverity.Error,
                        element.Id,
                        "Required IFC property set is missing: " + pset + ".");
                }

                foreach (var relationship in profile.RequiredRelationships)
                {
                    string value;
                    var key = "IfcRel." + relationship;
                    AddIf(result,
                        !element.Properties.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value),
                        profile,
                        "QA2.MISSING_RELATIONSHIP",
                        QsQaSeverity.Critical,
                        element.Id,
                        "Required IFC relationship is missing: " + relationship + ".");
                }

                string spatialContainer;
                if (element.Storey.Length > 0 && element.Properties.TryGetValue("IfcRel.SpatialContainer", out spatialContainer) && !string.IsNullOrWhiteSpace(spatialContainer))
                {
                    AddIf(result,
                        !string.Equals(element.Storey, spatialContainer.Trim(), StringComparison.OrdinalIgnoreCase),
                        profile,
                        "QA2.SPATIAL_MISMATCH",
                        QsQaSeverity.Critical,
                        element.Id,
                        "Storey and IFC spatial container disagree.");
                }

                string typeAssignment;
                if (element.Type.Length > 0 && element.Properties.TryGetValue("IfcRel.TypeAssignment", out typeAssignment) && !string.IsNullOrWhiteSpace(typeAssignment))
                {
                    AddIf(result,
                        !string.Equals(element.Type, typeAssignment.Trim(), StringComparison.OrdinalIgnoreCase),
                        profile,
                        "QA2.TYPE_ASSIGNMENT_MISMATCH",
                        QsQaSeverity.Critical,
                        element.Id,
                        "Element type and IFC type assignment disagree.");
                }
            }

            return new ReadOnlyCollection<QsQaFinding>(result
                .OrderBy(x => x.ElementId, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase)
                .ToList());
        }

        private static void AddIf(List<QsQaFinding> result, bool condition, QsQaRuleProfile profile, string ruleId, QsQaSeverity fallback, string elementId, string message)
        {
            if (condition) result.Add(new QsQaFinding(ruleId, profile.SeverityFor(ruleId, fallback), elementId, message));
        }

        private static int Rank(QsQaSeverity severity)
        {
            switch (severity)
            {
                case QsQaSeverity.Info: return 0;
                case QsQaSeverity.Warning: return 1;
                case QsQaSeverity.Error: return 2;
                case QsQaSeverity.Critical: return 3;
                default: throw new ArgumentOutOfRangeException("severity");
            }
        }
    }
}
