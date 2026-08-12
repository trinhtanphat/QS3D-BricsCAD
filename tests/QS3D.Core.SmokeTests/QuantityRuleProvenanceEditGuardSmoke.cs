using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Rules;
using QS3D.Core.Selection;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantityRuleProvenanceEditGuardSmoke
    {
        internal static void Run()
        {
            ReservedNamespaceIsReadOnly();
            GenericEditorsRejectBeforeMutation();
            NearbyUserPropertyRemainsEditable();
        }

        private static void ReservedNamespaceIsReadOnly()
        {
            if (SemanticPropertyEditPolicy.IsEditablePropertyKey("Rule:Manual"))
                throw new Exception("Quantity-rule provenance namespace remained generically editable.");
            if (SemanticPropertyEditPolicy.IsEditablePropertyKey(" rule:Manual "))
                throw new Exception("Quantity-rule provenance namespace guard was not canonical/case-insensitive.");
            if (!SemanticPropertyEditPolicy.IsEditablePropertyKey("RuleFactor"))
                throw new Exception("Quantity-rule provenance guard overblocked a nearby user-defined property key.");
        }

        private static void GenericEditorsRejectBeforeMutation()
        {
            var project = new ProjectState("rule-provenance-edit-guard", "Rule provenance edit guard");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.SetQuantity("Manual", 42d);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var beforeVersion = project.ChangeVersion;
            var beforeProjectUpdated = project.UpdatedUtc;
            var beforeElementUpdated = element.UpdatedUtc;

            ThrowsInvalidOperation(() => new BulkEditService().SetProperty(project, new[] { element }, "Rule:Manual", "spoof"));
            ThrowsInvalidOperation(() => new SemanticSelectionBulkEditService().SetProperty(project, new[] { element.Id }, " rule:Manual ", "spoof"));

            if (element.Properties.ContainsKey("Rule:Manual"))
                throw new Exception("Rejected generic edit created quantity-rule provenance metadata.");
            if (!element.Quantities.TryGetValue("Manual", out var manual) || manual != 42d)
                throw new Exception("Rejected quantity-rule provenance edit mutated the manual quantity.");
            if (element.Dirty != ElementDirtyFlags.None || element.UpdatedUtc != beforeElementUpdated)
                throw new Exception("Rejected quantity-rule provenance edit touched element persistence state.");
            if (project.ChangeVersion != beforeVersion || project.UpdatedUtc != beforeProjectUpdated)
                throw new Exception("Rejected quantity-rule provenance edit touched project persistence state.");

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);
            if (applied != 0)
                throw new Exception("Rule engine unexpectedly reported managed-output work for an unprovenanced manual quantity.");
            if (!element.Quantities.TryGetValue("Manual", out manual) || manual != 42d)
                throw new Exception("Manual quantity was removed after rejected provenance spoofing.");
        }

        private static void NearbyUserPropertyRemainsEditable()
        {
            var project = new ProjectState("rule-user-key", "Rule user key");
            var element = new ProjectElement("E1", ElementCategory.Room);
            element.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(element);

            var changed = new BulkEditService().SetProperty(project, new[] { element }, "RuleFactor", "2");
            if (changed.Count != 1 || !string.Equals(element.Properties["RuleFactor"], "2", StringComparison.Ordinal))
                throw new Exception("Quantity-rule provenance guard blocked a nearby user-defined property key.");
        }

        private static void ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if ((ex.Message ?? string.Empty).IndexOf("provenance", StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception("Reserved Rule: edit failed for an unrelated reason.", ex);
                return;
            }

            throw new Exception("Expected reserved Rule: property edit to fail closed.");
        }
    }

    internal static class QuantityRuleProvenanceEditGuardRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => QuantityRuleProvenanceEditGuardSmoke.Run();
    }
}
