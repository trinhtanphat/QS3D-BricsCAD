using System;
using System.IO;
using System.Linq;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Rules;
using QS3D.Core.Services;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class WorkflowSafetySmoke
    {
        public static void Run()
        {
            TemplateApplyPreflightIsAtomic();
            TemplateFamilyDefaultsRespectOverrides();
            TemplateParserRejectsDuplicateMappings();
            ProjectSnapshotRestoresTemplateRollbackState();
            RuleOutputsAreCleanedWhenRulesChange();
            RuleEvaluationIsAtomic();
            DuplicateRuleOutputsAreRejected();
        }

        private static void TemplateApplyPreflightIsAtomic()
        {
            var project = NewBeamProject();
            project.QuantityRules.Add(new QuantityRule("existing", ElementCategory.Beam, "Conflict", "1", "1"));
            var profile = new TemplateProfile("conflict", "Conflict");
            var family = new ProjectFamily("beam", "Beam Updated", ElementCategory.Beam);
            family.Properties["WidthM"] = "0.4";
            family.Properties["HeightM"] = "0.6";
            profile.Families.Add(family);
            profile.QuantityRules.Add(new QuantityRule("new-rule", ElementCategory.Beam, "Conflict", "2", "1"));

            Throws<InvalidOperationException>(() => new TemplateProfileStore().Apply(project, profile));
            Equal("Beam", project.FindFamily("beam")!.Name);
            Equal("0.3", project.FindFamily("beam")!.Properties["WidthM"]);
            Equal(1, project.QuantityRules.Count);
            Equal(0, project.AuditEvents.Count);
        }

        private static void TemplateFamilyDefaultsRespectOverrides()
        {
            var project = NewBeamProject();
            var element = project.Elements.Single();
            element.Properties["HeightM"] = "0.9";
            element.MarkClean(ElementDirtyFlags.All);

            var profile = new TemplateProfile("family-update", "Family Update");
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam);
            family.Properties["WidthM"] = "0.4";
            family.Properties["HeightM"] = "0.6";
            profile.Families.Add(family);

            var result = new TemplateProfileStore().Apply(project, profile);
            Equal(1, result.FamiliesUpdated);
            Equal(1, result.AffectedElements);
            Equal("0.4", element.Properties["WidthM"]);
            Equal("0.9", element.Properties["HeightM"]);
            True((element.Dirty & ElementDirtyFlags.Properties) != 0);
        }

        private static void TemplateParserRejectsDuplicateMappings()
        {
            var directory = TempDirectory("template-duplicate-map");
            var path = Path.Combine(directory, "duplicate.qstemplate");
            try
            {
                File.WriteAllText(path,
                    "<qs3dTemplate schema=\"1\" id=\"x\" name=\"X\"><families/><rules/><layerMappings>" +
                    "<map pattern=\"A-BEAM\" category=\"Beam\"/><map pattern=\"a-beam\" category=\"Slab\"/>" +
                    "</layerMappings><bqColumns/></qs3dTemplate>");
                Throws<InvalidDataException>(() => new TemplateProfileStore().Load(path));
            }
            finally { DeleteDirectory(directory); }
        }

        private static void ProjectSnapshotRestoresTemplateRollbackState()
        {
            var project = NewBeamProject();
            project.Name = "Before";
            project.Metadata["Custom"] = "before";
            project.QuantityRules.Add(new QuantityRule("r", ElementCategory.Beam, "RuleQ", "1", "1"));
            project.AuditEvents.Add(new AuditEvent { Action = "before", ElementId = "B1", Detail = "original", Utc = DateTime.UtcNow });
            var element = project.Elements.Single();
            element.Quantities["NetVolumeM3"] = .3d;
            element.MarkClean(ElementDirtyFlags.All);
            var expectedDirty = element.Dirty;
            var expectedUpdated = element.UpdatedUtc;
            var snapshot = ProjectStateSnapshot.Capture(project);

            project.Name = "After";
            project.FindFamily("beam")!.Name = "Mutated";
            project.FindFamily("beam")!.Properties["WidthM"] = "9";
            element.Properties["WidthM"] = "9";
            element.Quantities["NetVolumeM3"] = 99d;
            element.MarkDirty(ElementDirtyFlags.All);
            project.QuantityRules.Clear();
            project.Metadata["Custom"] = "after";
            project.AuditEvents.Clear();
            project.Elements.Add(new ProjectElement("EXTRA", ElementCategory.Room, string.Empty, "f", "z"));

            snapshot.Restore(project);
            Equal("Before", project.Name);
            Equal("Beam", project.FindFamily("beam")!.Name);
            Equal("0.3", project.FindFamily("beam")!.Properties["WidthM"]);
            Equal(1, project.Elements.Count);
            Equal("0.3", project.FindElement("B1")!.Properties["WidthM"]);
            Near(.3d, project.FindElement("B1")!.Quantities["NetVolumeM3"]);
            Equal(expectedDirty, project.FindElement("B1")!.Dirty);
            Equal(expectedUpdated, project.FindElement("B1")!.UpdatedUtc);
            Equal(1, project.QuantityRules.Count);
            Equal("before", project.Metadata["Custom"]);
            Equal(1, project.AuditEvents.Count);
            Equal("before", project.AuditEvents.Single().Action);
        }

        private static void RuleOutputsAreCleanedWhenRulesChange()
        {
            var project = NewBeamProject();
            var element = project.Elements.Single();
            project.QuantityRules.Add(new QuantityRule("r", ElementCategory.Beam, "OldMetric", "NetVolumeM3*2", "1"));
            element.MarkDirty(ElementDirtyFlags.All);
            var engine = new RegenerationEngine(new DependencyGraph(), RegeneratorCatalog.CreateDefault());
            engine.RegenerateDirty(project);
            True(element.Quantities.ContainsKey("OldMetric"));
            True(element.Properties.ContainsKey("Rule:OldMetric"));

            project.QuantityRules.Clear();
            project.QuantityRules.Add(new QuantityRule("r", ElementCategory.Beam, "NewMetric", "NetVolumeM3*3", "2"));
            element.MarkDirty(ElementDirtyFlags.Quantity);
            engine.RegenerateDirty(project);
            True(!element.Quantities.ContainsKey("OldMetric"));
            True(!element.Properties.ContainsKey("Rule:OldMetric"));
            Near(0.9d, element.Quantities["NewMetric"]);
            Equal("r@2", element.Properties["Rule:NewMetric"]);
        }

        private static void RuleEvaluationIsAtomic()
        {
            var project = NewBeamProject();
            var element = project.Elements.Single();
            project.QuantityRules.Add(new QuantityRule("good", ElementCategory.Beam, "First", "1", "1"));
            project.QuantityRules.Add(new QuantityRule("bad", ElementCategory.Beam, "Second", "1/0", "1"));
            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));
            True(!element.Quantities.ContainsKey("First"));
            True(!element.Properties.ContainsKey("Rule:First"));
        }

        private static void DuplicateRuleOutputsAreRejected()
        {
            var project = NewBeamProject();
            var element = project.Elements.Single();
            project.QuantityRules.Add(new QuantityRule("a", ElementCategory.Beam, "Same", "1", "1"));
            project.QuantityRules.Add(new QuantityRule("b", ElementCategory.Beam, "same", "2", "1"));
            Throws<InvalidOperationException>(() => new QuantityRuleEngine().ApplyMatching(project, element));
        }

        private static ProjectState NewBeamProject()
        {
            var project = new ProjectState("workflow-safety", "Workflow Safety");
            project.Zones.Add(new ZoneDefinition("z", "Zone"));
            project.Floors.Add(new FloorDefinition("f", "Floor", 0d));
            project.ActiveZoneId = "z";
            project.ActiveFloorId = "f";
            var family = new ProjectFamily("beam", "Beam", ElementCategory.Beam);
            family.Properties["WidthM"] = "0.3";
            family.Properties["HeightM"] = "0.5";
            project.Families.Add(family);
            var element = new ProjectElement("B1", ElementCategory.Beam, family.Id, "f", "z");
            element.Properties["LengthM"] = "2";
            element.Properties["WidthM"] = "0.3";
            element.Properties["HeightM"] = "0.5";
            project.Elements.Add(element);
            return project;
        }

        private static string TempDirectory(string name)
        {
            var directory = Path.Combine(Path.GetTempPath(), "qs3d-" + name + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static void DeleteDirectory(string directory)
        {
            try { if (Directory.Exists(directory)) Directory.Delete(directory, true); } catch { }
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual);
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
