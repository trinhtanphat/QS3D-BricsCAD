using System;

namespace QS3D.Core.Features
{
    public enum ParityEvidenceStage
    {
        ReferenceCaptured = 0,
        UiPresent = 1,
        CommandWired = 2,
        SemanticBehaviorPass = 3,
        SaveReopenPass = 4,
        V25V26ParityPass = 5
    }

    public enum ParityApplicability
    {
        Applicable = 0,
        NotApplicableByHostBoundary = 1
    }

    public sealed class ParityFeatureRecord
    {
        public ParityFeatureRecord(
            FeatureId featureId,
            string domain,
            string referencePath,
            string workflowKey,
            ParityApplicability applicability,
            ParityEvidenceStage evidenceStage,
            string? decisionReference = null,
            string? decisionReason = null)
        {
            FeatureId = featureId;
            Domain = Required(domain, nameof(domain));
            ReferencePath = Required(referencePath, nameof(referencePath));
            WorkflowKey = Required(workflowKey, nameof(workflowKey)).ToLowerInvariant();
            Applicability = applicability;
            EvidenceStage = evidenceStage;
            DecisionReference = Optional(decisionReference);
            DecisionReason = Optional(decisionReason);

            if (applicability == ParityApplicability.NotApplicableByHostBoundary &&
                (DecisionReference == null || DecisionReason == null))
            {
                throw new ArgumentException("Host-boundary N/A requires decision reference and reason.");
            }
        }

        public FeatureId FeatureId { get; }
        public string Domain { get; }
        public string ReferencePath { get; }
        public string WorkflowKey { get; }
        public ParityApplicability Applicability { get; }
        public ParityEvidenceStage EvidenceStage { get; }
        public string? DecisionReference { get; }
        public string? DecisionReason { get; }
        public bool IsApplicable => Applicability == ParityApplicability.Applicable;

        private static string Required(string value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(name + " cannot be blank.", name);
            return value.Trim();
        }

        private static string? Optional(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
    }
}
