using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeRemapLevelReferenceSmoke
    {
        internal static void Run()
        {
            ImportAsNewRemapsPortableLevelReferences();
            InvalidTopOnlyLevelRelationRollsBack();
        }

        private static void ImportAsNewRemapsPortableLevelReferences()
        {
            var target = BuildProject("target");
            var source = BuildProject("source");
            source.DrawingFingerprint = "source-fingerprint";
            var sourceElement = source.Elements.Single();
            sourceElement.Properties[ProjectFloorService.BottomLevelIdKey] = "L0";
            sourceElement.Properties[ProjectFloorService.TopLevelIdKey] = "L1";
            sourceElement.Properties[ProjectFloorService.BottomLevelOffsetKey] = "0.15";
            sourceElement.Properties[ProjectFloorService.TopLevelOffsetKey] = "-0.05";
            sourceElement.SourceHandles.Add("AB12");
            sourceElement.DrawingFingerprint = source.DrawingFingerprint;

            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeRemapAppendImporter.Plan(target, json);
            True(plan.CanImport);
            True(plan.ReferenceRewriteCount >= 5);

            var result = ProjectInterchangeRemapAppendImporter.Import(target, json);
            Equal(1, result.ZonesAdded);
            Equal(2, result.FloorsAdded);
            Equal(1, result.FamiliesAdded);
            Equal(1, result.ElementsAdded);
            Equal(1, result.SourceHandlesDiscarded);
            True(result.ReferencesRewritten >= 5);

            var original = target.FindElement("E");
            True(original != null);
            Equal("F", original!.FamilyId);
            var imported = target.FindElement("E-import");
            True(imported != null);
            Equal("F-import", imported!.FamilyId);
            Equal("L0-import", imported.FloorId);
            Equal("Z-import", imported.ZoneId);
            Equal("L0-import", imported.Properties[ProjectFloorService.BottomLevelIdKey]);
            Equal("L1-import", imported.Properties[ProjectFloorService.TopLevelIdKey]);
            Equal("0.15", imported.Properties[ProjectFloorService.BottomLevelOffsetKey]);
            Equal("-0.05", imported.Properties[ProjectFloorService.TopLevelOffsetKey]);
            Equal(0, imported.SourceHandles.Count);
            Equal(string.Empty, imported.DrawingFingerprint);
            Equal(ElementDirtyFlags.All, imported.Dirty);
            True(target.FindFloor("L0") != null);
            True(target.FindFloor("L0-import") != null);
            True(target.FindFloor("L1") != null);
            True(target.FindFloor("L1-import") != null);
        }

        private static void InvalidTopOnlyLevelRelationRollsBack()
        {
            var target = BuildProject("target");
            var source = BuildProject("source");
            source.Elements.Single().Properties[ProjectFloorService.TopLevelIdKey] = "L1";
            var json = ProjectInterchangeJsonExporter.Build(source);
            var beforeZones = target.Zones.Count;
            var beforeFloors = target.Floors.Count;
            var beforeFamilies = target.Families.Count;
            var beforeElements = target.Elements.Count;
            var beforeVersion = target.ChangeVersion;

            MustFail(() => ProjectInterchangeRemapAppendImporter.Import(target, json));
            Equal(beforeZones, target.Zones.Count);
            Equal(beforeFloors, target.Floors.Count);
            Equal(beforeFamilies, target.Families.Count);
            Equal(beforeElements, target.Elements.Count);
            Equal(beforeVersion, target.ChangeVersion);
            True(target.FindElement("E-import") == null);
            True(target.FindFloor("L1-import") == null);
        }

        private static ProjectState BuildProject(string id)
        {
            var project = new ProjectState(id, "Project " + id);
            project.Zones.Add(new ZoneDefinition("Z", "Main Zone"));
            project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.Floors.Add(new FloorDefinition("L1", "Level 1", 3.6d));
            project.Families.Add(new ProjectFamily("F", "Beam 300x500", ElementCategory.Beam));
            project.Elements.Add(new ProjectElement("E", ElementCategory.Beam, "F", "L0", "Z"));
            return project;
        }

        private static void MustFail(Action action)
        {
            try { action(); } catch (InvalidOperationException) { return; }
            throw new Exception("Expected InvalidOperationException.");
        }
        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
    }

    internal static class ProjectInterchangeRemapLevelReferenceSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapLevelReferenceSmoke.Run();
    }
}
