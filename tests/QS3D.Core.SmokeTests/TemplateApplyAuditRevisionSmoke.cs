using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateApplyAuditRevisionSmoke
    {
        internal static void Run()
        {
            ApplyUsesOneAuditOwnedRevision();
        }

        private static void ApplyUsesOneAuditOwnedRevision()
        {
            var project = new ProjectState("template-audit-revision", "Template Audit Revision");
            var profile = new TemplateProfile("TPL-1", "Template One");
            profile.Families.Add(new ProjectFamily("FAM-1", "Beam Family", ElementCategory.Beam));

            var beforeVersion = project.ChangeVersion;
            var beforeAuditCount = project.AuditEvents.Count;
            var result = new TemplateProfileStore().Apply(project, profile);

            Equal(1, result.FamiliesAdded);
            Equal(0, result.FamiliesUpdated);
            Equal(beforeVersion + 2L, project.ChangeVersion);
            Equal(beforeAuditCount + 1, project.AuditEvents.Count);
            Equal("template.apply", project.AuditEvents[project.AuditEvents.Count - 1].Action);

            var family = project.FindFamily("FAM-1");
            True(family != null);
            Equal("Beam Family", family!.Name);
            Equal(ElementCategory.Beam, family.Category);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }

    internal static class TemplateApplyAuditRevisionSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => TemplateApplyAuditRevisionSmoke.Run();
    }
}
