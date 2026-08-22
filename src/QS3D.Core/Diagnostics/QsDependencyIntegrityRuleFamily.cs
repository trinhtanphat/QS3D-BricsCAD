namespace QS3D.Core.Diagnostics
{
    public static class QsDependencyIntegrityRuleFamily
    {
        public static QsRuleProfile CreateProfile()
        {
            return new QsRuleProfile("QSC.DEPENDENCY.INTEGRITY", new[]
            {
                NewRule("QSC.DEP.01", "DEPENDENCY_ELEMENT_ID_DUPLICATE", "Dependency graph elements must have unique semantic identities."),
                NewRule("QSC.DEP.02", "DEPENDENCY_TARGET_NON_CANONICAL", "Dependency targets must use canonical semantic identities."),
                NewRule("QSC.DEP.03", "DEPENDENCY_TARGET_DUPLICATE", "An element must not repeat the same dependency target."),
                NewRule("QSC.DEP.04", "DEPENDENCY_TARGET_AMBIGUOUS", "Dependency targets must resolve to one semantic element."),
                NewRule("QSC.DEP.05", "DEPENDENCY_TARGET_BLANK", "Dependency targets must not be blank."),
                NewRule("QSC.DEP.06", "DEPENDENCY_TARGET_MISSING", "Dependency targets must exist in the project."),
                NewRule("QSC.DEP.07", "DEPENDENCY_SELF_REFERENCE", "An element must not depend on itself."),
                NewRule("QSC.DEP.08", "DEPENDENCY_CYCLE", "Semantic dependency relations must remain acyclic.")
            });
        }

        private static QsRuleDefinition NewRule(string id, string code, string explanation)
        {
            return new QsRuleDefinition(id, code, HealthSeverity.Error, explanation);
        }
    }
}
