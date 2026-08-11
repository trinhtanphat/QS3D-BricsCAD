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
            AuditOverflowRestoresWholeTemplateApply();
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

            var project = AtVersion(source, long.MaxValue - 1L);
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
            Equal(long.MaxValue - 1L, project.ChangeVersion, "Failed template apply did not restore the project version.");
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
