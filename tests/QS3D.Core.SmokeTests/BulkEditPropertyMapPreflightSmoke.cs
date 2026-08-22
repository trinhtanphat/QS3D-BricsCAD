using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class BulkEditPropertyMapPreflightSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            SetPropertyRejectsMalformedPendingMapAtomically();
            MultiplyRejectsMalformedPendingMapAtomically();
            SetPropertyNoOpPreservesMalformedLegacyMap();
            MultiplyNoOpPreservesMalformedLegacyMap();
            CanonicalPendingMapsStillMutate();
        }

        private static void SetPropertyRejectsMalformedPendingMapAtomically()
        {
            var project = NewProject("set-atomic", out var first, out var second);
            first.Properties["Scale"] = "1";
            second.Properties["Scale"] = "1";
            second.Properties[" LegacyKey "] = "legacy";
            MarkClean(first, second);
            var beforeVersion = project.ChangeVersion;

            RequireInvalidOperation(
                () => new BulkEditService().SetProperty(project, new[] { first, second }, "Scale", "2"),
                "non-canonical property key",
                "string bulk mutation must reject a malformed pending property map");

            RequireValue(first, "Scale", "1", "string atomic rejection first target");
            RequireValue(second, "Scale", "1", "string atomic rejection second target");
            RequireValue(second, " LegacyKey ", "legacy", "string atomic rejection legacy key");
            RequireUnchanged(project, beforeVersion, first, second, "string atomic rejection");
        }

        private static void MultiplyRejectsMalformedPendingMapAtomically()
        {
            var project = NewProject("multiply-atomic", out var first, out var second);
            first.Properties["Scale"] = "2";
            second.Properties["Scale"] = "3";
            second.Properties[" "] = "legacy";
            MarkClean(first, second);
            var beforeVersion = project.ChangeVersion;

            RequireInvalidOperation(
                () => new BulkEditService().MultiplyNumericProperty(project, new[] { first, second }, "Scale", 2d),
                "empty property key",
                "numeric bulk mutation must reject a malformed pending property map");

            RequireValue(first, "Scale", "2", "numeric atomic rejection first target");
            RequireValue(second, "Scale", "3", "numeric atomic rejection second target");
            RequireValue(second, " ", "legacy", "numeric atomic rejection legacy key");
            RequireUnchanged(project, beforeVersion, first, second, "numeric atomic rejection");
        }

        private static void SetPropertyNoOpPreservesMalformedLegacyMap()
        {
            var project = NewProject("set-noop", out var element, out _);
            element.Properties["Scale"] = "2";
            element.Properties[" LegacyKey "] = "legacy";
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().SetProperty(project, new[] { element }, "Scale", "2");

            if (changed.Count != 0)
                throw new InvalidOperationException("Exact string bulk no-op must not report a changed element.");
            RequireValue(element, " LegacyKey ", "legacy", "string no-op legacy key");
            RequireUnchanged(project, beforeVersion, element, null, "string no-op");
        }

        private static void MultiplyNoOpPreservesMalformedLegacyMap()
        {
            var project = NewProject("multiply-noop", out var element, out _);
            element.Properties["Scale"] = "2";
            element.Properties[" LegacyKey "] = "legacy";
            element.MarkClean(ElementDirtyFlags.All);
            var beforeVersion = project.ChangeVersion;

            var changed = new BulkEditService().MultiplyNumericProperty(project, new[] { element }, "Scale", 1d);

            if (changed.Count != 0)
                throw new InvalidOperationException("Exact numeric bulk no-op must not report a changed element.");
            RequireValue(element, "Scale", "2", "numeric no-op value");
            RequireValue(element, " LegacyKey ", "legacy", "numeric no-op legacy key");
            RequireUnchanged(project, beforeVersion, element, null, "numeric no-op");
        }

        private static void CanonicalPendingMapsStillMutate()
        {
            var project = NewProject("canonical", out var first, out var second);
            first.Properties["Scale"] = "2";
            second.Properties["Scale"] = "3";
            MarkClean(first, second);
            var beforeSet = project.ChangeVersion;

            var setChanged = new BulkEditService().SetProperty(project, new[] { first, second }, "Material", "Concrete");
            RequireTwoChanges(setChanged, first.Id, second.Id, "canonical string mutation");
            if (project.ChangeVersion != checked(beforeSet + 1L))
                throw new InvalidOperationException("Canonical string bulk mutation must touch the project exactly once.");

            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
            var beforeMultiply = project.ChangeVersion;
            var numericChanged = new BulkEditService().MultiplyNumericProperty(project, new[] { first, second }, "Scale", 2d);

            RequireTwoChanges(numericChanged, first.Id, second.Id, "canonical numeric mutation");
            RequireValue(first, "Scale", "4", "canonical numeric first target");
            RequireValue(second, "Scale", "6", "canonical numeric second target");
            if (project.ChangeVersion != checked(beforeMultiply + 1L))
                throw new InvalidOperationException("Canonical numeric bulk mutation must touch the project exactly once.");
        }

        private static ProjectState NewProject(string suffix, out ProjectElement first, out ProjectElement second)
        {
            var project = new ProjectState("P-BULK-MAP-" + suffix, "Bulk property-map preflight");
            first = new ProjectElement("E-1", ElementCategory.ArchitecturalWall);
            second = new ProjectElement("E-2", ElementCategory.ArchitecturalWall);
            project.Elements.Add(first);
            project.Elements.Add(second);
            return project;
        }

        private static void MarkClean(ProjectElement first, ProjectElement second)
        {
            first.MarkClean(ElementDirtyFlags.All);
            second.MarkClean(ElementDirtyFlags.All);
        }

        private static void RequireInvalidOperation(Action action, string messageFragment, string label)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(messageFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new InvalidOperationException(label + " returned the wrong diagnostic: " + ex.Message);
                return;
            }

            throw new InvalidOperationException(label + ".");
        }

        private static void RequireValue(ProjectElement element, string key, string expected, string label)
        {
            if (!element.Properties.TryGetValue(key, out var actual) || !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException(label + " did not retain the expected property value.");
        }

        private static void RequireUnchanged(ProjectState project, long beforeVersion, ProjectElement first, ProjectElement? second, string label)
        {
            if (project.ChangeVersion != beforeVersion)
                throw new InvalidOperationException(label + " must not change the project revision.");
            if (first.Dirty != ElementDirtyFlags.None)
                throw new InvalidOperationException(label + " dirtied the first target.");
            if (second != null && second.Dirty != ElementDirtyFlags.None)
                throw new InvalidOperationException(label + " dirtied the second target.");
        }

        private static void RequireTwoChanges(System.Collections.Generic.IReadOnlyList<string> changed, string firstId, string secondId, string label)
        {
            var foundFirst = false;
            var foundSecond = false;
            foreach (var id in changed)
            {
                if (string.Equals(id, firstId, StringComparison.Ordinal)) foundFirst = true;
                if (string.Equals(id, secondId, StringComparison.Ordinal)) foundSecond = true;
            }
            if (changed.Count != 2 || !foundFirst || !foundSecond)
                throw new InvalidOperationException(label + " did not report both changed targets.");
        }
    }
}
