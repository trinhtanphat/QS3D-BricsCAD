namespace QS3D.Core.Diagnostics
{
    public static class QsSemanticReadinessRuleFamily
    {
        public const string ProfileId = "QSC.SEMANTIC.READINESS";

        public static QsRuleProfile CreateProfile()
        {
            return new QsRuleProfile(
                ProfileId,
                new[]
                {
                    Rule("QSC.SEMANTIC.FAMILY.AMBIGUOUS", "AMBIGUOUS_FAMILY", HealthSeverity.Error, "Family reference must resolve to one canonical family identity."),
                    Rule("QSC.SEMANTIC.FAMILY.CATEGORY_MISMATCH", "FAMILY_CATEGORY_MISMATCH", HealthSeverity.Warning, "Element category should match its referenced family category."),
                    Rule("QSC.SEMANTIC.FAMILY.MISSING", "MISSING_FAMILY", HealthSeverity.Error, "Element must reference an existing family."),
                    Rule("QSC.SEMANTIC.FAMILY.NON_CANONICAL", "FAMILY_REFERENCE_NON_CANONICAL", HealthSeverity.Error, "Family reference must use the exact canonical family identity."),
                    Rule("QSC.SEMANTIC.FLOOR.AMBIGUOUS", "AMBIGUOUS_FLOOR", HealthSeverity.Error, "Floor reference must resolve to one canonical floor identity."),
                    Rule("QSC.SEMANTIC.FLOOR.MISSING", "MISSING_FLOOR", HealthSeverity.Warning, "Element should reference an existing floor."),
                    Rule("QSC.SEMANTIC.FLOOR.NON_CANONICAL", "FLOOR_REFERENCE_NON_CANONICAL", HealthSeverity.Error, "Floor reference must use the exact canonical floor identity."),
                    Rule("QSC.SEMANTIC.ZONE.AMBIGUOUS", "AMBIGUOUS_ZONE", HealthSeverity.Error, "Zone reference must resolve to one canonical zone identity."),
                    Rule("QSC.SEMANTIC.ZONE.MISSING", "MISSING_ZONE", HealthSeverity.Warning, "Element should reference an existing zone."),
                    Rule("QSC.SEMANTIC.ZONE.NON_CANONICAL", "ZONE_REFERENCE_NON_CANONICAL", HealthSeverity.Error, "Zone reference must use the exact canonical zone identity."),
                    Rule("QSC.SEMANTIC.MATERIAL.MISSING", "MISSING_MATERIAL", HealthSeverity.Warning, "Material-required elements should resolve material evidence from canonical element or family state."),
                    Rule("QSC.SEMANTIC.DIMENSION.MISSING", "MISSING_DIMENSION", HealthSeverity.Error, "Required measurement dimensions must be present."),
                    Rule("QSC.SEMANTIC.DIMENSION.INVALID", "INVALID_DIMENSION", HealthSeverity.Error, "Declared measurement dimensions must be finite and strictly positive.")
                });
        }

        private static QsRuleDefinition Rule(string ruleId, string healthIssueCode, HealthSeverity severity, string explanation)
        {
            return new QsRuleDefinition(ruleId, healthIssueCode, severity, explanation);
        }
    }
}
