using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleProvenanceCanonicalReadSmoke
    {
        internal static void Run()
        {
            PaddedStaleProvenanceFailsBeforeMutation();
            BlankProvenanceFailsBeforeMutation();
            PaddedActiveProvenanceFailsBeforeRuleApply();
            CanonicalStaleProvenanceStillCleansExactly();
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
            element.Properties["Rule:   "] = "bad@1";

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
