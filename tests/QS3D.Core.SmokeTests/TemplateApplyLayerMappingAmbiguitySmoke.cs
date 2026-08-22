using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateApplyLayerMappingAmbiguitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState("P-TEMPLATE-MAPPING-AMBIGUITY", "Template mapping ambiguity");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-WALL"] = ElementCategory.ArchitecturalWall.ToString();
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A WALL"] = ElementCategory.ArchitecturalWall.ToString();

            var beforeVersion = project.ChangeVersion;
            var beforeAuditCount = project.AuditEvents.Count;
            var profile = new TemplateProfile("T-MAPPING-AMBIGUITY", "Mapping ambiguity");

            var failedClosed = false;
            try
            {
                new TemplateProfileStore().Apply(project, profile);
            }
            catch (InvalidOperationException ex)
            {
                failedClosed = ex.Message.IndexOf("ambiguous normalized layer mappings", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            if (!failedClosed)
                throw new Exception("Template apply must fail closed when persisted project layer mappings collapse to the same recognition pattern.");
            if (project.ChangeVersion != beforeVersion)
                throw new Exception("Template mapping ambiguity preflight must fail before project revision mutation.");
            if (project.AuditEvents.Count != beforeAuditCount)
                throw new Exception("Template mapping ambiguity preflight must fail before audit history mutation.");
            if (!project.Metadata.ContainsKey(TemplateProfileStore.LayerMappingPrefix + "A-WALL") ||
                !project.Metadata.ContainsKey(TemplateProfileStore.LayerMappingPrefix + "A WALL"))
                throw new Exception("Template mapping ambiguity preflight must not rewrite persisted project mappings.");
        }
    }
}
