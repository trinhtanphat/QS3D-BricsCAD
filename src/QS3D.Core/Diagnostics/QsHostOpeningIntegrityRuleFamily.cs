namespace QS3D.Core.Diagnostics
{
    /// <summary>
    /// Declarative QS metadata for host/opening integrity findings already emitted by
    /// ModelHealthService. This class does not evaluate project state or duplicate
    /// Semantic Health predicates; resolution remains code-only through QsRuleProfile.
    /// </summary>
    public static class QsHostOpeningIntegrityRuleFamily
    {
        public static QsRuleProfile Profile { get; } = new QsRuleProfile(
            "QSC.HOST-OPENING.INTEGRITY.V1",
            new[]
            {
                new QsRuleDefinition(
                    "QSC.HOST.AMBIGUOUS",
                    "AMBIGUOUS_HOST",
                    HealthSeverity.Error,
                    "Door or opening host reference must resolve to exactly one semantic element."),
                new QsRuleDefinition(
                    "QSC.HOST.CATEGORY",
                    "INVALID_HOST_CATEGORY",
                    HealthSeverity.Error,
                    "Door or opening host must match the host category required by its semantic contract."),
                new QsRuleDefinition(
                    "QSC.HOST.INVALID",
                    "INVALID_HOST",
                    HealthSeverity.Error,
                    "Door or opening host reference must resolve to an existing semantic element."),
                new QsRuleDefinition(
                    "QSC.HOST.MISSING",
                    "MISSING_HOST",
                    HealthSeverity.Error,
                    "Door or opening must declare the host reference required by its semantic contract before host-dependent workflows can proceed."),
                new QsRuleDefinition(
                    "QSC.HOST.NON_CANONICAL",
                    "HOST_REFERENCE_NON_CANONICAL",
                    HealthSeverity.Error,
                    "The declared host reference must exactly match the canonical semantic id of its required host.")
            });
    }
}
