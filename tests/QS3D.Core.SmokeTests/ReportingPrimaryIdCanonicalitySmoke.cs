using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ReportingPrimaryIdCanonicalitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ValidDoorScheduleResolvesCanonicalLabels();
            PaddedFloorPrimaryIdFailsClosed();
            PaddedFamilyPrimaryIdFailsClosed();
        }

        private static void ValidDoorScheduleResolvesCanonicalLabels()
        {
            var project = CreateProject();
            var rows = DoorOpeningScheduleBuilder.Build(project);
            if (rows.Count != 1) throw new Exception("Expected one valid door schedule row.");
            if (!string.Equals(rows[0].Floor, "Floor One", StringComparison.Ordinal))
                throw new Exception("Valid reporting Floor identity did not resolve its canonical label.");
            if (!string.Equals(rows[0].FamilyName, "Door Family", StringComparison.Ordinal))
                throw new Exception("Valid reporting Family identity did not resolve its canonical label.");
        }

        private static void PaddedFloorPrimaryIdFailsClosed()
        {
            var project = CreateProject();
            CorruptId(project.Floors[0], " F1 ");
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static void PaddedFamilyPrimaryIdFailsClosed()
        {
            var project = CreateProject();
            CorruptId(project.Families[0], " FAM1 ");
            Throws<InvalidOperationException>(() => DoorOpeningScheduleBuilder.Build(project));
        }

        private static ProjectState CreateProject()
        {
            var project = new ProjectState("P", "Reporting primary ID canonicality");
            var floor = new FloorDefinition("F1", "Floor One", 0d);
            var family = new ProjectFamily("FAM1", "Door Family", ElementCategory.Door);
            family.Properties["WidthM"] = "0.9";
            family.Properties["HeightM"] = "2.1";
            project.Floors.Add(floor);
            project.Families.Add(family);
            project.Elements.Add(new ProjectElement("E1", ElementCategory.Door, family.Id, floor.Id, string.Empty));
            return project;
        }

        private static void CorruptId(object target, string value)
        {
            var field = target.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new InvalidOperationException(target.GetType().Name + " Id backing field was not found for corruption smoke coverage.");
            field.SetValue(target, value);
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
        }
    }
}
