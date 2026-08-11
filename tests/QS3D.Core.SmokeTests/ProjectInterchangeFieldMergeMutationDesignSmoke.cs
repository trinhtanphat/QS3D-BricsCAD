using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeFieldMergeMutationDesignSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            TargetBindingAndDependentCleanupAreExplicit();
            KeepTargetDoesNotManufactureCleanupAuthority();
            UnresolvedFieldPlanCannotBecomeMutationDesign();
            DestructiveDesignRequiresDrawingFingerprint();
        }

        private static void TargetBindingAndDependentCleanupAreExplicit()
        {
            var target = BuildTarget("target-field-design");
            var source = BuildSource();
            var beforeVersion = target.ChangeVersion;

            var design = ProjectInterchangeFieldMergeMutationDesignPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(source),
                new ProjectInterchangeFieldMergePolicy
                {
                    FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource
                });

            True(design.IsPreviewOnly);
            True(design.CanProceedToGuardedAdapterDesign);
            True(design.FieldPlan.IsPreviewOnly);
            Equal("TARGET-FIELD-DESIGN", design.TargetProjectId);
            Equal("SOURCE-FIELD-DESIGN", design.SourceProjectId);
            Equal("target-field-design", design.TargetDrawingFingerprint);
            Equal(beforeVersion, design.TargetChangeVersion);
            Equal(beforeVersion, target.ChangeVersion);

            Sequence(new[] { "E-DEP", "E-HOST" }, design.AffectedTargetElementIds);
            True(design.RequiresNativeCleanup);
            Equal(2, design.NativeCleanupRequirements.Count);
            Equal(3, design.TargetGeneratedHandlesToClean);
            Sequence(new[] { "E-DEP", "E-HOST" }, design.TargetElementIdsRequiringNativeCleanup);

            var host = design.NativeCleanupRequirements.Single(x => string.Equals(x.ElementId, "E-HOST", StringComparison.OrdinalIgnoreCase));
            Sequence(new[] { "A1" }, host.OwnerHandles);
            var dependent = design.NativeCleanupRequirements.Single(x => string.Equals(x.ElementId, "E-DEP", StringComparison.OrdinalIgnoreCase));
            Sequence(new[] { "B1", "B2" }, dependent.OwnerHandles);
            True(!design.AffectedTargetElementIds.Contains("E-OTHER", StringComparer.OrdinalIgnoreCase));
        }

        private static void KeepTargetDoesNotManufactureCleanupAuthority()
        {
            var target = BuildTarget("target-field-design");
            var design = ProjectInterchangeFieldMergeMutationDesignPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(BuildSource()),
                new ProjectInterchangeFieldMergePolicy
                {
                    FamilyProperties = InterchangeFieldPrecedenceChoice.KeepTarget
                });

            True(design.CanProceedToGuardedAdapterDesign);
            Equal(0, design.AffectedTargetElementIds.Count);
            Equal(0, design.NativeCleanupRequirements.Count);
            Equal(0, design.TargetGeneratedHandlesToClean);
            True(!design.RequiresNativeCleanup);
        }

        private static void UnresolvedFieldPlanCannotBecomeMutationDesign()
        {
            var target = BuildTarget("target-field-design");
            var beforeVersion = target.ChangeVersion;
            ThrowsInvalidOperation(() => ProjectInterchangeFieldMergeMutationDesignPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(BuildSource()),
                new ProjectInterchangeFieldMergePolicy()));
            Equal(beforeVersion, target.ChangeVersion);
        }

        private static void DestructiveDesignRequiresDrawingFingerprint()
        {
            var target = BuildTarget(string.Empty);
            var beforeVersion = target.ChangeVersion;
            ThrowsInvalidOperation(() => ProjectInterchangeFieldMergeMutationDesignPlanner.Plan(
                target,
                ProjectInterchangeJsonExporter.Build(BuildSource()),
                new ProjectInterchangeFieldMergePolicy
                {
                    FamilyProperties = InterchangeFieldPrecedenceChoice.UseSource
                }));
            Equal(beforeVersion, target.ChangeVersion);
        }

        private static ProjectState BuildTarget(string drawingFingerprint)
        {
            var project = new ProjectState("TARGET-FIELD-DESIGN", "Target field mutation design")
            {
                DrawingFingerprint = drawingFingerprint
            };
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone"));
            project.Floors.Add(new FloorDefinition("F-01", "Floor", 0d));

            var primary = new ProjectFamily("FAM-A", "Primary", ElementCategory.Beam);
            primary.Properties["Material"] = "C30";
            project.Families.Add(primary);
            project.Families.Add(new ProjectFamily("FAM-B", "Dependent", ElementCategory.Beam));

            var host = new ProjectElement("E-HOST", ElementCategory.Beam, "FAM-A", "F-01", "Z-01")
            {
                DrawingFingerprint = drawingFingerprint
            };
            host.SetProperty("GeneratedSolidHandle", "A1");
            project.Elements.Add(host);

            var dependent = new ProjectElement("E-DEP", ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = drawingFingerprint
            };
            dependent.DependsOn.Add("E-HOST");
            dependent.SetProperty("GeneratedRebarHandles", "B1;B2");
            project.Elements.Add(dependent);

            var unrelated = new ProjectElement("E-OTHER", ElementCategory.Beam, "FAM-B", "F-01", "Z-01")
            {
                DrawingFingerprint = drawingFingerprint
            };
            unrelated.SetProperty("GeneratedSolidHandle", "C1");
            project.Elements.Add(unrelated);
            return project;
        }

        private static ProjectState BuildSource()
        {
            var project = new ProjectState("SOURCE-FIELD-DESIGN", "Source field mutation design")
            {
                DrawingFingerprint = "source-field-design"
            };
            project.Zones.Add(new ZoneDefinition("Z-01", "Zone"));
            project.Floors.Add(new FloorDefinition("F-01", "Floor", 0d));
            var family = new ProjectFamily("FAM-A", "Primary", ElementCategory.Beam);
            family.Properties["Material"] = "C40";
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E-HOST", ElementCategory.Beam, "FAM-A", "F-01", "Z-01")
            {
                DrawingFingerprint = "source-field-design"
            });
            return project;
        }

        private static void ThrowsInvalidOperation(Action action)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }
            throw new Exception("Expected InvalidOperationException was not thrown.");
        }

        private static void Sequence(string[] expected, System.Collections.Generic.IEnumerable<string> actual)
        {
            var values = actual.ToArray();
            if (!expected.SequenceEqual(values, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    "ProjectInterchangeFieldMergeMutationDesignSmoke sequence mismatch. expected=" +
                    string.Join(",", expected) + " actual=" + string.Join(",", values));
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(
                    "ProjectInterchangeFieldMergeMutationDesignSmoke expected '" + expected + "' but got '" + actual + "'.");
        }

        private static void True(bool condition)
        {
            if (!condition)
                throw new InvalidOperationException("ProjectInterchangeFieldMergeMutationDesignSmoke assertion failed.");
        }
    }
}
