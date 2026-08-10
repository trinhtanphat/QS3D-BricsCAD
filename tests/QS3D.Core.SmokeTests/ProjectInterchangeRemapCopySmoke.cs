using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeRemapCopySmoke
    {
        public static void Run()
        {
            DeterministicCopyPreservesTargetAndRemapsReferences();
            UnknownReferenceLikePropertyFailsClosed();
            NamespaceValidationFailsClosed();
        }

        private static void DeterministicCopyPreservesTargetAndRemapsReferences()
        {
            var source = BuildSource();
            var json = ProjectInterchangeJsonExporter.Build(source);
            var target = BuildTargetWithCollisions();

            var firstPlan = ProjectInterchangeRemapCopyImporter.Plan(target, json, "ARCH-A");
            var secondPlan = ProjectInterchangeRemapCopyImporter.Plan(target, json, "ARCH-A");
            Equal(2, firstPlan.ZonesToAdd);
            Equal(2, firstPlan.FloorsToAdd);
            Equal(2, firstPlan.FamiliesToAdd);
            Equal(2, firstPlan.ElementsToAdd);
            Equal(1, firstPlan.SourceHandlesToDiscard);
            Equal(2, firstPlan.PropertyReferencesRemapped);
            Equal(firstPlan.Mappings.Count, secondPlan.Mappings.Count);
            for (var i = 0; i < firstPlan.Mappings.Count; i++)
            {
                Equal(firstPlan.Mappings[i].SourceId, secondPlan.Mappings[i].SourceId);
                Equal(firstPlan.Mappings[i].TargetId, secondPlan.Mappings[i].TargetId);
            }

            var sourceWallMap = firstPlan.Mappings.Single(x => x.Kind == InterchangeRemapIdentityKind.Element && x.SourceId == "W1");
            var sourceOpeningMap = firstPlan.Mappings.Single(x => x.Kind == InterchangeRemapIdentityKind.Element && x.SourceId == "O1");
            var sourceBottomMap = firstPlan.Mappings.Single(x => x.Kind == InterchangeRemapIdentityKind.Floor && x.SourceId == "L1");
            var sourceTopMap = firstPlan.Mappings.Single(x => x.Kind == InterchangeRemapIdentityKind.Floor && x.SourceId == "L2");

            var result = ProjectInterchangeRemapCopyImporter.Import(target, json, "ARCH-A");
            Equal(2, result.ElementsAdded);
            Equal(1, result.SourceHandlesDiscarded);
            Equal("Target Wall", target.FindElement("W1")!.Properties["Mark"]);
            Equal("Target Ground", target.FindFloor("L1")!.Name);

            var importedWall = target.FindElement(sourceWallMap.TargetId) ?? throw new Exception("Remapped wall was not added.");
            var importedOpening = target.FindElement(sourceOpeningMap.TargetId) ?? throw new Exception("Remapped opening was not added.");
            Equal(0, importedWall.SourceHandles.Count);
            Equal(string.Empty, importedWall.DrawingFingerprint);
            Equal(sourceBottomMap.TargetId, importedWall.Properties[ProjectFloorService.BottomLevelIdKey]);
            Equal(sourceTopMap.TargetId, importedWall.Properties[ProjectFloorService.TopLevelIdKey]);
            Equal(1, importedOpening.DependsOn.Count);
            Equal(sourceWallMap.TargetId, importedOpening.DependsOn[0]);
            Equal(ElementDirtyFlags.All, importedWall.Dirty);
            Equal(ElementDirtyFlags.All, importedOpening.Dirty);
            Equal(ProjectInterchangeRemapCopyImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
            Equal("ARCH-A", target.Metadata[ProjectInterchangeRemapCopyImporter.LastNamespaceKey]);
        }

        private static void UnknownReferenceLikePropertyFailsClosed()
        {
            var source = BuildSource();
            source.FindElement("O1")!.Properties["CustomHostId"] = "W1";
            var json = ProjectInterchangeJsonExporter.Build(source);
            MustFail(
                () => ProjectInterchangeRemapCopyImporter.Plan(BuildTargetWithCollisions(), json, "ARCH-B"),
                "Unknown reference-like property keys that resolve to source identities must fail closed.");
        }

        private static void NamespaceValidationFailsClosed()
        {
            var json = ProjectInterchangeJsonExporter.Build(BuildSource());
            MustFail(
                () => ProjectInterchangeRemapCopyImporter.Plan(BuildTargetWithCollisions(), json, "bad namespace"),
                "Remap namespaces with spaces must fail closed.");
        }

        private static ProjectState BuildSource()
        {
            var project = new ProjectState("SRC-PROJECT", "Source")
            {
                DrawingFingerprint = "source-drawing"
            };
            project.Zones.Add(new ZoneDefinition("ZA", "Zone A"));
            project.Zones.Add(new ZoneDefinition("ZB", "Zone B"));
            project.Floors.Add(new FloorDefinition("L1", "Ground", 0d));
            project.Floors.Add(new FloorDefinition("L2", "Level 2", 3.6d));
            project.Families.Add(new ProjectFamily("FW", "Wall 200", ElementCategory.ArchitecturalWall));
            project.Families.Add(new ProjectFamily("FO", "Opening", ElementCategory.WallOpening));

            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, "FW", "L1", "ZA")
            {
                DrawingFingerprint = "source-drawing"
            };
            wall.SourceHandles.Add("AB12");
            wall.Properties["Mark"] = "Source Wall";
            wall.Properties[ProjectFloorService.BottomLevelIdKey] = "L1";
            wall.Properties[ProjectFloorService.TopLevelIdKey] = "L2";
            wall.MarkClean(ElementDirtyFlags.All);

            var opening = new ProjectElement("O1", ElementCategory.WallOpening, "FO", "L1", "ZB")
            {
                DrawingFingerprint = "source-drawing"
            };
            opening.DependsOn.Add("W1");
            opening.Properties["WidthM"] = "0.9";
            opening.Properties["HeightM"] = "2.1";
            opening.MarkClean(ElementDirtyFlags.All);

            project.Elements.Add(wall);
            project.Elements.Add(opening);
            return project;
        }

        private static ProjectState BuildTargetWithCollisions()
        {
            var project = new ProjectState("TARGET-PROJECT", "Target");
            project.Zones.Add(new ZoneDefinition("ZA", "Target Zone A"));
            project.Zones.Add(new ZoneDefinition("ZB", "Target Zone B"));
            project.Floors.Add(new FloorDefinition("L1", "Target Ground", 0d));
            project.Floors.Add(new FloorDefinition("L2", "Target Level 2", 4d));
            project.Families.Add(new ProjectFamily("FW", "Target Wall 300", ElementCategory.ArchitecturalWall));
            project.Families.Add(new ProjectFamily("FO", "Target Opening", ElementCategory.WallOpening));
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, "FW", "L1", "ZA");
            wall.Properties["Mark"] = "Target Wall";
            project.Elements.Add(wall);
            project.ActiveZoneId = "ZA";
            project.ActiveFloorId = "L1";
            return project;
        }

        private static void MustFail(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            catch (ArgumentException)
            {
                return;
            }
            throw new Exception(message);
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected '" + expected + "' but got '" + actual + "'.");
        }
    }
}
