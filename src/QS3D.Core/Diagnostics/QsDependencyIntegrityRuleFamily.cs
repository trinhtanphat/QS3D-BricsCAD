namespace QS3D.Core.Diagnostics
{
    public static class QsDependencyIntegrityRuleFamily
    {
        public static QsRuleProfile CreateProfile()
        {
            return new QsRuleProfile("QSC.DEPENDENCY.INTEGRITY", new[]
            {
                NewRule("QSC.DEP.01", "DEPENDENCY_ELEMENT_ID_DUPLICATE"),
                NewRule("QSC.DEP.02", "DEPENDENCY_TARGET_NON_CANONICAL"),
                NewRule("QSC.DEP.03", "DEPENDENCY_TARGET_DUPLICATE"),
                NewRule("QSC.DEP.04", "DEPENDENCY_TARGET_AMBIGUOUS"),
                NewRule("QSC.DEP.05", "DEPENDENCY_TARGET_BLANK"),
                NewRule("QSC.DEP.06", "DEPENDENCY_TARGET_MISSING"),
                NewRule("QSC.DEP.07", "DEPENDENCY_SELF_REFERENCE"),
                NewRule("QSC.DEP.08", "DEPENDENCY_CYCLE")
            });
        }

        private static QsRuleDefinition NewRule(string id, string code)
        {
            return new QsRuleDefinition(id, code, HealthSeverity.Error, "Dependency integrity finding.");
        }
    }
}
