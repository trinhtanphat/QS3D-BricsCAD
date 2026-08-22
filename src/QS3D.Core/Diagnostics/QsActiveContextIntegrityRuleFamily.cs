namespace QS3D.Core.Diagnostics
{
    public static class QsActiveContextIntegrityRuleFamily
    {
        public static QsRuleProfile Profile { get; } = new QsRuleProfile(
            "QSC.ACTIVE-CONTEXT.INTEGRITY.V1",
            new[]
            {
                new QsRuleDefinition("QSC.ACTIVE.FLOOR.AMBIGUOUS", "AMBIGUOUS_ACTIVE_FLOOR", HealthSeverity.Error, "Active floor must resolve to exactly one canonical floor identity."),
                new QsRuleDefinition("QSC.ACTIVE.FLOOR.INVALID", "INVALID_ACTIVE_FLOOR", HealthSeverity.Warning, "Project working context should reference an existing active floor."),
                new QsRuleDefinition("QSC.ACTIVE.FLOOR.NON_CANONICAL", "ACTIVE_FLOOR_NON_CANONICAL", HealthSeverity.Error, "ActiveFloorId must exactly match the canonical floor identity."),
                new QsRuleDefinition("QSC.ACTIVE.ZONE.AMBIGUOUS", "AMBIGUOUS_ACTIVE_ZONE", HealthSeverity.Error, "Active zone must resolve to exactly one canonical zone identity."),
                new QsRuleDefinition("QSC.ACTIVE.ZONE.INVALID", "INVALID_ACTIVE_ZONE", HealthSeverity.Warning, "Project working context should reference an existing active zone."),
                new QsRuleDefinition("QSC.ACTIVE.ZONE.NON_CANONICAL", "ACTIVE_ZONE_NON_CANONICAL", HealthSeverity.Error, "ActiveZoneId must exactly match the canonical zone identity.")
            });
    }
}
