using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeRemapCompatibilitySmoke
    {
        internal static void Run()
        {
            FamilyPropertyValueOverflowBlocksBeforeMutation();
        }

        private static void FamilyPropertyValueOverflowBlocksBeforeMutation()
        {
            var target = NewProject("target", ElementCategory.Column, "TARGET-FAM", "Target Family", "TARGET-ELEM");
            var source = NewProject("source", ElementCategory.Beam, "SOURCE-FAM", "Source Family", "SOURCE-ELEM");
            source.Families.Single().Properties["LongValue"] = new string('X', 1001);
            var json = ProjectInterchangeJsonExporter.Build(source);
            var zones = target.Zones.Count;
            var floors = target.Floors.Count;
            var families = target.Families.Count;
            var elements = target.Elements.Count;

            var plan = ProjectInterchangeRemapAppendImporter.Plan(target, json);

            False(plan.CanImport);
            Equal(1, plan.CompatibilityBlockers.Count);
            var blocker = plan.CompatibilityBlockers.Single();
            Equal("Family", blocker.OwnerKind);
            Equal("SOURCE-FAM", blocker.OwnerSourceId);
            Equal("LongValue", blocker.Field);
            True(blocker.Reason.IndexOf("1000", StringComparison.Ordinal) >= 0);
            Throws<InvalidOperationException>(() => ProjectInterchangeRemapAppendImporter.Import(target, json));
            Equal(zones, target.Zones.Count);
            Equal(floors, target.Floors.Count);
            Equal(families, target.Families.Count);
            Equal(elements, target.Elements.Count);
        }

        private static ProjectState NewProject(
            string id,
            ElementCategory category,
            string familyId,
            string familyName,
            string elementId)
        {
            var project = new ProjectState(id, "Project " + id);
            project.Zones.Add(new ZoneDefinition(id + "-zone", "Zone " + id));
            project.Floors.Add(new FloorDefinition(id + "-floor", "Floor " + id, 0d));
            project.Families.Add(new ProjectFamily(familyId, familyName, category));
            project.Elements.Add(new ProjectElement(elementId, category, familyId, id + "-floor", id + "-zone"));
            return project;
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }

        private static void False(bool value)
        {
            if (value) throw new Exception("Expected condition to be false.");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }

    internal static class ProjectInterchangeRemapCompatibilitySmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => ProjectInterchangeRemapCompatibilitySmoke.Run();
    }
}
