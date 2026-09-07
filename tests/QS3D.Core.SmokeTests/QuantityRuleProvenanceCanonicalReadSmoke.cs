using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleProvenanceCanonicalReadSmoke
    {
        internal static void Run()
        {
            LeadingWhitespacePrefixFailsBeforeMutation();
            PaddedStaleProvenanceFailsBeforeMutation();
            BlankProvenanceFailsBeforeMutation();
            PaddedActiveProvenanceFailsBeforeRuleApply();
            CanonicalStaleProvenanceStillCleansExactly();
        }

        private static void LeadingWhitespacePrefixFailsBeforeMutation()
        {
            var project = NewProject();
            var element = project.FindElement("B1")!;
            element.Quantities["Ghost"] = 7d;
            InjectLegacyElementProperty(element, " Rule:Ghost", "old@1");
            var beforeUpdatedUtc = element.UpdatedUtc;
            var beforeDirty = element.Dirty;

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Near(7d, element.Quantities["Ghost"]);
            Equal("old@1", element.Properties[" Rule:Ghost"]);
            True(!element.Properties.ContainsKey("Rule:Ghost"));
            if (element.UpdatedUtc != beforeUpdatedUtc || element.Dirty != beforeDirty)
                throw new InvalidOperationException("Rejected leading-whitespace rule provenance changed element freshness state.");
        }

        private static void PaddedStaleProvenanceFailsBeforeMutation()
        {
            var project = NewProject();
            var element = project.FindElement("B1")!;
            element.Quantities["Ghost"] = 7d;
            element.Properties["Rule: Ghost"] = "old@1";

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Near(7d, element.Quantities["Ghost"]);
            Equal("old@1", element.Properties["Rule: Ghost"]);
            True(!element.Properties.ContainsKey("Rule:Ghost"));
        }

        private static void BlankProvenanceFailsBeforeMutation()
        {
            var project = NewProject();
            var element = project.FindElement("B1")!;
            element.Quantities["Keep"] = 3d;
            InjectLegacyElementProperty(element, "Rule:   ", "bad@1");

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Near(3d, element.Quantities["Keep"]);
            Equal("bad@1", element.Properties["Rule:   "]);
        }

        private static void PaddedActiveProvenanceFailsBeforeRuleApply()
        {
            var project = NewProject();
            var element = project.FindElement("B1")!;
            project.QuantityRules.Add(new QuantityRule("ghost", ElementCategory.Beam, "Ghost", "LengthM*2", "1"));
            element.Quantities["Ghost"] = 5d;
            element.Properties["Rule: Ghost"] = "legacy@1";

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));

            Near(5d, element.Quantities["Ghost"]);
            Equal("legacy@1", element.Properties["Rule: Ghost"]);
            True(!element.Properties.ContainsKey("Rule:Ghost"));
        }

        private static void CanonicalStaleProvenanceStillCleansExactly()
        {
            var project = NewProject();
            var element = project.FindElement("B1")!;
            element.Quantities["Ghost"] = 7d;
            element.Properties["Rule:Ghost"] = "old@1";

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            True(!element.Quantities.ContainsKey("Ghost"));
            True(!element.Properties.ContainsKey("Rule:Ghost"));
        }

        private static ProjectState NewProject()
        {
            var project = new ProjectState("P-RULE-PROV", "Rule provenance canonical read");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("BEAM", "Beam", ElementCategory.Beam));
            var element = new ProjectElement("B1", ElementCategory.Beam, "BEAM", "F", "Z");
            element.Properties["LengthM"] = "2";
            project.Elements.Add(element);
            return project;
        }

        private static void InjectLegacyElementProperty(ProjectElement element, string key, string value)
        {
            var field = typeof(ProjectElement).GetField("_properties", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Legacy Element fixture could not locate the property backing dictionary.");
            var backing = field.GetValue(element) as Dictionary<string, string>
                ?? throw new InvalidOperationException("Legacy Element fixture property backing dictionary had an unexpected type.");
            backing[key] = value;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9d)
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("Expected true.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class QuantityRuleProvenanceCanonicalReadSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRuleProvenanceCanonicalReadSmoke.Run();
    }
}
