using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportGenerationFenceSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            StableGenerationPublishesFrozenValues();
            RejectsInPlaceQuantityMutationWithoutProjectVersionChange();
            RejectsInPlaceFamilyNameMutationWithoutProjectVersionChange();
            RejectsInPlaceSourceHandleMutationWithoutProjectVersionChange();
        }

        private static void StableGenerationPublishesFrozenValues()
        {
            var project = BuildProject();
            var rows = ProjectQuantityReportBuilder.Group(project);
            if (rows.Count != 1) throw new InvalidOperationException("Project quantity generation fence smoke expected one stable row.");
            if (!rows[0].GrossConcreteM3.Equals(2d)) throw new InvalidOperationException("Project quantity generation fence smoke changed stable quantity output.");
            if (!string.Equals(rows[0].FamilyName, "Wall Type A", StringComparison.Ordinal)) throw new InvalidOperationException("Project quantity generation fence smoke changed stable family output.");
        }

        private static void RejectsInPlaceQuantityMutationWithoutProjectVersionChange()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            SetSnapshotHook(p => p.Elements[0].Quantities["GrossConcreteM3"] = 9d);
            ExpectGenerationDrift(project, "quantity");
            if (project.ChangeVersion != version) throw new InvalidOperationException("Direct quantity mutation unexpectedly changed ProjectState.ChangeVersion; regression no longer exercises the bypass.");
        }

        private static void RejectsInPlaceFamilyNameMutationWithoutProjectVersionChange()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            SetSnapshotHook(p =>
            {
                var field = typeof(ProjectFamily).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(ProjectFamily).FullName, "_name");
                field.SetValue(p.Families[0], "Wall Type Drifted");
            });
            ExpectGenerationDrift(project, "family catalog");
            if (project.ChangeVersion != version) throw new InvalidOperationException("Direct family mutation unexpectedly changed ProjectState.ChangeVersion; regression no longer exercises the bypass.");
        }

        private static void RejectsInPlaceSourceHandleMutationWithoutProjectVersionChange()
        {
            var project = BuildProject();
            var version = project.ChangeVersion;
            SetSnapshotHook(p => p.Elements[0].SourceHandles.Add("BEEF"));
            ExpectGenerationDrift(project, "provenance");
            if (project.ChangeVersion != version) throw new InvalidOperationException("Direct provenance mutation unexpectedly changed ProjectState.ChangeVersion; regression no longer exercises the bypass.");
        }

        private static ProjectState BuildProject()
        {
            var project = new ProjectState("P-QTY-GEN", "Project quantity generation fence") { DrawingFingerprint = "FP-QTY-GEN" };
            project.Floors.Add(new FloorDefinition("F1", "Level 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var family = new ProjectFamily("WF1", "Wall Type A", ElementCategory.Wall);
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            var element = new ProjectElement("W1", ElementCategory.Wall, "WF1", "F1", "Z1");
            element.Quantities["GrossConcreteM3"] = 2d;
            element.Quantities["NetConcreteM3"] = 2d;
            element.SourceHandles.Add("ABCD");
            project.Elements.Add(element);
            return project;
        }

        private static void SetSnapshotHook(Action<ProjectState> mutation)
        {
            var field = typeof(ProjectQuantityReportBuilder).GetField("GenerationSnapshotCaptured", BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingFieldException(typeof(ProjectQuantityReportBuilder).FullName, "GenerationSnapshotCaptured");
            field.SetValue(null, mutation);
        }

        private static void ExpectGenerationDrift(ProjectState project, string label)
        {
            try
            {
                _ = ProjectQuantityReportBuilder.Group(project);
                throw new InvalidOperationException("Project quantity generation fence accepted in-place " + label + " drift.");
            }
            catch (InvalidOperationException ex) when (ex.Message.IndexOf("Project changed while the quantity report was being built", StringComparison.Ordinal) >= 0)
            {
            }
        }
    }
}
