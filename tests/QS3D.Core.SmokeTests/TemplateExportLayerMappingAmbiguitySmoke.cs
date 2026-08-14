using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateExportLayerMappingAmbiguitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            AmbiguousMappingsFailClosed();
            CanonicalMappingStillExports();
        }

        private static void AmbiguousMappingsFailClosed()
        {
            var project = new ProjectState("P-TEMPLATE-EXPORT-AMBIGUITY", "Template export ambiguity");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-WALL"] = ElementCategory.ArchitecturalWall.ToString();
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A WALL"] = ElementCategory.ArchitecturalWall.ToString();
            var beforeVersion = project.ChangeVersion;
            var beforeAuditCount = project.AuditEvents.Count;

            var failedClosed = false;
            try
            {
                new TemplateProfileStore().ExportProject(project, "T-AMBIGUOUS", "Ambiguous");
            }
            catch (InvalidOperationException ex)
            {
                failedClosed = ex.Message.IndexOf("ambiguous normalized layer mappings", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!failedClosed)
                throw new Exception("Template export must fail closed instead of collapsing ambiguous project recognition mappings.");
            if (project.ChangeVersion != beforeVersion || project.AuditEvents.Count != beforeAuditCount)
                throw new Exception("Template export mapping validation must remain read-only.");
        }

        private static void CanonicalMappingStillExports()
        {
            var project = new ProjectState("P-TEMPLATE-EXPORT-CANONICAL", "Template export canonical");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-WALL"] = ElementCategory.ArchitecturalWall.ToString();

            var profile = new TemplateProfileStore().ExportProject(project, "T-CANONICAL", "Canonical");
            if (profile.LayerMappings.Count != 1 ||
                !profile.LayerMappings.TryGetValue("A-WALL", out var category) ||
                !string.Equals(category, ElementCategory.ArchitecturalWall.ToString(), StringComparison.Ordinal))
                throw new Exception("Canonical project recognition mapping must continue to export unchanged.");
        }
    }
}
