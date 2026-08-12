using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateEmptyBqLayoutApplySmoke
    {
        internal static void Run()
        {
            EmptyLayoutClearsExistingProjectPreference();
            NonEmptyLayoutStillReplacesProjectPreference();
            EmptyLayoutWithoutExistingPreferenceRemainsAbsent();
        }

        private static void EmptyLayoutClearsExistingProjectPreference()
        {
            var project = new ProjectState("p-template-empty-columns", "Template empty columns");
            project.Metadata[TemplateProfileStore.VisibleBqColumnsKey] = "Code|Description|Quantity";
            var profile = new TemplateProfile("T-EMPTY", "Empty BQ layout");

            new TemplateProfileStore().Apply(project, profile);

            if (project.Metadata.ContainsKey(TemplateProfileStore.VisibleBqColumnsKey))
                throw new InvalidOperationException("Applying an empty template BQ layout preserved stale project column metadata.");
        }

        private static void NonEmptyLayoutStillReplacesProjectPreference()
        {
            var project = new ProjectState("p-template-columns-replace", "Template columns replace");
            project.Metadata[TemplateProfileStore.VisibleBqColumnsKey] = "LegacyA|LegacyB";
            var profile = new TemplateProfile("T-COLUMNS", "BQ layout");
            profile.VisibleBqColumns.Add("Code");
            profile.VisibleBqColumns.Add("Quantity");

            new TemplateProfileStore().Apply(project, profile);

            if (!project.Metadata.TryGetValue(TemplateProfileStore.VisibleBqColumnsKey, out var actual) ||
                !string.Equals(actual, "Code|Quantity", StringComparison.Ordinal))
                throw new InvalidOperationException("Applying a nonempty template BQ layout no longer replaces the project preference.");
        }

        private static void EmptyLayoutWithoutExistingPreferenceRemainsAbsent()
        {
            var project = new ProjectState("p-template-no-columns", "Template no columns");
            var profile = new TemplateProfile("T-NONE", "No BQ layout");

            new TemplateProfileStore().Apply(project, profile);

            if (project.Metadata.ContainsKey(TemplateProfileStore.VisibleBqColumnsKey))
                throw new InvalidOperationException("Applying an empty template BQ layout created unexpected project column metadata.");
        }
    }

    internal static class TemplateEmptyBqLayoutApplySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateEmptyBqLayoutApplySmoke.Run();
    }
}
