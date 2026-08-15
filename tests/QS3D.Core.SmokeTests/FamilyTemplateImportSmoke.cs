using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Rules;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class FamilyTemplateImportSmoke
    {
        public static void Run()
        {
            ImportUsesCategoryNameAndLocalIds();
            RepeatedImportIsTrueNoOp();
        }

        private static void ImportUsesCategoryNameAndLocalIds()
        {
            var project = new ProjectState("P-FAMILY-IMPORT", "Family template import smoke");
            var localBeam = new ProjectFamily("local-beam", "D300x500", ElementCategory.Beam);
            localBeam.Properties["WidthM"] = "0.300";
            localBeam.Properties["HeightM"] = "0.500";
            localBeam.Properties["Material"] = "Bê tông";
            localBeam.Properties["CustomKeep"] = "LOCAL";
            project.Families.Add(localBeam);

            var inherited = new ProjectElement("beam-inherited", ElementCategory.Beam, localBeam.Id, string.Empty, string.Empty);
            inherited.Properties["WidthM"] = "0.300";
            inherited.Properties["HeightM"] = "0.500";
            project.Elements.Add(inherited);

            var overridden = new ProjectElement("beam-override", ElementCategory.Beam, localBeam.Id, string.Empty, string.Empty);
            overridden.Properties["WidthM"] = "0.325";
            overridden.Properties["HeightM"] = "0.500";
            project.Elements.Add(overridden);

            var profile = new TemplateProfile("USER-FAMILY-TEMPLATE", "User Family Template");
            var sourceBeam = new ProjectFamily("serialized-beam-id", "D300x500", ElementCategory.Beam);
            sourceBeam.Properties["WidthM"] = "0.350";
            sourceBeam.Properties["HeightM"] = "0.500";
            sourceBeam.Properties["Material"] = "Bê tông";
            sourceBeam.Properties["BQCode"] = "BEAM-350X500";
            profile.Families.Add(sourceBeam);

            var sourceSlab = new ProjectFamily("serialized-slab-id", "S120", ElementCategory.Slab);
            sourceSlab.Properties["ThicknessM"] = "0.120";
            sourceSlab.Properties["Material"] = "Bê tông";
            profile.Families.Add(sourceSlab);

            profile.QuantityRules.Add(new QuantityRule("RULE-SOURCE-ONLY", ElementCategory.Beam, "IgnoredQty", "1", "1"));
            profile.LayerMappings["A-BEAM"] = ElementCategory.Beam.ToString();
            profile.VisibleBqColumns.Add("IgnoredQty");

            var result = FamilyTemplateImportService.Apply(project, profile);
            Equal(1, result.FamiliesAdded, "Import should add only the missing Slab Family.");
            Equal(1, result.FamiliesUpdated, "Import should update the existing same Category + Name Beam.");
            Equal(2, project.Families.Count, "Import produced an unexpected Family count.");

            var beam = project.Families.Single(x => x.Category == ElementCategory.Beam && x.Name == "D300x500");
            Equal("local-beam", beam.Id, "Import must preserve the project-local id for a matched Family.");
            Property(beam, "WidthM", "0.350");
            Property(beam, "CustomKeep", "LOCAL");
            Property(beam, "BQCode", "BEAM-350X500");
            Equal("0.350", inherited.Properties["WidthM"], "Inherited Family property should update through ProjectFamilyService semantics.");
            Equal("0.325", overridden.Properties["WidthM"], "Instance override should be preserved during Family import.");

            var slab = project.Families.Single(x => x.Category == ElementCategory.Slab && x.Name == "S120");
            if (string.Equals(slab.Id, sourceSlab.Id, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Cross-project Family import must allocate a fresh local id instead of trusting the serialized source id.");
            if (!slab.Id.StartsWith("family-", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Imported Family did not receive the expected project-local id form.");

            if (project.FindQuantityRule("RULE-SOURCE-ONLY") != null)
                throw new Exception("Family-only import must not apply template quantity rules.");
            if (project.Metadata.ContainsKey(TemplateProfileStore.LayerMappingPrefix + "A-BEAM"))
                throw new Exception("Family-only import must not apply template layer mappings.");
            if (project.Metadata.ContainsKey(TemplateProfileStore.VisibleBqColumnsKey))
                throw new Exception("Family-only import must not apply template BQ layout.");
        }

        private static void RepeatedImportIsTrueNoOp()
        {
            var project = new ProjectState("P-FAMILY-IMPORT-NOOP", "Family template no-op smoke");
            var profile = new TemplateProfile("USER-FAMILY-NOOP", "No-op Family Template");
            var source = new ProjectFamily("foreign-id", "T200", ElementCategory.ArchitecturalWall);
            source.Properties["ThicknessM"] = "0.200";
            source.Properties["Material"] = "Gạch";
            profile.Families.Add(source);

            var first = FamilyTemplateImportService.Apply(project, profile);
            Equal(1, first.FamiliesAdded, "First import should add the missing Family.");

            var familyId = project.Families.Single().Id;
            var versionBeforeSecond = project.ChangeVersion;
            var auditsBeforeSecond = project.AuditEvents.Count;
            var updatedBeforeSecond = project.UpdatedUtc;

            var second = FamilyTemplateImportService.Apply(project, profile);
            Equal(0, second.FamiliesAdded, "Second import must not add a duplicate Family.");
            Equal(0, second.FamiliesUpdated, "Second import must not rewrite the existing Family.");
            Equal(0, second.PropertiesApplied, "Second import must not rewrite unchanged properties.");
            Equal(1, project.Families.Count, "Second import changed Family count.");
            Equal(familyId, project.Families.Single().Id, "Second import changed the project-local Family id.");
            Equal(versionBeforeSecond, project.ChangeVersion, "Second import must not bump ChangeVersion.");
            Equal(auditsBeforeSecond, project.AuditEvents.Count, "Second import must not append a no-op audit event.");
            Equal(updatedBeforeSecond, project.UpdatedUtc, "Second import must not change UpdatedUtc.");
        }

        private static void Property(ProjectFamily family, string key, string expected)
        {
            if (!family.Properties.TryGetValue(key, out var actual))
                throw new Exception("Missing Family property " + family.Name + "/" + key + ".");
            Equal(expected, actual, "Unexpected Family property " + family.Name + "/" + key + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }
    }
}
