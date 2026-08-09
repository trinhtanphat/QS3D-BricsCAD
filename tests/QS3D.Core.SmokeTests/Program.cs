using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using QS3D.Core.Domain;
using QS3D.Core.Export;
using QS3D.Core.Formulas;
using QS3D.Core.Geometry;
using QS3D.Core.Model;
using QS3D.Core.Rebar;
using QS3D.Core.Reporting;
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
            Test("PolylineMetrics rectangle", PolylineRectangle); Test("UnitScale mm length", UnitScaleLength); Test("QuantityEngine length", QuantityLength);
            Test("Formula variables", FormulaVariables); Test("Formula functions", FormulaFunctions); Test("Formula division by zero", FormulaDivisionByZero);
            Test("Rebar count parser", RebarCountParser); Test("Rebar spacing parser", RebarSpacingParser); Test("Rebar compound parser", RebarCompoundParser); Test("Rebar multiplied parser", RebarMultipliedParser); Test("Rebar weight", RebarWeightCalculation);
            Test("Quantity report grouping", QuantityGrouping); Test("Quantity totals", QuantityTotals); Test("Wall quantity with opening", WallQuantityWithOpening); Test("Room finish generator", RoomFinishGeneration); Test("XLSX exporter package", XlsxExporterPackage);
            Console.WriteLine(_failed == 0 ? "ALL PASS" : $"FAILED: {_failed}"); return _failed == 0 ? 0 : 1;
        }
        private static void PolylineRectangle() { var points = new[] { new Point2(0,0), new Point2(5000,0), new Point2(5000,3000), new Point2(0,3000) }; Near(16000, PolylineMetrics.Length(points,true),1e-9); Near(15000000, PolylineMetrics.Area(points),1e-9); }
        private static void UnitScaleLength() => Near(5, UnitScale.ToMeters(5000, DrawingUnit.Millimeter), 1e-12);
        private static void QuantityLength() { var entity = new EntitySnapshot("A1","Line","KT") { LengthDrawingUnits = 2500 }; var result = QuantityEngine.Calculate(entity, TakeoffKind.Length, DrawingUnit.Millimeter); Near(2.5,result.Value,1e-12); Equal("m",result.Unit); }
        private static void FormulaVariables() { var evaluator = new ExpressionEvaluator(); var vars = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase) { ["Width"] = .4, ["Height"] = .6, ["Length"] = 5.35, ["Count"] = 12 }; Near(15.408,evaluator.Evaluate("Width*Height*Length*Count",vars),1e-12); }
        private static void FormulaFunctions() { var evaluator = new ExpressionEvaluator(); Near(3.14,evaluator.Evaluate("round(max(2.1, 3.14159), 2)"),1e-12); Near(2,evaluator.Evaluate("floor(abs(-2.9))"),1e-12); }
        private static void FormulaDivisionByZero() => Throws<InvalidOperationException>(() => new ExpressionEvaluator().Evaluate("1/(2-2)"));
        private static void RebarCountParser() { var g=RebarNotationParser.Parse("4Ø20"); Equal(1,g.Count); Equal(4,g[0].Quantity!.Value); Near(20,g[0].DiameterMm,1e-12); }
        private static void RebarSpacingParser() { var g=RebarNotationParser.Parse("D8@150"); Equal(1,g.Count); Near(8,g[0].DiameterMm,1e-12); Near(150,g[0].SpacingMm!.Value,1e-12); }
        private static void RebarCompoundParser() { var g=RebarNotationParser.Parse("2Ø18+2D20"); Equal(2,g.Count); Equal(2,g[0].Quantity!.Value); Equal(2,g[1].Quantity!.Value); }
        private static void RebarMultipliedParser() { var g=RebarNotationParser.Parse("3x4Ø16"); Equal(12,g[0].Quantity!.Value); Equal(3,g[0].Sets!.Value); Equal(4,g[0].BarsPerSet!.Value); }
        private static void RebarWeightCalculation() => Near(20d*20d/162d, RebarWeight.KilogramsPerMeter(20),1e-12);
        private static IReadOnlyList<QuantityReportRow> BuildRows() { var family = new FamilyDefinition("Tường Gạch-1",ElementCategory.ArchitecturalWall,"Gạch"); var a=new ElementInstance("A",family,"Nền 0.00"){LengthM=5,GrossConcreteM3=1.2,DeductionM3=.1,DoorAreaM2=2}; var b=new ElementInstance("B",family,"Nền 0.00"){LengthM=6,GrossConcreteM3=1.3,DeductionM3=.2,DoorAreaM2=1.8}; return QuantityReportBuilder.Group(new[]{a,b}); }
        private static void QuantityGrouping() { var r=BuildRows(); Equal(1,r.Count); Equal(2,r[0].Count); Near(11,r[0].LengthM,1e-12); Near(2.2,r[0].NetConcreteM3,1e-12); }
        private static void QuantityTotals() { var t=QuantityReportTotals.FromRows(BuildRows()); Equal(2,t.Count); Near(11,t.LengthM,1e-12); Near(3.8,t.DoorAreaM2,1e-12); }
        private static void WallQuantityWithOpening() { var q=WallQuantityCalculator.Calculate(5,3,.11,new[]{new OpeningCut{WidthM=.9,HeightM=2.2}}); Near(15,q.GrossAreaM2,1e-12); Near(1.98,q.OpeningAreaM2,1e-12); Near(13.02,q.NetAreaM2,1e-12); Near(1.4322,q.NetVolumeM3,1e-12); }
        private static void RoomFinishGeneration() { var family=new FamilyDefinition("Phòng-1",ElementCategory.Room); var room=new ElementInstance("R1",family,"Tầng trệt"){AreaM2=20,InnerPerimeterM=18,SideAreaM2=48}; room.SourceHandles.Add("AB12"); var g=RoomFinishGenerator.Generate(room,new RoomPropertySet()); Equal(5,g.Count); Equal(ElementCategory.FloorFinish,g[0].Family.Category); Near(20,g[0].AreaM2,1e-12); Equal("AB12",g[0].SourceHandles[0]); }
        private static void XlsxExporterPackage() { var path=Path.Combine(Path.GetTempPath(),"qs3d-smoke-"+Guid.NewGuid().ToString("N")+".xlsx"); try { XlsxQuantityExporter.Export(path,BuildRows()); using(var a=ZipFile.OpenRead(path)){RequireEntry(a,"[Content_Types].xml");RequireEntry(a,"xl/workbook.xml");RequireEntry(a,"xl/worksheets/sheet1.xml");} } finally { if(File.Exists(path)) File.Delete(path); } }
        private static void RequireEntry(ZipArchive archive,string name){ if(archive.GetEntry(name)==null) throw new Exception("Missing XLSX entry: "+name); }
        private static void Test(string name,Action action){ try{action();Console.WriteLine("PASS "+name);}catch(Exception ex){_failed++;Console.Error.WriteLine("FAIL "+name+": "+ex.Message);} }
        private static void Near(double expected,double actual,double tolerance){ if(Math.Abs(expected-actual)>tolerance) throw new Exception($"Expected {expected}, got {actual}."); }
        private static void Equal<T>(T expected,T actual){ if(!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception($"Expected {expected}, got {actual}."); }
        private static void Throws<T>(Action action) where T:Exception { try{action();}catch(T){return;} throw new Exception("Expected exception "+typeof(T).Name+"."); }
    }
}
