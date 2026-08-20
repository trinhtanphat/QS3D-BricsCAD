using System;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class TemplateApplyAtomicitySmoke
    {
        public static void Run()
        {
            OversizedProfileRejectedBeforePlanning();
            BelowBoundProfileStillApplies();
            AuditOverflowRestoresWholeTemplateApply();
        }

        private static void OversizedProfileRejectedBeforePlanning()
        {
            var project = new ProjectState("P-TEMPLATE-BOUND", "Template apply bound");
            project.Families.Add(new ProjectFamily("DUP", "Existing A", ElementCategory.ArchitecturalWall));
            project.Families.Add(new ProjectFamily("DUP", "Existing B", ElementCategory.StructuralWall));
            var beforeFamilies = project.Families.Count;
            var beforeAudits = project.AuditEvents.Count;
            var beforeVersion = project.ChangeVersion;
            var beforeUtc = project.UpdatedUtc;

            var profile = new TemplateProfile("T-OVERSIZED", "Oversized template");
            var family = new ProjectFamily("F-OVERSIZED", "Oversized wall", ElementCategory.ArchitecturalWall);
            family.Properties["Payload"] = new string('x', 8 * 1024 * 1024);
            profile.Families.Add(family);

            try
            {
                new TemplateProfileStore().Apply(project, profile);
                throw new Exception("Expected oversized in-memory template apply to fail.");
            }
            catch (InvalidDataException ex)
            {
                if (!ex.Message.Contains("exceeds 8 MiB", StringComparison.Ordinal))
                    throw new Exception("Oversized template apply did not fail through the established size contract: " + ex.Message);
            }

            Equal(beforeFamilies, project.Families.Count, "Oversized template apply changed project families.");
            Equal(beforeAudits, project.AuditEvents.Count, "Oversized template apply appended an audit event.");
            Equal(beforeVersion, project.ChangeVersion, "Oversized template apply changed project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Oversized template apply changed UpdatedUtc.");
        }

        private static void BelowBoundProfileStillApplies()
        {
            var project = new ProjectState("P-TEMPLATE-BOUND-OK", "Template apply bound control");
            var profile = new TemplateProfile("T-BOUND-OK", "Bounded template");
            var family = new ProjectFamily("F-BOUND-OK", "Bounded wall", ElementCategory.ArchitecturalWall);
            family.Properties["WidthM"] = "0.2";
            profile.Families.Add(family);

            var result = new TemplateProfileStore().Apply(project, profile);

            Equal(1, result.FamiliesAdded, "Below-bound template did not add its family.");
            Equal(1, project.Families.Count, "Below-bound template apply produced the wrong family count.");
            Equal("F-BOUND-OK", project.Families[0].Id, "Below-bound template apply added the wrong family.");
            Equal("0.2", project.Families[0].Properties["WidthM"], "Below-bound template apply lost family defaults.");
        }

        private static void AuditOverflowRestoresWholeTemplateApply()
        {
            var source = new ProjectState("P-TEMPLATE-ATOMIC", "Template apply atomicity");
            var family = new ProjectFamily("F-WALL", "Old wall", ElementCategory.ArchitecturalWall);
            family.Properties["WidthM"] = "0.2";
            source.Families.Add(family);
            source.QuantityRules.Add(new QuantityRule("R-WALL", ElementCategory.ArchitecturalWall, "RuleQty", "1", "1"));
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, string.Empty, string.Empty);
            wall.Properties["WidthM"] = "0.2";
            wall.MarkClean(ElementDirtyFlags.All);
            source.Elements.Add(wall);

            var project = AtVersion(source, long.MaxValue);
            var beforeUtc = project.UpdatedUtc;
            var beforeAudits = project.AuditEvents.Count;

            var profile = new TemplateProfile("T-ATOMIC", "Atomic template");
            var updatedFamily = new ProjectFamily("F-WALL", "New wall", ElementCategory.ArchitecturalWall);
            updatedFamily.Properties["WidthM"] = "0.3";
            profile.Families.Add(updatedFamily);
            profile.QuantityRules.Add(new QuantityRule("R-WALL", ElementCategory.ArchitecturalWall, "RuleQty", "2", "2"));
            profile.LayerMappings["A-WALL"] = ElementCategory.ArchitecturalWall.ToString();
            profile.VisibleBqColumns.Add("RuleQty");

            Throws<OverflowException>(() => new TemplateProfileStore().Apply(project, profile));

            family = project.FindFamily("F-WALL") ?? throw new Exception("Template rollback lost the original family.");
            Equal("Old wall", family.Name, "Failed template apply changed family name.");
            Equal("0.2", family.Properties["WidthM"], "Failed template apply changed family defaults.");

            wall = project.FindElement("W1") ?? throw new Exception("Template rollback lost the wall.");
            Equal("0.2", wall.Properties["WidthM"], "Failed template apply propagated family defaults.");
            Equal(ElementDirtyFlags.None, wall.Dirty, "Failed template apply changed element dirty flags.");

            var rule = project.FindQuantityRule("R-WALL") ?? throw new Exception("Template rollback lost the original rule.");
            Equal("1", rule.Expression, "Failed template apply changed the rule expression.");
            Equal("1", rule.Version, "Failed template apply changed the rule version.");

            if (project.Metadata.ContainsKey(TemplateProfileStore.LayerMappingPrefix + "A-WALL"))
                throw new Exception("Failed template apply persisted a layer mapping.");
            if (project.Metadata.ContainsKey(TemplateProfileStore.VisibleBqColumnsKey))
                throw new Exception("Failed template apply persisted visible BQ columns.");
            Equal(beforeAudits, project.AuditEvents.Count, "Failed template apply appended an audit event.");
            Equal(long.MaxValue, project.ChangeVersion, "Failed template apply did not restore the project version.");
            Equal(beforeUtc, project.UpdatedUtc, "Failed template apply did not restore UpdatedUtc.");
        }

        private static ProjectState AtVersion(ProjectState source, long version)
        {
            var path = Path.Combine(Path.GetTempPath(), "qs3d-template-atomicity-" + Guid.NewGuid().ToString("N") + ".qsdb");
            try
            {
                var store = new QsdbProjectStore();
                store.SaveNew(source, path);
                var document = XDocument.Load(path);
                var root = document.Root ?? throw new Exception("QSDB fixture has no root element.");
                root.SetAttributeValue("changeVersion", version.ToString(CultureInfo.InvariantCulture));
                document.Save(path, SaveOptions.DisableFormatting);
                return store.Load(path);
            }
            finally
            {
                TryDelete(path);
                TryDelete(path + ".bak");
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual)) throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
