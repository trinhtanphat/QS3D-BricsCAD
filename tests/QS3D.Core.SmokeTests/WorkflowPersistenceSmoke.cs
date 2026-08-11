using System;
using System.IO;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Recognition;
using QS3D.Core.Rules;
using QS3D.Core.Services;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkflowPersistenceSmoke
    {
        public static void Run()
        {
            SchemaV2MigratesToV3();
            RuleAuditRoundTrip();
            RuleDrivenRegeneration();
            RuleDependenciesAreDeterministic();
            RuleCyclesAreAtomic();
            TemplateRoundTripApply();
            ProjectLayerMappingWins();
            ProjectLayerMappingOverridesFallback();
        }

        private static void SchemaV2MigratesToV3()
        {
            var directory = TempDirectory("schema-v3"); var path = Path.Combine(directory, "legacy.qsdb");
            try
            {
                File.WriteAllText(path, "<qs3d schema=\"2\" projectId=\"p\" name=\"Legacy\" updatedUtc=\"2026-08-10T00:00:00Z\"><metadata/><zones/><floors/><families/><elements/></qs3d>");
                var project = new QsdbProjectStore().Load(path);
                Equal(3, project.SchemaVersion); Equal(0, project.QuantityRules.Count); Equal(0, project.AuditEvents.Count); Equal("2", project.Metadata["QS3D.SchemaMigratedFrom"]);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RuleAuditRoundTrip()
        {
            var directory = TempDirectory("rule-audit"); var path = Path.Combine(directory, "project.qsdb");
            try
            {
                var project = NewBeamProject();
                project.QuantityRules.Add(new QuantityRule("beam-cost", ElementCategory.Beam, "CostIndex", "NetVolumeM3*120", "1"));
                AuditTrail.ForProject(project).Record("test", "B1", "roundtrip", "smoke", "corr-1");
                var store = new QsdbProjectStore(); store.Save(project, path); var loaded = store.Load(path);
                var rule = loaded.QuantityRules.Single(); Equal("beam-cost", rule.Id); Equal("CostIndex", rule.OutputName); Equal("NetVolumeM3*120", rule.Expression);
                var audit = loaded.AuditEvents.Single(); Equal("test", audit.Action); Equal("B1", audit.ElementId); Equal("smoke", audit.Actor); Equal("corr-1", audit.CorrelationId);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void RuleDrivenRegeneration()
        {
            Throws<ArgumentOutOfRangeException>(() => new QuantityRule("invalid-category", (ElementCategory)999, "Bad", "1", "1"));

            var project = NewBeamProject();
            project.QuantityRules.Add(new QuantityRule("beam-double", ElementCategory.Beam, "DoubleVolume", "NetVolumeM3*2", "1"));
            var element = project.Elements.Single(); element.MarkDirty(ElementDirtyFlags.All);
            var count = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault()).RegenerateDirty(project);
            True(count > 0); Near(0.3d, element.Quantities["NetVolumeM3"]); Near(0.6d, element.Quantities["DoubleVolume"]); Equal(ElementDirtyFlags.Geometry, element.Dirty);
        }

        private static void RuleDependenciesAreDeterministic()
        {
            var project = NewBeamProject();
            var element = project.Elements.Single();
            element.Quantities["NetVolumeM3"] = 0.3d;
            element.Quantities["AdjustedVolume"] = 999d;
            element.Quantities["FinalCost"] = 99900d;
            element.Properties["Rule:AdjustedVolume"] = "old-adjust@0";
            element.Properties["Rule:FinalCost"] = "old-final@0";

            project.QuantityRules.Add(new QuantityRule("a-final", ElementCategory.Beam, "FinalCost", "AdjustedVolume*100", "1"));
            project.QuantityRules.Add(new QuantityRule("z-adjust", ElementCategory.Beam, "AdjustedVolume", "NetVolumeM3*2", "1"));

            var applied = new QuantityRuleEngine().ApplyMatching(project, element);
            Equal(2, applied);
            Near(0.6d, element.Quantities["AdjustedVolume"]);
            Near(60d, element.Quantities["FinalCost"]);
            Equal("z-adjust@1", element.Properties["Rule:AdjustedVolume"]);
            Equal("a-final@1", element.Properties["Rule:FinalCost"]);
        }

        private static void RuleCyclesAreAtomic()
        {
            var project = NewBeamProject();
            var element = project.Elements.Single();
            element.Quantities["A"] = 10d;
            element.Quantities["B"] = 20d;
            element.Properties["Rule:A"] = "old-a@0";
            element.Properties["Rule:B"] = "old-b@0";
            project.QuantityRules.Add(new QuantityRule("rule-a", ElementCategory.Beam, "A", "B+1", "1"));
            project.QuantityRules.Add(new QuantityRule("rule-b", ElementCategory.Beam, "B", "A+1", "1"));

            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));
            Near(10d, element.Quantities["A"]);
            Near(20d, element.Quantities["B"]);
            Equal("old-a@0", element.Properties["Rule:A"]);
            Equal("old-b@0", element.Properties["Rule:B"]);
        }

        private static void TemplateRoundTripApply()
        {
            var directory = TempDirectory("template"); var path = Path.Combine(directory, "company.qstemplate");
            try
            {
                var profile = new TemplateProfile("company", "Company Standard");
                var family = new ProjectFamily("beam-company", "Dầm C30", ElementCategory.Beam); family.Properties["WidthM"] = "0.3"; family.Properties["HeightM"] = "0.6"; family.Properties["Material"] = "C30"; family.Properties["Classification.Code"] = "STR-BEAM"; profile.Families.Add(family);
                profile.QuantityRules.Add(new QuantityRule("beam-factor", ElementCategory.Beam, "TenderVolumeM3", "NetVolumeM3*1.03", "2026.1"));
                profile.LayerMappings["A-BEAM"] = ElementCategory.Beam.ToString(); profile.VisibleBqColumns.Add("Floor"); profile.VisibleBqColumns.Add("NetConcreteM3");
                var store = new TemplateProfileStore(); store.Save(profile, path); store.Save(profile, path); True(File.Exists(path + ".bak"));
                var loaded = store.Load(path); Equal("C30", loaded.Families.Single().Properties["Material"]); Equal("STR-BEAM", loaded.Families.Single().Properties["Classification.Code"]);
                var project = new ProjectState("p", "Project"); var result = store.Apply(project, loaded);
                Equal(1, result.FamiliesAdded); Equal(1, result.RulesAdded); Equal(1, result.LayerMappingsApplied); Equal(ElementCategory.Beam.ToString(), project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-BEAM"]); True(project.Metadata[TemplateProfileStore.VisibleBqColumnsKey].Contains("NetConcreteM3"));
                var exported = store.ExportProject(project, "copy", "Copy"); Equal(1, exported.Families.Count); Equal(1, exported.QuantityRules.Count); Equal(ElementCategory.Beam.ToString(), exported.LayerMappings["A-BEAM"]);
            }
            finally { DeleteDirectory(directory); }
        }

        private static void ProjectLayerMappingWins()
        {
            var project = new ProjectState("p", "Recognition"); project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-MISC"] = ElementCategory.Door.ToString();
            var snapshot = new EntitySnapshot("AA", "BlockReference", "A-MISC");
            var result = new ProjectRecognitionService().Suggest(project, snapshot);
            True(result.TopCandidate != null); Equal(ElementCategory.Door, result.TopCandidate!.Category); Near(.99d, result.TopCandidate.Confidence); True(!result.RequiresReview);
        }

        private static void ProjectLayerMappingOverridesFallback()
        {
            var project = new ProjectState("p", "Recognition override");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-BEAM"] = ElementCategory.Door.ToString();
            var snapshot = new EntitySnapshot("AB", "Line", "A-BEAM");
            snapshot.Metadata["Text"] = "Dầm chính";

            var service = new ProjectRecognitionService();
            var result = service.Suggest(project, snapshot);
            True(result.TopCandidate != null);
            Equal(ElementCategory.Beam, result.TopCandidate!.Category);
            True(!result.RequiresReview);

            var batch = service.SuggestBatch(project, new[] { snapshot });
            Equal(1, batch.AutoAccepted.Count);
            Equal(0, batch.ReviewRequired.Count);
        }

        private static ProjectState NewBeamProject()
        {
            var project = new ProjectState("p", "Beam"); project.Zones.Add(new ZoneDefinition("z", "Z")); project.Floors.Add(new FloorDefinition("f", "F", 0d));
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam); family.Properties["WidthM"] = "0.3"; family.Properties["HeightM"] = "0.5"; project.Families.Add(family);
            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, "f", "z"); element.Properties["LengthM"] = "2"; element.Properties["WidthM"] = "0.3"; element.Properties["HeightM"] = "0.5"; project.Elements.Add(element); return project;
        }

        private static string TempDirectory(string name) { var directory = Path.Combine(Path.GetTempPath(), "qs3d-" + name + "-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory); return directory; }
        private static void DeleteDirectory(string directory) { try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { } }
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
