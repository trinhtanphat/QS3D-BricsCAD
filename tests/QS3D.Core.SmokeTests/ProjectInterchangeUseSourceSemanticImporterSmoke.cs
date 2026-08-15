using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeUseSourceSemanticImporterSmoke
    {
        internal static void Run()
        {
            PlanClassifiesReplacementAndNativeCleanup();
            ImportRejectsMissingNativeCleanupWithoutMutation();
            ImportRejectsStaleNativeCleanupHandleAuthorizationWithoutMutation();
            CaseInsensitiveCollisionsUseSourceAndAddDistinctItems();
            ImportReplacesInPlaceAndInvalidatesAffectedTargetElements();
            SemanticOnlyReplacementNeedsNoNativeAuthorization();
            ConflictsFailBeforeMutation();
        }

        private static void CaseInsensitiveCollisionsUseSourceAndAddDistinctItems()
        {
            var target = LowercaseIdentityTargetProject();
            var existingFamily = target.Families.Single();
            var existingElement = target.Elements.Single();

            var result = ProjectInterchangeUseSourceSemanticImporter.Import(
                target,
                ProjectInterchangeJsonExporter.Build(SourceProject()),
                ProjectInterchangeNativeCleanupAuthorization.None);

            Equal(4, result.ZonesAdded + result.FloorsAdded + result.FamiliesAdded + result.ElementsAdded);
            Equal(4, result.ZonesReplaced + result.FloorsReplaced + result.FamiliesReplaced + result.ElementsReplaced);
            Equal(2, target.Zones.Count);
            Equal(2, target.Floors.Count);
            Equal(2, target.Families.Count);
            Equal(2, target.Elements.Count);
            True(ReferenceEquals(existingFamily, target.Families.Single(x => x.Id == "fam1")));
            True(ReferenceEquals(existingElement, target.Elements.Single(x => x.Id == "e1")));
            Equal("Source Zone Same Id", (target.FindZone("z1") ?? throw new Exception("Replaced Zone missing.")).Name);
            Equal("Source Floor Same Id", (target.FindFloor("f1") ?? throw new Exception("Replaced Floor missing.")).Name);
            Equal("SOURCE-C99", existingFamily.Properties["Material"]);
            Equal("SOURCE-B-01", existingElement.Properties["Mark"]);
            Equal("z1", target.ActiveZoneId);
            Equal("f1", target.ActiveFloorId);
            Equal("fam1", target.Metadata["ActiveFamilyId"]);
            True(target.FindElement("E2") != null);
        }

        private static void PlanClassifiesReplacementAndNativeCleanup()
        {
            var target = TargetProject(includeGeneratedOwnership: true);
            var json = ProjectInterchangeJsonExporter.Build(SourceProject());
            var updated = new DateTime(2026, 8, 11, 0, 15, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;

            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);

            Equal("SOURCE-P", plan.SourceProjectId);
            Equal(1, plan.ZonesToAdd);
            Equal(1, plan.FloorsToAdd);
            Equal(1, plan.FamiliesToAdd);
            Equal(1, plan.ElementsToAdd);
            Equal(1, plan.ZonesToReplace);
            Equal(1, plan.FloorsToReplace);
            Equal(1, plan.FamiliesToReplace);
            Equal(1, plan.ElementsToReplace);
            Equal(4, plan.TotalSemanticIdentitiesToAdd);
            Equal(4, plan.TotalSemanticIdentitiesToReplace);
            Equal(2, plan.SourceHandlesToDiscard);
            Equal(2, plan.AffectedTargetElementIds.Count);
            True(plan.AffectedTargetElementIds.Contains("E1", StringComparer.OrdinalIgnoreCase));
            True(plan.AffectedTargetElementIds.Contains("E3", StringComparer.OrdinalIgnoreCase));
            Equal(2, plan.TargetElementIdsRequiringNativeCleanup.Count);
            Equal(2, plan.NativeCleanupRequirements.Count);
            Equal("AA11", string.Join("|", plan.NativeCleanupRequirements.Single(x => x.ElementId == "E1").OwnerHandles));
            Equal("EE22|FF33", string.Join("|", plan.NativeCleanupRequirements.Single(x => x.ElementId == "E3").OwnerHandles));
            Equal(3, plan.TargetGeneratedHandlesToClean);
            True(plan.RequiresNativeCleanup);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportRejectsMissingNativeCleanupWithoutMutation()
        {
            var target = TargetProject(includeGeneratedOwnership: true);
            var zone = target.FindZone("Z1") ?? throw new Exception("Target Zone missing.");
            var floor = target.FindFloor("F1") ?? throw new Exception("Target Floor missing.");
            var family = target.FindFamily("FAM1") ?? throw new Exception("Target Family missing.");
            var element = target.FindElement("E1") ?? throw new Exception("Target element missing.");
            var targetOnly = target.FindElement("E3") ?? throw new Exception("Target-only element missing.");
            var updated = new DateTime(2026, 8, 11, 0, 20, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(
                    target,
                    ProjectInterchangeJsonExporter.Build(SourceProject()),
                    ProjectInterchangeNativeCleanupAuthorization.None));

            True(ReferenceEquals(zone, target.FindZone("Z1")));
            True(ReferenceEquals(floor, target.FindFloor("F1")));
            True(ReferenceEquals(family, target.FindFamily("FAM1")));
            True(ReferenceEquals(element, target.FindElement("E1")));
            True(ReferenceEquals(targetOnly, target.FindElement("E3")));
            Equal("Target Zone", zone.Name);
            Equal("Target Floor", floor.Name);
            Near(0d, floor.ElevationM);
            Equal("C30", family.Properties["Material"]);
            Equal("TARGET-B-01", element.Properties["Mark"]);
            Equal("AA11", element.Properties["GeneratedSolidHandle"]);
            Equal("EE22;FF33", targetOnly.Properties["GeneratedRebarHandles"]);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportRejectsStaleNativeCleanupHandleAuthorizationWithoutMutation()
        {
            var target = TargetProject(includeGeneratedOwnership: true);
            var json = ProjectInterchangeJsonExporter.Build(SourceProject());
            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan);
            var element = target.FindElement("E1") ?? throw new Exception("Target element missing.");
            element.Properties["GeneratedSolidHandle"] = "BB22";
            var updated = new DateTime(2026, 8, 11, 0, 22, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;

            Throws<InvalidOperationException>(() =>
                ProjectInterchangeUseSourceSemanticImporter.Import(target, json, authorization));

            Equal("TARGET-B-01", element.Properties["Mark"]);
            Equal("BB22", element.Properties["GeneratedSolidHandle"]);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static void ImportReplacesInPlaceAndInvalidatesAffectedTargetElements()
        {
            var target = TargetProject(includeGeneratedOwnership: true);
            var originalProjectId = target.ProjectId;
            var originalName = target.Name;
            var originalDrawingPath = target.DrawingPath;
            var originalDrawingFingerprint = target.DrawingFingerprint;
            var originalActiveZone = target.ActiveZoneId;
            var originalActiveFloor = target.ActiveFloorId;
            var originalActiveFamily = target.Metadata["ActiveFamilyId"];
            var zone = target.FindZone("Z1") ?? throw new Exception("Target Zone missing.");
            var floor = target.FindFloor("F1") ?? throw new Exception("Target Floor missing.");
            var family = target.FindFamily("FAM1") ?? throw new Exception("Target Family missing.");
            var element = target.FindElement("E1") ?? throw new Exception("Target element missing.");
            var targetOnly = target.FindElement("E3") ?? throw new Exception("Target-only element missing.");
            var targetOnlySourceHandle = targetOnly.SourceHandles.Single();
            var targetOnlyFingerprint = targetOnly.DrawingFingerprint;

            var json = ProjectInterchangeJsonExporter.Build(SourceProject());
            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            var authorization = ProjectInterchangeNativeCleanupAuthorization.ForPlan(plan);
            True(authorization.IsHandleBound);
            var result = ProjectInterchangeUseSourceSemanticImporter.Import(target, json, authorization);

            Equal("SOURCE-P", result.SourceProjectId);
            Equal(1, result.ZonesAdded);
            Equal(1, result.FloorsAdded);
            Equal(1, result.FamiliesAdded);
            Equal(1, result.ElementsAdded);
            Equal(1, result.ZonesReplaced);
            Equal(1, result.FloorsReplaced);
            Equal(1, result.FamiliesReplaced);
            Equal(1, result.ElementsReplaced);
            Equal(2, result.SourceHandlesDiscarded);
            Equal(2, result.AffectedTargetElementsMarkedDirty);
            Equal(2, result.NativeCleanupElementsAuthorized);
            Equal(3, result.TargetGeneratedHandlesCleaned);

            Equal(originalProjectId, target.ProjectId);
            Equal(originalName, target.Name);
            Equal(originalDrawingPath, target.DrawingPath);
            Equal(originalDrawingFingerprint, target.DrawingFingerprint);
            Equal(originalActiveZone, target.ActiveZoneId);
            Equal(originalActiveFloor, target.ActiveFloorId);
            Equal(originalActiveFamily, target.Metadata["ActiveFamilyId"]);

            True(ReferenceEquals(zone, target.FindZone("Z1")));
            True(ReferenceEquals(floor, target.FindFloor("F1")));
            True(ReferenceEquals(family, target.FindFamily("FAM1")));
            True(ReferenceEquals(element, target.FindElement("E1")));
            True(ReferenceEquals(targetOnly, target.FindElement("E3")));

            Equal("Source Zone Same Id", zone.Name);
            Equal("Source Floor Same Id", floor.Name);
            Near(9d, floor.ElevationM);
            Equal("Source Beam Same Id", family.Name);
            Equal("SOURCE-C99", family.Properties["Material"]);

            Equal("SOURCE-B-01", element.Properties["Mark"]);
            Near(99d, element.Quantities["LengthM"]);
            Equal(0, element.SourceHandles.Count);
            Equal(string.Empty, element.DrawingFingerprint);
            True(!element.Properties.ContainsKey("GeneratedSolidHandle"));
            Equal(ElementDirtyFlags.All, element.Dirty);

            Equal("TARGET-B-03", targetOnly.Properties["Mark"]);
            Equal(targetOnlySourceHandle, targetOnly.SourceHandles.Single());
            Equal(targetOnlyFingerprint, targetOnly.DrawingFingerprint);
            True(!targetOnly.Properties.ContainsKey("GeneratedRebarHandles"));
            True(!targetOnly.Properties.ContainsKey(ProjectElement.GeneratedRebarStateKey));
            Equal(ElementDirtyFlags.All, targetOnly.Dirty);

            Equal(2, target.Zones.Count);
            Equal(2, target.Floors.Count);
            Equal(2, target.Families.Count);
            Equal(3, target.Elements.Count);
            var imported = target.FindElement("E2") ?? throw new Exception("New source element was not imported.");
            Equal("FAM2", imported.FamilyId);
            Equal("F2", imported.FloorId);
            Equal("Z2", imported.ZoneId);
            Equal("E1", imported.DependsOn.Single());
            Equal("B-02", imported.Properties["Mark"]);
            Near(6.25d, imported.Quantities["LengthM"]);
            Equal(0, imported.SourceHandles.Count);
            Equal(string.Empty, imported.DrawingFingerprint);
            Equal(ElementDirtyFlags.All, imported.Dirty);

            Equal(ProjectInterchangeUseSourceSemanticImporter.ImportMode, target.Metadata[ProjectInterchangeAppendOnlyImporter.LastModeKey]);
            Equal("4", target.Metadata[ProjectInterchangeUseSourceSemanticImporter.LastSemanticIdentitiesAddedKey]);
            Equal("4", target.Metadata[ProjectInterchangeUseSourceSemanticImporter.LastSemanticIdentitiesReplacedKey]);
            Equal("2", target.Metadata[ProjectInterchangeUseSourceSemanticImporter.LastAffectedTargetElementsKey]);
            Equal("2", target.Metadata[ProjectInterchangeUseSourceSemanticImporter.LastNativeCleanupElementsKey]);
            Equal("3", target.Metadata[ProjectInterchangeUseSourceSemanticImporter.LastTargetGeneratedHandlesCleanedKey]);
            Equal("ImportInterchangeUseSourceSemantic", target.AuditEvents.Last().Action);
        }

        private static void SemanticOnlyReplacementNeedsNoNativeAuthorization()
        {
            var target = TargetProject(includeGeneratedOwnership: false);
            var json = ProjectInterchangeJsonExporter.Build(SourceProject());
            var plan = ProjectInterchangeUseSourceSemanticImporter.Plan(target, json);
            True(!plan.RequiresNativeCleanup);
            Equal(0, plan.TargetGeneratedHandlesToClean);

            var result = ProjectInterchangeUseSourceSemanticImporter.Import(
                target,
                json,
                ProjectInterchangeNativeCleanupAuthorization.None);

            Equal(1, result.ElementsReplaced);
            Equal(0, result.NativeCleanupElementsAuthorized);
            Equal("SOURCE-B-01", (target.FindElement("E1") ?? throw new Exception("Replaced element missing.")).Properties["Mark"]);
        }

        private static void ConflictsFailBeforeMutation()
        {
            var target = TargetProject(includeGeneratedOwnership: false);
            var source = SourceProject();
            source.Zones.Add(new ZoneDefinition("Z3", "Target Zone"));
            AssertPlanFailsWithoutMutation(target, ProjectInterchangeJsonExporter.Build(source));

            target = TargetProject(includeGeneratedOwnership: false);
            source = SourceProject();
            source.Families.Remove(source.Families.Single(x => x.Id == "FAM1"));
            source.Families.Add(new ProjectFamily("FAM1", "Source Column Family", ElementCategory.Column));
            source.Elements.Remove(source.Elements.Single(x => x.Id == "E1"));
            source.Elements.Add(new ProjectElement("E1", ElementCategory.Column, "FAM1", "F1", "Z1"));
            AssertPlanFailsWithoutMutation(target, ProjectInterchangeJsonExporter.Build(source));

            target = TargetProject(includeGeneratedOwnership: false);
            var updated = new DateTime(2026, 8, 11, 0, 30, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;
            Throws<InvalidDataException>(() => ProjectInterchangeUseSourceSemanticImporter.Plan(target, "{\"format\":\"Wrong\"}"));
            Equal(updated, target.UpdatedUtc);
        }

        private static void AssertPlanFailsWithoutMutation(ProjectState target, string json)
        {
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;
            var metadata = target.Metadata.Count;
            var audits = target.AuditEvents.Count;
            var updated = new DateTime(2026, 8, 11, 0, 25, 0, DateTimeKind.Utc);
            target.UpdatedUtc = updated;

            Throws<InvalidOperationException>(() => ProjectInterchangeUseSourceSemanticImporter.Plan(target, json));

            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
            Equal(metadata, target.Metadata.Count);
            Equal(audits, target.AuditEvents.Count);
            Equal(updated, target.UpdatedUtc);
        }

        private static ProjectState TargetProject(bool includeGeneratedOwnership)
        {
            var project = new ProjectState("TARGET-P", "Target Project")
            {
                DrawingPath = "target.dwg",
                DrawingFingerprint = "target-fingerprint",
                ActiveZoneId = "Z1",
                ActiveFloorId = "F1"
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Target Zone"));
            project.Floors.Add(new FloorDefinition("F1", "Target Floor", 0d));
            var family = new ProjectFamily("FAM1", "Target Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);
            project.Metadata["ActiveFamilyId"] = family.Id;

            var element = new ProjectElement("E1", ElementCategory.Beam, family.Id, "F1", "Z1")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            element.SourceHandles.Add("10A0");
            element.Properties["Mark"] = "TARGET-B-01";
            element.Quantities["LengthM"] = 4d;
            if (includeGeneratedOwnership)
            {
                element.Properties["GeneratedSolidHandle"] = "AA11";
                element.Properties[ProjectElement.GeneratedSolidStateKey] = "current";
            }
            project.Elements.Add(element);

            var targetOnly = new ProjectElement("E3", ElementCategory.Beam, family.Id, "F1", "Z1")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            targetOnly.SourceHandles.Add("30C0");
            targetOnly.Properties["Mark"] = "TARGET-B-03";
            targetOnly.Quantities["LengthM"] = 3d;
            if (includeGeneratedOwnership)
            {
                targetOnly.Properties["GeneratedRebarHandles"] = "EE22;FF33";
                targetOnly.Properties[ProjectElement.GeneratedRebarStateKey] = "current";
            }
            project.Elements.Add(targetOnly);
            return project;
        }

        private static ProjectState LowercaseIdentityTargetProject()
        {
            var project = new ProjectState("TARGET-P", "Target Project")
            {
                DrawingPath = "target.dwg",
                DrawingFingerprint = "target-fingerprint",
                ActiveZoneId = "z1",
                ActiveFloorId = "f1"
            };
            project.Zones.Add(new ZoneDefinition("z1", "Target Zone"));
            project.Floors.Add(new FloorDefinition("f1", "Target Floor", 0d));
            var family = new ProjectFamily("fam1", "Target Beam Family", ElementCategory.Beam);
            family.Properties["Material"] = "C30";
            project.Families.Add(family);
            project.Metadata["ActiveFamilyId"] = family.Id;
            var element = new ProjectElement("e1", ElementCategory.Beam, family.Id, "f1", "z1")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            element.Properties["Mark"] = "TARGET-B-01";
            element.Quantities["LengthM"] = 4d;
            project.Elements.Add(element);
            return project;
        }

        private static ProjectState SourceProject()
        {
            var project = new ProjectState("SOURCE-P", "Source Project")
            {
                DrawingFingerprint = "source-fingerprint",
                UpdatedUtc = new DateTime(2026, 8, 11, 0, 10, 0, DateTimeKind.Utc)
            };
            project.Zones.Add(new ZoneDefinition("Z1", "Source Zone Same Id"));
            project.Zones.Add(new ZoneDefinition("Z2", "Source Zone 2"));
            project.Floors.Add(new FloorDefinition("F1", "Source Floor Same Id", 9d));
            project.Floors.Add(new FloorDefinition("F2", "Source Floor 2", 3.5d));

            var existingFamily = new ProjectFamily("FAM1", "Source Beam Same Id", ElementCategory.Beam);
            existingFamily.Properties["Material"] = "SOURCE-C99";
            project.Families.Add(existingFamily);
            var newFamily = new ProjectFamily("FAM2", "Source Beam 2", ElementCategory.Beam);
            newFamily.Properties["Material"] = "C40";
            project.Families.Add(newFamily);

            var existingElement = new ProjectElement("E1", ElementCategory.Beam, "FAM1", "F1", "Z1")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            existingElement.SourceHandles.Add("1A2B");
            existingElement.Properties["Mark"] = "SOURCE-B-01";
            existingElement.Quantities["LengthM"] = 99d;
            project.Elements.Add(existingElement);

            var newElement = new ProjectElement("E2", ElementCategory.Beam, "FAM2", "F2", "Z2")
            {
                DrawingFingerprint = project.DrawingFingerprint
            };
            newElement.SourceHandles.Add("2B3C");
            newElement.DependsOn.Add("E1");
            newElement.Properties["Mark"] = "B-02";
            newElement.Quantities["LengthM"] = 6.25d;
            project.Elements.Add(newElement);
            return project;
        }

        private static void Near(double expected, double actual)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeUseSourceSemanticImporterSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeUseSourceSemanticImporterSmoke.Run();
    }
}
