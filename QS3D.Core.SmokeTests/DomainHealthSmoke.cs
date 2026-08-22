using System;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class DomainHealthSmoke
    {
        public static void Run()
        {
            StructuralDimensionsAndInheritedMaterial();
            EarthworkDimensions();
            RebarDefinitionAndLength();
        }

        private static void StructuralDimensionsAndInheritedMaterial()
        {
            var project = NewProject();
            var family = new ProjectFamily("beam", "Dầm", ElementCategory.Beam); family.Properties["WidthM"] = "0.2"; family.Properties["HeightM"] = "0.4"; family.Properties["Material"] = "Bê tông"; project.Families.Add(family);
            var beam = new ProjectElement("B1", ElementCategory.Beam, family.Id, "f", "z"); project.Elements.Add(beam);
            var issues = new ModelHealthService().Inspect(project);
            Require(issues.Any(x => x.ElementId == "B1" && x.Code == "MISSING_DIMENSION"), "Beam missing length was not reported.");
            Require(!issues.Any(x => x.ElementId == "B1" && x.Code == "MISSING_MATERIAL"), "Inherited structural material was not recognized.");
            beam.Properties["LengthM"] = "5"; issues = new ModelHealthService().Inspect(project);
            Require(!issues.Any(x => x.ElementId == "B1" && x.Code == "MISSING_DIMENSION"), "Valid beam dimensions were rejected.");
        }

        private static void EarthworkDimensions()
        {
            var project = NewProject(); var family = new ProjectFamily("earth", "Đào đất", ElementCategory.Earthwork); family.Properties["DepthM"] = "0.5"; project.Families.Add(family); var earth = new ProjectElement("E1", ElementCategory.Earthwork, family.Id, "f", "z"); project.Elements.Add(earth);
            var issues = new ModelHealthService().Inspect(project); Require(issues.Any(x => x.ElementId == "E1" && x.Code == "MISSING_DIMENSION"), "Earthwork missing area was not reported."); earth.Properties["AreaM2"] = "20"; issues = new ModelHealthService().Inspect(project); Require(!issues.Any(x => x.ElementId == "E1" && x.Code == "MISSING_DIMENSION"), "Valid earthwork dimensions were rejected.");
        }

        private static void RebarDefinitionAndLength()
        {
            var project = NewProject(); var family = new ProjectFamily("r", "Cốt thép", ElementCategory.Rebar); project.Families.Add(family); var rebar = new ProjectElement("R1", ElementCategory.Rebar, family.Id, "f", "z"); project.Elements.Add(rebar); var issues = new ModelHealthService().Inspect(project);
            Require(issues.Any(x => x.ElementId == "R1" && x.Code == "MISSING_REBAR_DEFINITION"), "Missing rebar definition was not reported."); Require(issues.Any(x => x.ElementId == "R1" && x.Code == "MISSING_REBAR_LENGTH"), "Missing rebar length was not reported.");
            rebar.Properties["Notation"] = "4D16"; rebar.Properties["CutLengthM"] = "5"; issues = new ModelHealthService().Inspect(project); Require(!issues.Any(x => x.ElementId == "R1" && (x.Code == "MISSING_REBAR_DEFINITION" || x.Code == "MISSING_REBAR_LENGTH")), "Valid rebar definition was rejected.");
        }

        private static ProjectState NewProject() { var project = new ProjectState(Guid.NewGuid().ToString("N"), "Health"); project.Zones.Add(new ZoneDefinition("z", "Zone")); project.Floors.Add(new FloorDefinition("f", "Floor", 0d)); project.ActiveZoneId = "z"; project.ActiveFloorId = "f"; return project; }
        private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
    }
}
