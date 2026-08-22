using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectQuantityReportMassUnderflowSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            var project = new ProjectState(Guid.NewGuid().ToString("N"), "Quantity report mass underflow");
            project.Zones.Add(new ZoneDefinition("zone-1", "Zone 1"));
            project.Floors.Add(new FloorDefinition("floor-0", "Floor 0", 0d));
            project.ActiveZoneId = "zone-1";
            project.ActiveFloorId = "floor-0";

            var family = new ProjectFamily("wall", "Wall", ElementCategory.ArchitecturalWall);
            project.Families.Add(family);

            var element = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, "floor-0", "zone-1");
            element.Properties["DensityKgM3"] = "0.5";
            element.SetQuantity("VolumeM3", double.Epsilon);
            project.Elements.Add(element);

            try
            {
                var rows = ProjectQuantityReportBuilder.Detail(project);
                var reportedMass = rows.Count == 1 && rows[0].MassKg.HasValue
                    ? rows[0].MassKg.GetValueOrDefault().ToString("R")
                    : "<missing>";
                throw new InvalidOperationException(
                    "Positive finite density-derived mass that rounds to zero must fail closed instead of reporting MassKg=" +
                    reportedMass + ".");
            }
            catch (InvalidOperationException ex) when (
                ex.Message.IndexOf("underflow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
            }
        }
    }
}
