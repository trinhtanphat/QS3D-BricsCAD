using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;

namespace QS3D.Core.SmokeTests
{
    internal static class QsRuleProfileSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            DeterministicDetachedProfile();
            ResolvesStrictlyByHealthCode();
            RejectsAmbiguousOrMalformedProfiles();
        }

        private static void DeterministicDetachedProfile()
        {
            var warning = new QsRuleDefinition("QSC.B", "MISSING_MATERIAL", HealthSeverity.Warning, "Material is required.");
            var error = new QsRuleDefinition("QSC.A", "ORPHAN_HANDLE", HealthSeverity.Error, "Source CAD object is missing.");
            var input = new List<QsRuleDefinition> { warning, error };
            var profile = new QsRuleProfile("PROFILE.DEFAULT", input);

            Equal("PROFILE.DEFAULT", profile.ProfileId, "profile id");
            Equal(2, profile.Rules.Count, "rule count");
            Equal("QSC.A", profile.Rules[0].RuleId, "first deterministic rule");
            Equal("QSC.B", profile.Rules[1].RuleId, "second deterministic rule");

            input.Clear();
            Equal(2, profile.Rules.Count, "profile must be detached from caller collection");

            var mutableView = profile.Rules as IList<QsRuleDefinition>;
            if (mutableView == null)
                throw new InvalidOperationException("Rules must expose a read-only list view.");
            Throws<NotSupportedException>(() => mutableView[0] = warning);
        }

        private static void ResolvesStrictlyByHealthCode()
        {
            var rule = new QsRuleDefinition("QSC.ORPHAN", "ORPHAN_HANDLE", HealthSeverity.Error, "Source CAD object is missing.");
            var profile = new QsRuleProfile("PROFILE.RESOLUTION", new[] { rule });

            var issue = new ModelHealthIssue("ORPHAN_HANDLE", HealthSeverity.Info, "Different runtime message.", "E-1");
            if (!profile.TryResolve(issue, out var resolved) || !ReferenceEquals(rule, resolved))
                throw new InvalidOperationException("Existing health issue code must resolve to its declarative rule metadata.");
            if (!ReferenceEquals(rule, profile.Resolve(issue)))
                throw new InvalidOperationException("Resolve must use the same code-only mapping as TryResolve.");

            var caseVariant = new ModelHealthIssue("orphan_handle", HealthSeverity.Warning, "Case variant code.");
            if (!ReferenceEquals(rule, profile.Resolve(caseVariant)))
                throw new InvalidOperationException("Health issue code identity must use the profile's canonical case-insensitive identity contract.");

            var unmapped = new ModelHealthIssue("UNMAPPED_HEALTH_CODE", HealthSeverity.Error, "Not configured.");
            if (profile.TryResolve(unmapped, out var unexpected) || unexpected != null || profile.Resolve(unmapped) != null)
                throw new InvalidOperationException("Unmapped health issues must remain explicitly unmapped.");

            Throws<ArgumentNullException>(() => profile.Resolve(null!));
        }

        private static void RejectsAmbiguousOrMalformedProfiles()
        {
            var first = new QsRuleDefinition("QSC.ONE", "CODE_ONE", HealthSeverity.Info, "First rule.");
            var duplicateId = new QsRuleDefinition("qsc.one", "CODE_TWO", HealthSeverity.Warning, "Duplicate id.");
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE.DUP-ID", new[] { first, duplicateId }));

            var duplicateCode = new QsRuleDefinition("QSC.TWO", "code_one", HealthSeverity.Error, "Duplicate health code.");
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE.DUP-CODE", new[] { first, duplicateCode }));

            Throws<ArgumentNullException>(() => new QsRuleProfile("PROFILE.NULL", null!));
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE.NULL-RULE", new QsRuleDefinition[1]));
            Throws<ArgumentException>(() => new QsRuleProfile(" ", new[] { first }));
            Throws<ArgumentException>(() => new QsRuleProfile(" PROFILE.PADDED ", new[] { first }));
            Throws<ArgumentException>(() => new QsRuleProfile("PROFILE BAD", new[] { first }));

            Throws<ArgumentException>(() => new QsRuleDefinition(" ", "CODE", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition(" QSC.PADDED ", "CODE", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.PADDED", " CODE ", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD ID", "CODE", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD", "CODE/BAD", HealthSeverity.Info, "Explanation."));
            Throws<ArgumentOutOfRangeException>(() => new QsRuleDefinition("QSC.BAD", "CODE", (HealthSeverity)999, "Explanation."));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD", "CODE", HealthSeverity.Info, " "));
            Throws<ArgumentException>(() => new QsRuleDefinition("QSC.BAD", "CODE", HealthSeverity.Info, "Bad\nexplanation"));
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }
    }
}
