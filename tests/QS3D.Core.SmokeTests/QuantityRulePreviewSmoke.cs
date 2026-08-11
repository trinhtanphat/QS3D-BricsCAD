using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRulePreviewSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            PreviewIsReadOnlyAndClassifiesChanges();
            ProvenanceOnlyStaleOutputIsRemoved();
            StaleElementPreviewFailsBeforeMutation();
            ChangeVersionInvalidatesEquivalentPreview();
            ChangedElementApplyAdvancesProjectRevisionOnce();
            NoChangeElementApplyIsSideEffectFree();
            ProjectPreviewAppliesAtomicallyFromFreshState();
            HealthGuardedProjectApplyReturnsRegressionDiff();
            ForeignElementInstanceFailsClosed();
        }

        private static void PreviewIsReadOnlyAndClassifiesChanges()
        {
            var project = Fixture();
            var element = project.FindElement("E1")!;
            element.Quantities["OldManaged"] = 9d;
            element.Properties["Rule:OldManaged"] = "old@1";
            var originalUpdated = element.UpdatedUtc;
            var originalVersion = project.ChangeVersion;

            var service = new QuantityRulePreviewService();
            var preview = service.PreviewElement(project, element);

            True(preview.HasChanges);
            Equal(originalVersion, preview.SourceChangeVersion);
            Equal(2, preview.Changes.Count);
            Equal(QuantityRulePreviewChangeKind.Added, preview.Changes.Single(x => x.OutputName == "Cost").Kind);
            Equal(QuantityRulePreviewChangeKind.Removed, preview.Changes.Single(x => x.OutputName == "OldManaged").Kind);
            True(!element.Quantities.ContainsKey("Cost"));
            Near(9d, element.Quantities["OldManaged"]);
            Equal("old@1", element.Properties["Rule:OldManaged"]);
            Equal(originalUpdated, element.UpdatedUtc);
            Equal(originalVersion, project.ChangeVersion);
        }

        private static void ProvenanceOnlyStaleOutputIsRemoved()
        {
            var project = Fixture();
            var element = project.FindElement("E1")!;
            element.Properties["Rule:Ghost"] = "old@1";
            var preview = new QuantityRulePreviewService().PreviewElement(project, element);
            var ghost = preview.Changes.Single(x => x.OutputName == "Ghost");
            Equal(QuantityRulePreviewChangeKind.Removed, ghost.Kind);
            True(!ghost.BeforeValue.HasValue && !ghost.AfterValue.HasValue);
            Equal("old@1", ghost.BeforeProvenance);
            Equal(string.Empty, ghost.AfterProvenance);
        }

        private static void StaleElementPreviewFailsBeforeMutation()
        {
            var project = Fixture();
            var element = project.FindElement("E1")!;
            var service = new QuantityRulePreviewService();
            var preview = service.PreviewElement(project, element);

            element.SetProperty("LengthM", "8");
            var before = element.Quantities.Count;
            Throws<InvalidOperationException>(() => service.ApplyElement(project, element, preview));
            Equal(before, element.Quantities.Count);
            True(!element.Quantities.ContainsKey("Cost"));
        }

        private static void ChangeVersionInvalidatesEquivalentPreview()
        {
            var project = Fixture();
            var service = new QuantityRulePreviewService();
            var preview = service.PreviewProject(project);
            var oldVersion = preview.SourceChangeVersion;
            project.Touch();
            True(project.ChangeVersion > oldVersion);
            Throws<InvalidOperationException>(() => service.ApplyProject(project, preview));
            True(!project.FindElement("E1")!.Quantities.ContainsKey("Cost"));
        }

        private static void ChangedElementApplyAdvancesProjectRevisionOnce()
        {
            var project = Fixture();
            var element = project.FindElement("E1")!;
            var service = new QuantityRulePreviewService();
            var preview = service.PreviewElement(project, element);
            var beforeVersion = project.ChangeVersion;

            var applied = service.ApplyElement(project, element, preview);

            True(applied >= 1);
            Equal(beforeVersion + 1L, project.ChangeVersion);
            Near(6d, element.Quantities["Cost"]);
            Equal("cost@1", element.Properties["Rule:Cost"]);
        }

        private static void NoChangeElementApplyIsSideEffectFree()
        {
            var project = Fixture();
            var element = project.FindElement("E1")!;
            element.Quantities["Cost"] = 6d;
            element.Properties["Rule:Cost"] = "cost@1";
            var service = new QuantityRulePreviewService();
            var preview = service.PreviewElement(project, element);
            True(!preview.HasChanges);
            var beforeVersion = project.ChangeVersion;
            var beforeUpdated = element.UpdatedUtc;

            var applied = service.ApplyElement(project, element, preview);

            Equal(0, applied);
            Equal(beforeVersion, project.ChangeVersion);
            Equal(beforeUpdated, element.UpdatedUtc);
            Near(6d, element.Quantities["Cost"]);
            Equal("cost@1", element.Properties["Rule:Cost"]);
        }

        private static void ProjectPreviewAppliesAtomicallyFromFreshState()
        {
            var project = Fixture();
            var second = new ProjectElement("E2", ElementCategory.Beam, "FAM", "F", "Z");
            second.Properties["LengthM"] = "4";
            second.Properties["Rate"] = "3";
            project.Elements.Add(second);

            var service = new QuantityRulePreviewService();
            var preview = service.PreviewProject(project);
            Equal(2, preview.ChangedElementCount);
            Equal(2, preview.ChangeCount);
            var beforeVersion = project.ChangeVersion;

            var applied = service.ApplyProject(project, preview);
            True(applied >= 2);
            Equal(beforeVersion + 1L, project.ChangeVersion);
            Near(6d, project.FindElement("E1")!.Quantities["Cost"]);
            Near(12d, project.FindElement("E2")!.Quantities["Cost"]);
            Equal("cost@1", project.FindElement("E1")!.Properties["Rule:Cost"]);
            Equal("cost@1", project.FindElement("E2")!.Properties["Rule:Cost"]);
        }

        private static void HealthGuardedProjectApplyReturnsRegressionDiff()
        {
            var project = Fixture();
            var service = new QuantityRulePreviewService();
            var preview = service.PreviewProject(project);
            var beforeVersion = project.ChangeVersion;
            var result = service.ApplyProjectWithHealthGuard(project, preview);
            True(result.AppliedOperationCount >= 1);
            Equal(beforeVersion + 1L, project.ChangeVersion);
            Equal(0, result.HealthDiff.NewErrorCount);
            Near(6d, project.FindElement("E1")!.Quantities["Cost"]);
        }

        private static void ForeignElementInstanceFailsClosed()
        {
            var project = Fixture();
            var foreign = new ProjectElement("E1", ElementCategory.Beam, "FAM", "F", "Z");
            Throws<InvalidOperationException>(() => new QuantityRulePreviewService().PreviewElement(project, foreign));
        }

        private static ProjectState Fixture()
        {
            var project = new ProjectState("P-RULE-PREVIEW", "Rule Preview");
            project.Zones.Add(new ZoneDefinition("Z", "Zone"));
            project.Floors.Add(new FloorDefinition("F", "Floor", 0d));
            project.Families.Add(new ProjectFamily("FAM", "Beam", ElementCategory.Beam));
            project.QuantityRules.Add(new QuantityRule("cost", ElementCategory.Beam, "Cost", "LengthM*Rate", "1"));

            var element = new ProjectElement("E1", ElementCategory.Beam, "FAM", "F", "Z");
            element.Properties["LengthM"] = "2";
            element.Properties["Rate"] = "3";
            project.Elements.Add(element);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }
    }
}
