using System;
using System.IO;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Model;
using QS3D.Core.Rebar;
using QS3D.Core.Recognition;
using QS3D.Core.Reporting;
using QS3D.Core.Revisions;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class FullDomainIntegrationSmoke
    {
        public static void Run(){Recognition();RevisionStore();SteelInBqAndCsv();ColumnFootprint();EarthworkSwell();DimensionHealth();}
        private static void Recognition(){var engine=new RecognitionEngine();var beam=new EntitySnapshot("A1","Line","KC_DAM_B20");beam.Metadata["NearbyText"]="Dầm B20";var strong=engine.Suggest(beam);Equal(ElementCategory.Beam,strong.TopCandidate!.Category);True(strong.Confidence>.99d);True(!strong.RequiresReview);var ambiguous=new EntitySnapshot("A2","Line","WALL_VACH");var uncertain=engine.Suggest(ambiguous);True(uncertain.Candidates.Count>=2);True(uncertain.RequiresReview);var batch=engine.SuggestBatch(new[]{beam,ambiguous});Equal(1,batch.AutoAccepted.Count);Equal(1,batch.ReviewRequired.Count);}
        private static void RevisionStore(){var path=Path.Combine(Path.GetTempPath(),"qs3d-rev-"+Guid.NewGuid().ToString("N")+".qsrev");try{var project=NewProject();var e=new ProjectElement("B1",ElementCategory.Beam,"","f","z");e.Properties["Material"]="C30";e.SourceHandles.Add("AB12");e.SetQuantity("NetVolumeM3",.4d);project.Elements.Add(e);var snapshot=new RevisionService().Capture(project,"BASE");var store=new RevisionSnapshotStore();store.Save(snapshot,path);var loaded=store.Load(path);Equal("BASE",loaded.Id);Equal("C30",loaded.Elements[0].Properties["Material"]);Equal("AB12",loaded.Elements[0].SourceHandles[0]);Near(.4d,loaded.Elements[0].Quantities["NetVolumeM3"]);}finally{Delete(path);Delete(path+".bak");Delete(path+".tmp");}}
        private static void SteelInBqAndCsv(){var project=NewProject();var family=new ProjectFamily("beam","Dầm",ElementCategory.Beam);project.Families.Add(family);var beam=new ProjectElement("B2",ElementCategory.Beam,family.Id,"f","z");beam.Properties["RebarNotation"]="4D20";beam.Properties["RebarCuttingLengthM"]="5";project.Elements.Add(beam);var rows=ProjectQuantityReportBuilder.Group(project);Equal(1,rows.Count);True(rows[0].SteelWeightKg>49d);var total=QuantityReportTotals.FromRows(rows);Near(rows[0].SteelWeightKg,total.SteelWeightKg);var schedule=ProjectRebarScheduleBuilder.BuildElement(beam);var csv=RebarCsvExporter.ToCsv(schedule);True(csv.Contains("BarMark"));True(csv.Contains("TotalWeightKg"));}
        private static void ColumnFootprint(){var column=new ProjectElement("C1",ElementCategory.Column,"","f","z");column.Properties["AreaM2"]="0.09";column.Properties["PerimeterM"]="1.2";column.Properties["HeightM"]="3";new StructuralRegenerator().Regenerate(NewProject(),column);Near(.27d,column.Quantities["NetVolumeM3"]);Near(3.6d,column.Quantities["FormworkM2"]);}
        private static void EarthworkSwell(){var earth=new ProjectElement("E1",ElementCategory.Earthwork,"","f","z");earth.Properties["AreaM2"]="20";earth.Properties["DepthM"]="0.5";earth.Properties["SwellFactor"]="0.15";new StructuralRegenerator().Regenerate(NewProject(),earth);Near(10d,earth.Quantities["ExcavationVolumeM3"]);Near(11.5d,earth.Quantities["LooseExcavationVolumeM3"]);}
        private static void DimensionHealth(){var project=NewProject();var family=new ProjectFamily("beam","Dầm",ElementCategory.Beam);family.Properties["WidthM"]="0.2";family.Properties["HeightM"]="0.4";family.Properties["Material"]="Bê tông";project.Families.Add(family);var beam=new ProjectElement("B3",ElementCategory.Beam,family.Id,"f","z");project.Elements.Add(beam);var issues=new ModelHealthService().Inspect(project);True(issues.Any(x=>x.ElementId=="B3"&&x.Code=="MISSING_DIMENSION"));True(!issues.Any(x=>x.ElementId=="B3"&&x.Code=="MISSING_MATERIAL"));beam.Properties["LengthM"]="5";issues=new ModelHealthService().Inspect(project);True(!issues.Any(x=>x.ElementId=="B3"&&x.Code=="MISSING_DIMENSION"));}
        private static ProjectState NewProject(){var p=new ProjectState(Guid.NewGuid().ToString("N"),"Integration");p.Zones.Add(new ZoneDefinition("z","Zone"));p.Floors.Add(new FloorDefinition("f","Floor",0d));p.ActiveZoneId="z";p.ActiveFloorId="f";return p;}
        private static void Delete(string path){try{if(File.Exists(path))File.Delete(path);}catch{}}
        private static void Near(double expected,double actual){if(Math.Abs(expected-actual)>1e-9)throw new Exception("Expected "+expected+", got "+actual);}
        private static void Equal<T>(T expected,T actual){if(!Equals(expected,actual))throw new Exception("Expected "+expected+", got "+actual);}
        private static void True(bool value){if(!value)throw new Exception("Expected true.");}
    }
}
