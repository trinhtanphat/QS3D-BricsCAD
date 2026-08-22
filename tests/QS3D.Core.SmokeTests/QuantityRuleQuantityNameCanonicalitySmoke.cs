using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleQuantityNameCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            RejectsPaddedQuantityNameBeforeRuleMutation();
            RejectsBlankQuantityNameBeforeRuleMutation();
            CanonicalQuantityNameStillEvaluates();
        }

        private static void RejectsPaddedQuantityNameBeforeRuleMutation()
        {
            var setup = Create();
            setup.Element.Quantities[" LengthM "] = 3d;
            AssertRejectedWithoutMutation(setup, "padded quantity name");
        }

        private static void RejectsBlankQuantityNameBeforeRuleMutation()
        {
            var setup = Create();
            setup.Element.Quantities[string.Empty] = 3d;
            AssertRejectedWithoutMutation(setup, "blank quantity name");
        }

        private static void CanonicalQuantityNameStillEvaluates()
        {
            var setup = Create();
            setup.Element.Quantities["LengthM"] = 3d;

            var applied = new QuantityRuleEngine().ApplyMatching(setup.Project, setup.Element);

            if (applied != 1)
                throw new InvalidOperationException("Expected exactly one canonical quantity rule application, got " + applied + ".");
            if (!setup.Element.Quantities.TryGetValue("Result", out var result) || result != 6d)
                throw new InvalidOperationException("Canonical quantity variable did not evaluate to the expected rule result.");
            if (!setup.Element.Properties.TryGetValue("Rule:Result", out var provenance) || !string.Equals(provenance, "R1@1", StringComparison.Ordinal))
                throw new InvalidOperationException("Canonical quantity rule provenance was not recorded.");
        }

        private static void AssertRejectedWithoutMutation(Setup setup, string label)
        {
            var beforeUpdatedUtc = setup.Element.UpdatedUtc;
            var beforeDirty = setup.Element.Dirty;
            var beforeCount = setup.Element.Quantities.Count;

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(setup.Project, setup.Element));

            if (setup.Element.Quantities.Count != beforeCount || setup.Element.Quantities.ContainsKey("Result"))
                throw new InvalidOperationException("Rejected " + label + " changed quantity output state.");
            if (setup.Element.Properties.ContainsKey("Rule:Result"))
                throw new InvalidOperationException("Rejected " + label + " wrote quantity-rule provenance.");
            if (setup.Element.UpdatedUtc != beforeUpdatedUtc || setup.Element.Dirty != beforeDirty)
                throw new InvalidOperationException("Rejected " + label + " changed element freshness state.");
        }

        private static Setup Create()
        {
            var project = new ProjectState("P-RULE-QUANTITY-NAME", "Quantity rule quantity name");
            var element = new ProjectElement("E1", ElementCategory.Beam);
            project.Elements.Add(element);
            project.QuantityRules.Add(new QuantityRule("R1", ElementCategory.Beam, "Result", "LengthM*2", "1"));
            return new Setup(project, element);
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
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }

        private sealed class Setup
        {
            public Setup(ProjectState project, ProjectElement element)
            {
                Project = project;
                Element = element;
            }

            public ProjectState Project { get; }
            public ProjectElement Element { get; }
        }
    }
}
