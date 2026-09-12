using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimEntityCoverageSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var source = new IfcStepStandaloneSource();
            var document = source.Parse("entity-coverage.ifc", Step());
            var expected = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["G-FOOTING"] = "IfcFooting",
                ["G-PILE"] = "IfcPile",
                ["G-ROOF"] = "IfcRoof",
                ["G-CURTAIN"] = "IfcCurtainWall",
                ["G-MEMBER"] = "IfcMember",
                ["G-PLATE"] = "IfcPlate"
            };

            Equal(expected.Count, document.Elements.Count, "bounded entity count");
            foreach (var pair in expected)
            {
                var element = document.Elements.Single(x => x.Guid == pair.Key);
                Equal(pair.Value, element.Entity, pair.Key + " canonical entity");
                Equal("L-QS", element.Storey, pair.Key + " storey");
                Equal("QS.ELEMENT", element.Classification, pair.Key + " classification");
                Equal("ifc-step://#" + IdFor(pair.Key), element.GeometryReference, pair.Key + " evidence locator");
                Near(2.5d, element.Quantities.Single(x => x.QuantityName == "NetVolume").Quantity, 1e-12, pair.Key + " quantity");
                Equal("m3", element.Quantities.Single().Unit, pair.Key + " canonical quantity unit");
            }

            True(document.Elements.All(x => x.Guid != "G-UNSUPPORTED"), "unsupported entity stays outside bounded parser scope");

            var workbench = new QuantBimStandaloneWorkbench(source);
            var selection = new IfcSelectionSet("QS structural/envelope", expected.Keys);
            var takeoff = workbench.Takeoff(document, selection);
            Equal(expected.Count, takeoff.Count, "workbench takeoff count");
            var boq = workbench.BuildBoq(takeoff);
            Near(15d, boq.Single(x => x.Classification == "QS.ELEMENT" && x.Unit == "m3").Quantity, 1e-12, "downstream BOQ aggregation");

            var evidence = new QuantBimTraceableTakeoffEngine().Build(document, selection);
            Equal(expected.Count, evidence.Count, "traceable evidence count");
            True(evidence.All(x => x.GeometryReference.StartsWith("ifc-step://#", StringComparison.Ordinal)), "traceable source identity retained");
        }

        private static string IdFor(string guid)
        {
            switch (guid)
            {
                case "G-FOOTING": return "10";
                case "G-PILE": return "11";
                case "G-ROOF": return "12";
                case "G-CURTAIN": return "13";
                case "G-MEMBER": return "14";
                case "G-PLATE": return "15";
                default: throw new InvalidOperationException("Unexpected test GUID " + guid + ".");
            }
        }

        private static string Step()
        {
            return "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC4'));\nENDSEC;\nDATA;\n" +
                "#10=IFCFOOTING('G-FOOTING',$,'Footing',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#11=IFCPILE('G-PILE',$,'Pile',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#12=IFCROOF('G-ROOF',$,'Roof',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#13=IFCCURTAINWALL('G-CURTAIN',$,'Curtain',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#14=IFCMEMBER('G-MEMBER',$,'Member',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#15=IFCPLATE('G-PLATE',$,'Plate',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#16=IFCFURNISHINGELEMENT('G-UNSUPPORTED',$,'Furniture',$,$,$,$,$,.NOTDEFINED.);\n" +
                "#20=IFCBUILDINGSTOREY('S-QS',$,'L-QS',$,$,$,$,$,$,.ELEMENT.,0.);\n" +
                "#21=IFCRELCONTAINEDINSPATIALSTRUCTURE('R-SPATIAL',$,$,$,(#10,#11,#12,#13,#14,#15,#16),#20);\n" +
                "#30=IFCQUANTITYVOLUME('NetVolume',$,$,2.5,$);\n" +
                "#31=IFCELEMENTQUANTITY('Q-QS',$,'BaseQuantities',$,$,(#30));\n" +
                "#32=IFCRELDEFINESBYPROPERTIES('R-QTO',$,$,$,(#10,#11,#12,#13,#14,#15,#16),#31);\n" +
                "#40=IFCCLASSIFICATIONREFERENCE($,'QS.ELEMENT','QS element',$);\n" +
                "#41=IFCRELASSOCIATESCLASSIFICATION('R-CLASS',$,$,$,(#10,#11,#12,#13,#14,#15,#16),#40);\n" +
                "ENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
