using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Formulas;
using QS3D.Core.Geometry;
using QS3D.Core.Model;
using QS3D.Core.Persistence;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
using QS3D.Core.Revisions;
using QS3D.Core.Rules;
using QS3D.Core.Services;
using QS3D.Core.Takeoff;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class Program
    {
        private static int _failed;

        private static int Main()
        {
            try
            {
                SmokeTestRegistration.RunAll();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL registered smoke phase: " + ex.GetType().FullName + ": " + ex.Message);
                if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                    Console.Error.WriteLine(ex.StackTrace);
                return 1;
            }

            Test("PolylineMetrics rectangle", PolylineRectangle);
            Test("UnitScale mm length", UnitScaleLength);
            Test("ProjectUnitPolicy", ProjectUnitPolicyTest);
            Test("Geometry tolerance", GeometryTolerance);
            Test("QuantityEngine length", QuantityLength);
            Test("Formula variables", FormulaVariables);
            Test("Formula functions", FormulaFunctions);
            Test("Formula division by zero", FormulaDivisionByZero);
            Test("Quantity rule engine", QuantityRule);
            Test("Rebar count parser", RebarCountParser);
            Test("Rebar spacing parser", RebarSpacingParser);
            Test("Rebar compound parser", RebarCompoundParser);
            Test("Rebar multiplied parser", RebarMultipliedParser);
            Test("Rebar weight", RebarWeightCalculation);
            Test("Quantity report grouping", QuantityGrouping);
            Test("Quantity totals", QuantityTotals);
            Test("Wall quantity with opening", WallQuantityWithOpening);
            Test("Wall semantic regenerator", WallSemanticRegenerator);
            Test("Opening semantic regenerator", OpeningSemanticRegenerator);
            Test("Room finish generator", RoomFinishGeneration);
            Test("Dependency transitive", DependencyTransitive);
            Test("Dependency cycle guard", DependencyCycleGuard);
            Test("Regeneration dirty propagation", RegenerationDirtyPropagation);
            Test("Bulk edit", BulkEdit);
            Test("Model health host/orphan", ModelHealthHostOrphan);
            Test("Revision compare", RevisionCompare);
            Test("QSDB roundtrip", QsdbRoundtrip);
            Test("Project file lock", ProjectLock);
            Test("XLSX exporter package", XlsxExporterPackage);
            Console.WriteLine(_failed == 0 ? "ALL PASS" : $"FAILED: {_failed}");
            return _failed == 0 ? 0 : 1;
        }

        private static void PolylineRectangle()
        {
            var points = new[] { new Point2(0,0), new Point2(5000,0), new Point2(5000,3000), new Point2(0,3000) };
            Near(16000, PolylineMetrics.Length(points,true),1e-9);
            Near(15000000, PolylineMetrics.Area(points),1e-9);
        }

        private static void UnitScaleLength() => Near(5, UnitScale.ToMeters(5000, DrawingUnit.Millimeter), 1e-12);
        private static void ProjectUnitPolicyTest()
        {
            var units = new ProjectUnitPolicy(LengthUnit.Millimeter, 3);
            Near(5, units.ToMeters(5000), 1e-12);
            Near(15, units.AreaToSquareMeters(15000000), 1e-12);
            Near(1, units.VolumeToCubicMeters(1000000000), 1e-12);
            Near(1.235, units.RoundForDisplay(1.2346), 1e-12);
        }
        private static void GeometryTolerance()
        {
            var t = new GeometryTolerancePolicy();
            True(t.NearlyEqual(1, 1.0004));
            True(t.CanAutoClose(0.0015));
            True(!t.CanAutoClose(0.01));
        }
        private static void QuantityLength()
        {
            var entity = new EntitySnapshot("A1","Line","KT") { LengthDrawingUnits = 2500 };
            var result = QuantityEngine.Calculate(entity, TakeoffKind.Length, DrawingUnit.Millimeter);
            Near(2.5,result.Value,1e-12); Equal("m",result.Unit);
        }
        private static void FormulaVariables()
        {
            var evaluator = new ExpressionEvaluator();
            var vars = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase) { ["Width"] = .4, ["Height"] = .6, ["Length"] = 5.35, ["Count"] = 12 };
            Near(15.408,evaluator.Evaluate("Width*Height*Length*Count",vars),1e-12);
        }
        private static void FormulaFunctions()
        {
            var evaluator = new ExpressionEvaluator();
            Near(3.14,evaluator.Evaluate("round(max(2.1, 3.14159), 2)"),1e-12);
            Near(2,evaluator.Evaluate("floor(abs(-2.9))"),1e-12);
        }
        private static void FormulaDivisionByZero() => Throws<InvalidOperationException>(() => new ExpressionEvaluator().Evaluate("1/(2-2)"));
        private static void QuantityRule()
        {
            var project = NewProject();
            var family = new ProjectFamily("wall", "Tường 200", ElementCategory.ArchitecturalWall); project.Families.Add(family);
            var element = new ProjectElement("W1", ElementCategory.ArchitecturalWall, family.Id, "floor-0", "zone-1");
            var rule = new QuantityRule("wall-volume", ElementCategory.ArchitecturalWall, "NetVolumeM3", "Length*Height*Thickness", "1");
            new QuantityRuleEngine().Apply(element, rule, new Dictionary<string,double> { ["Length"] = 5, ["Height"] = 3, ["Thickness"] = .2 });
            Near(3, element.Quantities["NetVolumeM3"], 1e-12);
            Equal("wall-volume@1", element.Properties["Rule:NetVolumeM3"]);
        }
        private static void RebarCountParser() { var g=RebarNotationParser.Parse("4Ø20"); Equal(1,g.Count); Equal(4,g[0].Quantity!.Value); Near(20,g[0].DiameterMm,1e-12); }
        private static void RebarSpacingParser() { var g=RebarNotationParser.Parse("D8@150"); Equal(1,g.Count); Near(8,g[0].DiameterMm,1e-12); Near(150,g[0].SpacingMm!.Value,1e-12); }
        private static void RebarCompoundParser() { var g=RebarNotationParser.Parse("2Ø18+2D20"); Equal(2,g.Count); Equal(2,g[0].Quantity!.Value); Equal(2,g[1].Quantity!.Value); }
        private static void RebarMultipliedParser() { var g=RebarNotationParser.Parse("3x4Ø16"); Equal(12,g[0].Quantity!.Value); Equal(3,g[0].Sets!.Value); Equal(4,g[0].BarsPerSet!.Value); }
        private static void RebarWeightCalculation() => Near(20d*20d/162d, RebarWeight.KilogramsPerMeter(20),1e-12);

        private static IReadOnlyList<QuantityReportRow> BuildRows()
        {
            var family = new FamilyDefinition("Tường Gạch-1",ElementCategory.ArchitecturalWall,"Gạch");
            var a=new ElementInstance("A",family,"Nền 0.00"){LengthM=5,GrossConcreteM3=1.2,DeductionM3=.1,DoorAreaM2=2};
            var b=new ElementInstance("B",family,"Nền 0.00"){LengthM=6,GrossConcreteM3=1.3,DeductionM3=.2,DoorAreaM2=1.8};
            return QuantityReportBuilder.Group(new[]{a,b});
        }
        private static void QuantityGrouping() { var r=BuildRows(); Equal(1,r.Count); Equal(2,r[0].Count); Near(11,r[0].LengthM,1e-12); Near(2.2,r[0].NetConcreteM3,1e-12); }
        private static void QuantityTotals() { var t=QuantityReportTotals.FromRows(BuildRows()); Equal(2,t.Count); Near(11,t.LengthM,1e-12); Near(3.8,t.DoorAreaM2,1e-12); }
        private static void WallQuantityWithOpening()
        {
            var q=WallQuantityCalculator.Calculate(5,3,.11,new[]{new OpeningCut{WidthM=.9,HeightM=2.2}});
            Near(15,q.GrossAreaM2,1e-12); Near(1.98,q.OpeningAreaM2,1e-12); Near(13.02,q.NetAreaM2,1e-12); Near(1.4322,q.NetVolumeM3,1e-12);
        }
        private static void WallSemanticRegenerator()
        {
            var project = NewProject();
            var wall = new ProjectElement("W1", ElementCategory.ArchitecturalWall, "wall", "floor-0", "zone-1");
            wall.Properties["LengthM"]="5"; wall.Properties["HeightM"]="3"; wall.Properties["ThicknessM"]="0.2"; wall.Properties["OpeningAreaM2"]="1.98";
            new WallRegenerator().Regenerate(project, wall);
            Near(15, wall.Quantities["GrossWallAreaM2"], 1e-12); Near(13.02, wall.Quantities["NetWallAreaM2"], 1e-12); Near(2.604, wall.Quantities["NetVolumeM3"], 1e-12);
        }
        private static void OpeningSemanticRegenerator()
        {
            var opening = new ProjectElement("O1", ElementCategory.WallOpening, "opening", "floor-0", "zone-1");
            opening.Properties["WidthM"]="0.9"; opening.Properties["HeightM"]="2.2";
            new OpeningRegenerator().Regenerate(NewProject(), opening);
            Near(1.98, opening.Quantities["OpeningAreaM2"], 1e-12); Near(1, opening.Quantities["Count"], 1e-12);
        }
        private static void RoomFinishGeneration()
        {
            var family=new FamilyDefinition("Phòng-1",ElementCategory.Room);
            var room=new ElementInstance("R1",family,"Tầng trệt"){AreaM2=20,InnerPerimeterM=18,SideAreaM2=48}; room.SourceHandles.Add("AB12");
            var g=RoomFinishGenerator.Generate(room,new RoomPropertySet()); Equal(5,g.Count); Equal(ElementCategory.FloorFinish,g[0].Family.Category); Near(20,g[0].AreaM2,1e-12); Equal("AB12",g[0].SourceHandles[0]);
        }
        private static void DependencyTransitive()
        {
            var wall = new ProjectElement("W", ElementCategory.ArchitecturalWall, "", "f", "z");
            var opening = new ProjectElement("O", ElementCategory.WallOpening, "", "f", "z"); opening.DependsOn.Add("W");
            var door = new ProjectElement("D", ElementCategory.Door, "", "f", "z"); door.DependsOn.Add("O");
            var graph = new DependencyGraph(); graph.Rebuild(new[]{wall,opening,door});
            var dependents = graph.GetDependentsTransitive("W"); Equal(2, dependents.Count); True(dependents.Contains("O")); True(dependents.Contains("D"));
        }
        private static void DependencyCycleGuard()
        {
            var a = new ProjectElement("A", ElementCategory.Room, "", "f", "z"); var b = new ProjectElement("B", ElementCategory.Room, "", "f", "z");
            a.DependsOn.Add("B"); b.DependsOn.Add("A");
            Throws<InvalidOperationException>(() => new DependencyGraph().TopologicalDirtyOrder(new[]{a,b}));
        }
        private static void RegenerationDirtyPropagation()
        {
            var project = NewProject();
            var wall = new ProjectElement("W", ElementCategory.ArchitecturalWall, "", "floor-0", "zone-1"); wall.MarkClean(ElementDirtyFlags.All);
            var opening = new ProjectElement("O", ElementCategory.WallOpening, "", "floor-0", "zone-1"); opening.DependsOn.Add("W"); opening.MarkClean(ElementDirtyFlags.All);
            project.Elements.Add(wall); project.Elements.Add(opening);
            var engine = new RegenerationEngine(new DependencyGraph(), Array.Empty<IElementRegenerator>()); engine.MarkChanged(project,"W",ElementDirtyFlags.Geometry);
            True((wall.Dirty & ElementDirtyFlags.Geometry) != 0); True((opening.Dirty & ElementDirtyFlags.Quantity) != 0);
        }
        private static void BulkEdit()
        {
            var p=NewProject(); var f=new ProjectFamily("r","Room",ElementCategory.Room); p.Families.Add(f);
            var a=new ProjectElement("A",ElementCategory.Room,"r","floor-0","zone-1"); var b=new ProjectElement("B",ElementCategory.Room,"r","floor-0","zone-1"); p.Elements.Add(a);p.Elements.Add(b);
            Equal(2,new BulkEditService().SetProperty(p,new[]{"A","B"},"Material","Paint")); Equal("Paint",a.Properties["Material"]); Equal("Paint",b.Properties["Material"]);
        }
        private static void ModelHealthHostOrphan()
        {
            var p=NewProject(); var opening=new ProjectElement("O",ElementCategory.WallOpening,"","floor-0","zone-1"); opening.SourceHandles.Add("AB12"); p.Elements.Add(opening);
            var issues=new ModelHealthService().Inspect(p,new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            True(issues.Any(x=>x.Code=="MISSING_HOST")); True(issues.Any(x=>x.Code=="ORPHAN_HANDLE"));
        }
        private static void RevisionCompare()
        {
            var service=new RevisionService(); var p=NewProject(); var a=new ProjectElement("A",ElementCategory.Room,"","floor-0","zone-1"); a.SetQuantity("AreaM2",20); a.MarkClean(ElementDirtyFlags.All); p.Elements.Add(a);
            var before=service.Capture(p,"R1"); a.SetQuantity("AreaM2",21); var after=service.Capture(p,"R2");
            var diff=service.Compare(before,after); Equal(1,diff.Count); Equal("Changed",diff[0].Change);
        }
        private static void QsdbRoundtrip()
        {
            var path=Path.Combine(Path.GetTempPath(),"qs3d-project-"+Guid.NewGuid().ToString("N")+".qsdb");
            try
            {
                var p=NewProject(); p.DrawingPath="sample.dwg"; p.DrawingFingerprint="fingerprint";
                var f=new ProjectFamily("wall","Tường 200",ElementCategory.ArchitecturalWall); f.Properties["ThicknessM"]="0.2"; p.Families.Add(f);
                var e=new ProjectElement("W1",ElementCategory.ArchitecturalWall,f.Id,"floor-0","zone-1"); e.SourceHandles.Add("AB12"); e.DependsOn.Add("HOST"); e.Properties["LengthM"]="5"; e.SetQuantity("NetVolumeM3",3); e.MarkClean(ElementDirtyFlags.All); p.Elements.Add(e);
                var store=new QsdbProjectStore(); store.Save(p,path); var loaded=store.Load(path);
                Equal(p.ProjectId,loaded.ProjectId); Equal(1,loaded.Zones.Count); Equal(1,loaded.Floors.Count); Equal(1,loaded.Families.Count); Equal(1,loaded.Elements.Count); Equal("AB12",loaded.Elements[0].SourceHandles[0]); Near(3,loaded.Elements[0].Quantities["NetVolumeM3"],1e-12);
            }
            finally { SafeDelete(path); SafeDelete(path+".bak"); SafeDelete(path+".tmp"); }
        }
        private static void ProjectLock()
        {
            var path=Path.Combine(Path.GetTempPath(),"qs3d-lock-"+Guid.NewGuid().ToString("N")+".qsdb");
            try { using(var first=ProjectFileLock.Acquire(path)) Throws<InvalidOperationException>(()=>{ using var second=ProjectFileLock.Acquire(path); }); }
            finally { SafeDelete(path+".lock"); }
        }
        private static void XlsxExporterPackage()
        {
            var path=Path.Combine(Path.GetTempPath(),"qs3d-smoke-"+Guid.NewGuid().ToString("N")+".xlsx");
            try { XlsxQuantityExporter.Export(path,BuildRows()); using(var a=ZipFile.OpenRead(path)){RequireEntry(a,"[Content_Types].xml");RequireEntry(a,"xl/workbook.xml");RequireEntry(a,"xl/worksheets/sheet1.xml");} }
            finally { SafeDelete(path); }
        }

        private static ProjectState NewProject()
        {
            var p=new ProjectState(Guid.NewGuid().ToString("N"),"Test"); p.Zones.Add(new ZoneDefinition("zone-1","Vùng-1")); p.Floors.Add(new FloorDefinition("floor-0","Nền 0.00",0)); p.ActiveZoneId="zone-1"; p.ActiveFloorId="floor-0"; return p;
        }
        private static void SafeDelete(string path){ try{if(File.Exists(path))File.Delete(path);}catch{} }
        private static void RequireEntry(ZipArchive archive,string name){ if(archive.GetEntry(name)==null) throw new Exception("Missing XLSX entry: "+name); }
        private static void Test(string name,Action action){ try{action();Console.WriteLine("PASS "+name);}catch(Exception ex){_failed++;Console.Error.WriteLine("FAIL "+name+": "+ex.Message);} }
        private static void Near(double expected,double actual,double tolerance){ if(Math.Abs(expected-actual)>tolerance) throw new Exception($"Expected {expected}, got {actual}."); }
        private static void Equal<T>(T expected,T actual){ if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}."); }
        private static void True(bool value){ if(!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T:Exception { try{action();}catch(T){return;} throw new Exception("Expected exception "+typeof(T).Name+"."); }
    }
}