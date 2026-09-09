using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class MaterialUsageGenerationFenceSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            InPlaceQuantityDriftFailsClosed();
            FamilySemanticDriftFailsClosed();
            SourceHandleDriftFailsClosed();
            StableSnapshotRemainsDeterministic();
        }

        private static void InPlaceQuantityDriftFailsClosed()
        {
            var project = Project();
            var snapshot = Capture(project);
            project.Elements[0].Quantities["NetVolumeM3"] = 9d;
            RevalidateFails(snapshot, project, "in-place quantity drift");
        }

        private static void FamilySemanticDriftFailsClosed()
        {
            var project = Project();
            var snapshot = Capture(project);
            project.Families[0].Properties["Material"] = "Steel";
            RevalidateFails(snapshot, project, "family semantic drift");
        }

        private static void SourceHandleDriftFailsClosed()
        {
            var project = Project();
            var snapshot = Capture(project);
            project.Elements[0].SourceHandles[0] = "AB2";
            RevalidateFails(snapshot, project, "source-handle provenance drift");
        }

        private static void StableSnapshotRemainsDeterministic()
        {
            var project = Project();
            var first = MaterialUsageScheduleBuilder.Build(project).Single();
            var second = MaterialUsageScheduleBuilder.Build(project).Single();
            Equal(first.ProjectId, second.ProjectId, "project identity");
            Equal(first.DrawingFingerprint, second.DrawingFingerprint, "drawing fingerprint");
            Equal(first.MaterialName, second.MaterialName, "material");
            Equal(first.FamilyName, second.FamilyName, "family");
            Equal(first.VolumeM3, second.VolumeM3, "volume");
            Equal(string.Join(";", first.ElementIds), string.Join(";", second.ElementIds), "element provenance");
            Equal(string.Join(";", first.SourceHandles), string.Join(";", second.SourceHandles), "CAD provenance");
        }

        private static object Capture(ProjectState project)
        {
            var type = typeof(MaterialUsageScheduleBuilder).GetNestedType("MaterialUsageGenerationSnapshot", BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("MaterialUsageGenerationSnapshot type is missing.");
            var method = type.GetMethod("CaptureStable", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("CaptureStable boundary is missing.");
            return method.Invoke(null, new object[] { project })
                ?? throw new InvalidOperationException("CaptureStable returned no snapshot.");
        }

        private static void RevalidateFails(object snapshot, ProjectState project, string scenario)
        {
            var method = snapshot.GetType().GetMethod("Revalidate", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                ?? throw new InvalidOperationException("Revalidate boundary is missing.");
            try
            {
                method.Invoke(snapshot, new object[] { project });
            }
            catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
            {
                return;
            }
            throw new InvalidOperationException("Material Usage accepted " + scenario + " across the generation fence.");
        }

        private static ProjectState Project()
        {
            var project = new ProjectState("material-generation", "Material generation")
            {
                DrawingFingerprint = "DRAWING-GENERATION"
            };
            project.Floors.Add(new FloorDefinition("F1", "Floor 1", 0d));
            project.Zones.Add(new ZoneDefinition("Z1", "Zone 1"));
            var family = new ProjectFamily("FAM-1", "Slab family", ElementCategory.Slab);
            family.Properties["Material"] = "Concrete";
            project.Families.Add(family);
            var element = new ProjectElement("E1", ElementCategory.Slab, family.Id, "F1", "Z1");
            element.Quantities["NetVolumeM3"] = 2.5d;
            element.SourceHandles.Add("AA1");
            project.Elements.Add(element);
            return project;
        }

        private static void Equal(string expected, string actual, string label)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Material Usage stable snapshot changed " + label + ".");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException("Material Usage stable snapshot changed " + label + ".");
        }
    }
}
