using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleEngineOwnershipRegressionSmoke
    {
        public static void Run()
        {
            CanonicalElementAppliesNormally();
            DetachedSameIdFailsBeforeMutation();
            CrossProjectElementFailsClosed();
            IndependentCanonicalProjectElementAppliesNormally();
        }

        private static void CanonicalElementAppliesNormally()
        {
            var project = NewBeamProject("p-canonical", "B1");
            var element = project.FindElement("B1") ?? throw new InvalidOperationException("Missing canonical beam.");
            project.QuantityRules.Add(new QuantityRule("beam-double", ElementCategory.Beam, "Computed", "LengthM*2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            Near(4d, element.Quantities["Computed"]);
            Equal("beam-double@1", element.Properties["Rule:Computed"]);
        }

        private static void DetachedSameIdFailsBeforeMutation()
        {
            var project = NewBeamProject("p-detached", "B1");
            project.QuantityRules.Add(new QuantityRule("beam-double", ElementCategory.Beam, "Computed", "LengthM*2", "1"));

            var detached = NewBeamElement("B1");
            detached.Quantities["OldManaged"] = 7d;
            detached.Properties["Rule:OldManaged"] = "old@1";

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, detached));

            True(!detached.Quantities.ContainsKey("Computed"));
            True(!detached.Properties.ContainsKey("Rule:Computed"));
            Near(7d, detached.Quantities["OldManaged"]);
            Equal("old@1", detached.Properties["Rule:OldManaged"]);
        }

        private static void CrossProjectElementFailsClosed()
        {
            var project = NewBeamProject("p-a", "B1");
            var otherProject = NewBeamProject("p-b", "B1");
            project.QuantityRules.Add(new QuantityRule("beam-double", ElementCategory.Beam, "Computed", "LengthM*2", "1"));
            var otherElement = otherProject.FindElement("B1") ?? throw new InvalidOperationException("Missing other-project beam.");

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, otherElement));

            True(!otherElement.Quantities.ContainsKey("Computed"));
            True(!otherElement.Properties.ContainsKey("Rule:Computed"));
        }

        private static void IndependentCanonicalProjectElementAppliesNormally()
        {
            var project = NewBeamProject("p-copy", "B1");
            var element = project.FindElement("B1") ?? throw new InvalidOperationException("Missing copied-project canonical beam.");
            project.QuantityRules.Add(new QuantityRule("beam-double", ElementCategory.Beam, "Computed", "LengthM*2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);

            Equal(1, applied);
            Near(4d, element.Quantities["Computed"]);
            Equal("beam-double@1", element.Properties["Rule:Computed"]);
        }

        private static ProjectState NewBeamProject(string projectId, string elementId)
        {
            var project = new ProjectState(projectId, "Quantity ownership");
            project.Zones.Add(new ZoneDefinition("z", "Z"));
            project.Floors.Add(new FloorDefinition("f", "F", 0d));
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam);
            project.Families.Add(family);
            project.Elements.Add(NewBeamElement(elementId));
            return project;
        }

        private static ProjectElement NewBeamElement(string elementId)
        {
            var element = new ProjectElement(elementId, ElementCategory.Beam, "beam", "f", "z");
            element.Properties["LengthM"] = "2";
            return element;
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
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }
            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class QuantityRuleEngineOwnershipRegressionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            QuantityRuleEngineOwnershipRegressionSmoke.Run();
        }
    }
}
