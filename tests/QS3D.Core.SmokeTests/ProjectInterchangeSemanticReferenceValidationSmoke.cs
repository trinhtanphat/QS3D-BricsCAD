using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeSemanticReferenceValidationSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ExportRejectsMissingRegisteredReference();
            CatalogRejectsNullSemanticCollections();
            ExportRejectsDuplicateSemanticIdentities();
            ValidatorRejectsNullSemanticElementBeforeOrdering();
            ValidatorAndTypedReaderRejectMissingRegisteredReference();
            ValidatorAndTypedReaderRejectPaddedRegisteredReference();
            ValidatorPreservesFreeTextAndCaseInsensitiveReference();
            ValidatorAndTypedReaderRejectInvalidLevelChain();
            MixedFieldMergeRollsBackInvalidLevelComposition();
        }

        private static void ExportRejectsMissingRegisteredReference()
        {
            var project = BaseProject("P-EXPORT-REF");
            var opening = new ProjectElement("E-OPEN", ElementCategory.WallOpening, string.Empty, "A", string.Empty);
            opening.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey] = "E-MISSING";
            project.Elements.Add(opening);

            Throws<InvalidOperationException>(() => ProjectInterchangeJsonExporter.Build(project));
        }

        private static void CatalogRejectsNullSemanticCollections()
        {
            var zoneProject = BaseProject("P-NULL-ZONE");
            var zoneVersion = zoneProject.ChangeVersion;
            var zoneUpdatedUtc = zoneProject.UpdatedUtc;
            Throws<ArgumentNullException>(() => zoneProject.Zones.Add(null!));
            Equal(0, zoneProject.Zones.Count);
            Equal(zoneVersion, zoneProject.ChangeVersion);
            Equal(zoneUpdatedUtc, zoneProject.UpdatedUtc);
            True(!string.IsNullOrWhiteSpace(ProjectInterchangeJsonExporter.Build(zoneProject)));

            var floorProject = BaseProject("P-NULL-FLOOR");
            var floorCount = floorProject.Floors.Count;
            var floorVersion = floorProject.ChangeVersion;
            var floorUpdatedUtc = floorProject.UpdatedUtc;
            Throws<ArgumentNullException>(() => floorProject.Floors.Add(null!));
            Equal(floorCount, floorProject.Floors.Count);
            Equal(floorVersion, floorProject.ChangeVersion);
            Equal(floorUpdatedUtc, floorProject.UpdatedUtc);
            True(!string.IsNullOrWhiteSpace(ProjectInterchangeJsonExporter.Build(floorProject)));

            var familyProject = BaseProject("P-NULL-FAMILY");
            var familyVersion = familyProject.ChangeVersion;
            var familyUpdatedUtc = familyProject.UpdatedUtc;
            Throws<ArgumentNullException>(() => familyProject.Families.Add(null!));
            Equal(0, familyProject.Families.Count);
            Equal(familyVersion, familyProject.ChangeVersion);
            Equal(familyUpdatedUtc, familyProject.UpdatedUtc);
            True(!string.IsNullOrWhiteSpace(ProjectInterchangeJsonExporter.Build(familyProject)));
        }

        private static void ExportRejectsDuplicateSemanticIdentities()
        {
            var zoneProject = BaseProject("P-DUP-ZONE");
            zoneProject.Zones.Add(new ZoneDefinition("ZONE-1", "Zone 1"));
            zoneProject.Zones.Add(new ZoneDefinition("zone-1", "Zone 1 duplicate"));
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(zoneProject));

            var floorProject = BaseProject("P-DUP-FLOOR");
            floorProject.Floors.Add(new FloorDefinition("a", "A duplicate", 4d));
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(floorProject));

            var familyProject = BaseProject("P-DUP-FAMILY");
            familyProject.Families.Add(new ProjectFamily("F-1", "Family 1", ElementCategory.Column));
            familyProject.Families.Add(new ProjectFamily("f-1", "Family 1 duplicate", ElementCategory.Column));
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(familyProject));

            var elementProject = BaseProject("P-DUP-ELEMENT");
            elementProject.Elements.Add(new ProjectElement("E-1", ElementCategory.Column, string.Empty, "A", string.Empty));
            elementProject.Elements.Add(new ProjectElement("e-1", ElementCategory.Column, string.Empty, "A", string.Empty));
            Throws<InvalidDataException>(() => ProjectInterchangeJsonExporter.Build(elementProject));
        }

        private static void ValidatorRejectsNullSemanticElementBeforeOrdering()
        {
            var project = BaseProject("P-NULL-ELEMENT");
            project.Elements.Add(null!);

            Throws<InvalidOperationException>(() => ProjectInterchangeSemanticReferenceValidator.Validate(project));
        }

        private static void ValidatorAndTypedReaderRejectMissingRegisteredReference()
        {
            var project = BaseProject("P-READ-REF");
            var wall = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, string.Empty, "A", string.Empty);
            project.Elements.Add(wall);
            var opening = new ProjectElement("E-OPEN", ElementCategory.WallOpening, string.Empty, "A", string.Empty);
            opening.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey] = wall.Id;
            project.Elements.Add(opening);
            var json = ProjectInterchangeJsonExporter.Build(project);
            var smuggled = json.Replace(
                "\"HostWallId\":\"E-WALL\"",
                "\"HostWallId\":\"E-MISSING\"",
                StringComparison.Ordinal);
            True(!string.Equals(json, smuggled, StringComparison.Ordinal));
            var validation = ProjectInterchangeJsonValidator.Validate(smuggled);
            True(!validation.IsValid);
            True(validation.Issues.Any(x => string.Equals(x.Code, "SEMANTIC_PROPERTY_REF_MISSING", StringComparison.Ordinal)));
            Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read(smuggled));
        }

        private static void ValidatorAndTypedReaderRejectPaddedRegisteredReference()
        {
            var project = BaseProject("P-PADDED-REF");
            var wall = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, string.Empty, "A", string.Empty);
            project.Elements.Add(wall);
            var opening = new ProjectElement("E-OPEN", ElementCategory.WallOpening, string.Empty, "A", string.Empty);
            opening.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey] = wall.Id;
            project.Elements.Add(opening);

            ProjectInterchangeSemanticReferenceValidator.Validate(project);
            opening.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey] = " E-WALL ";
            Throws<InvalidOperationException>(() => ProjectInterchangeSemanticReferenceValidator.Validate(project));

            opening.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey] = wall.Id;
            var json = ProjectInterchangeJsonExporter.Build(project);
            var smuggled = json.Replace(
                "\"HostWallId\":\"E-WALL\"",
                "\"HostWallId\":\" E-WALL \"",
                StringComparison.Ordinal);
            True(!string.Equals(json, smuggled, StringComparison.Ordinal));
            var validation = ProjectInterchangeJsonValidator.Validate(smuggled);
            True(!validation.IsValid);
            True(validation.Issues.Any(x => string.Equals(x.Code, "SEMANTIC_PROPERTY_REF_NON_CANONICAL", StringComparison.Ordinal)));
            Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read(smuggled));
        }

        private static void ValidatorPreservesFreeTextAndCaseInsensitiveReference()
        {
            var project = BaseProject("P-CANONICAL-REF");
            var wall = new ProjectElement("E-WALL", ElementCategory.ArchitecturalWall, string.Empty, "A", string.Empty);
            project.Elements.Add(wall);
            var opening = new ProjectElement("E-OPEN", ElementCategory.WallOpening, string.Empty, "A", string.Empty);
            opening.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey] = "e-wall";
            opening.Properties["Description"] = " padded free text ";
            project.Elements.Add(opening);

            ProjectInterchangeSemanticReferenceValidator.Validate(project);
            var json = ProjectInterchangeJsonExporter.Build(project);
            var validation = ProjectInterchangeJsonValidator.Validate(json);
            True(validation.IsValid);
            var snapshot = ProjectInterchangeValidatedSnapshotReader.Read(json);
            var restored = snapshot.Elements.Single(x => string.Equals(x.Id, opening.Id, StringComparison.OrdinalIgnoreCase));
            Equal("e-wall", restored.Properties[ProjectInterchangeSemanticReferencePolicy.HostWallIdKey]);
            Equal(" padded free text ", restored.Properties["Description"]);
        }

        private static void ValidatorAndTypedReaderRejectInvalidLevelChain()
        {
            var project = BaseProject("P-READ-LEVEL");
            var element = new ProjectElement("E-1", ElementCategory.Column, string.Empty, "A", string.Empty);
            element.Properties[ProjectFloorService.BottomLevelIdKey] = "A";
            element.Properties[ProjectFloorService.TopLevelIdKey] = "B";
            project.Elements.Add(element);
            var json = ProjectInterchangeJsonExporter.Build(project);
            var invalid = json.Replace(
                "\"TopLevelId\":\"B\"",
                "\"TopLevelId\":\"A\"",
                StringComparison.Ordinal);
            True(!string.Equals(json, invalid, StringComparison.Ordinal));
            var validation = ProjectInterchangeJsonValidator.Validate(invalid);
            True(!validation.IsValid);
            True(validation.Issues.Any(x => string.Equals(x.Code, "LEVEL_ORDER", StringComparison.Ordinal)));
            Throws<InvalidDataException>(() => ProjectInterchangeValidatedSnapshotReader.Read(invalid));
        }

        private static void MixedFieldMergeRollsBackInvalidLevelComposition()
        {
            var target = BaseProject("TARGET-MIXED-LEVEL");
            var targetElement = new ProjectElement("E-1", ElementCategory.Column, string.Empty, "A", string.Empty);
            targetElement.Properties[ProjectFloorService.BottomLevelIdKey] = "A";
            targetElement.Properties[ProjectFloorService.TopLevelIdKey] = "B";
            target.Elements.Add(targetElement);

            var source = BaseProject("SOURCE-MIXED-LEVEL");
            source.FindFloor("A")!.ElevationM = 4d;
            source.FindFloor("B")!.ElevationM = 2d;
            var sourceElement = new ProjectElement("E-1", ElementCategory.Column, string.Empty, "C", string.Empty);
            sourceElement.Properties[ProjectFloorService.BottomLevelIdKey] = "C";
            sourceElement.Properties[ProjectFloorService.TopLevelIdKey] = "D";
            source.Elements.Add(sourceElement);

            var policy = new ProjectInterchangeFieldMergePolicy
            {
                FloorElevation = InterchangeFieldPrecedenceChoice.UseSource,
                ElementFloor = InterchangeFieldPrecedenceChoice.KeepTarget,
                ElementProperties = InterchangeFieldPrecedenceChoice.KeepTarget
            };
            var json = ProjectInterchangeJsonExporter.Build(source);
            var plan = ProjectInterchangeFieldMergeImporter.Plan(target, json, policy);
            True(plan.CanExecute);
            var authorization = plan.CreateAuthorization();
            var beforeVersion = target.ChangeVersion;
            var beforeA = target.FindFloor("A")!.ElevationM;
            var beforeB = target.FindFloor("B")!.ElevationM;

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeFieldMergeImporter.Import(target, json, policy, authorization));

            var restoredElement = target.FindElement("E-1") ?? throw new InvalidOperationException("Rollback lost E-1.");
            Equal(beforeA, target.FindFloor("A")!.ElevationM);
            Equal(beforeB, target.FindFloor("B")!.ElevationM);
            Equal("A", restoredElement.Properties[ProjectFloorService.BottomLevelIdKey]);
            Equal("B", restoredElement.Properties[ProjectFloorService.TopLevelIdKey]);
            Equal(beforeVersion, target.ChangeVersion);
        }

        private static ProjectState BaseProject(string id)
        {
            var project = new ProjectState(id, id) { DrawingFingerprint = id + "-fp" };
            project.Floors.Add(new FloorDefinition("A", "A", 0d));
            project.Floors.Add(new FloorDefinition("B", "B", 3d));
            project.Floors.Add(new FloorDefinition("C", "C", 0d));
            project.Floors.Add(new FloorDefinition("D", "D", 3d));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException("ProjectInterchangeSemanticReferenceValidationSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool value)
        {
            if (!value) throw new InvalidOperationException("ProjectInterchangeSemanticReferenceValidationSmoke expected true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("ProjectInterchangeSemanticReferenceValidationSmoke expected exception " + typeof(T).Name + ".");
        }
    }
}