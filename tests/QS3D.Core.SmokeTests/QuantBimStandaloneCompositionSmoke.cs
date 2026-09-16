using System;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QuantBimStandaloneCompositionSmoke
    {
        internal static void Run()
        {
            const string step = "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC4'));\nENDSEC;\nDATA;\n" +
                "#1=IFCPROJECT('P1',$,'Project',$,$,$,$,$,$);\n" +
                "#10=IFCWALL('G1',$,'Wall-01',$,$,$,#100,$);\n" +
                "#20=IFCBUILDINGSTOREY('S1',$,'L01',$,$,$,$,$,$,.ELEMENT.,0.);\n" +
                "#21=IFCRELCONTAINEDINSPATIALSTRUCTURE('R-SPATIAL',$,$,$,(#10),#20);\n" +
                "#30=IFCWALLTYPE('T1',$,'External',$,$,$,$,$,$,.NOTDEFINED.);\n" +
                "#31=IFCRELDEFINESBYTYPE('R-TYPE',$,$,$,(#10),#30);\n" +
                "#40=IFCPROPERTYSINGLEVALUE('IsExternal',$,IFCBOOLEAN(.T.),$);\n" +
                "#41=IFCPROPERTYSET('PS1',$,'Pset_WallCommon',$,(#40));\n" +
                "#42=IFCRELDEFINESBYPROPERTIES('R-PSET',$,$,$,(#10),#41);\n" +
                "#50=IFCQUANTITYAREA('NetSideArea',$,$,12.0,$);\n" +
                "#51=IFCELEMENTQUANTITY('Q1',$,'BaseQuantities',$,$,(#50));\n" +
                "#52=IFCRELDEFINESBYPROPERTIES('R-QTO',$,$,$,(#10),#51);\n" +
                "#60=IFCCLASSIFICATIONREFERENCE($,'ARC.WALL','Wall',$);\n" +
                "#61=IFCRELASSOCIATESCLASSIFICATION('R-CLASS',$,$,$,(#10),#60);\n" +
                "#100=IFCPRODUCTDEFINITIONSHAPE($,$,(#101));\n" +
                "#101=IFCSHAPEREPRESENTATION($,'Body','SweptSolid',(#102));\n" +
                "#102=IFCEXTRUDEDAREASOLID(#103,#104,#106,3.0);\n" +
                "#103=IFCRECTANGLEPROFILEDEF(.AREA.,$,$,4.0,0.2);\n" +
                "#104=IFCAXIS2PLACEMENT3D(#105,$,$);\n" +
                "#105=IFCCARTESIANPOINT((0.0,0.0,0.0));\n" +
                "#106=IFCDIRECTION((0.0,0.0,1.0));\nENDSEC;\nEND-ISO-10303-21;\n";

            var session = QuantBimStandaloneIfcSession.Parse("standalone.ifc", step);
            var filtered = session.Filter(new IfcWorkbenchFilter("IfcWall", "L01", "External", "ARC.WALL"));
            Equal(1, filtered.Count, "standalone filter");
            var wall = filtered.Single();
            True(session.PropertyTree(wall.Guid).Any(x => x.Name == "Pset_WallCommon.IsExternal"), "property tree");

            var selection = new IfcSelectionSet("External walls", new[] { wall.Guid });
            var qto = session.Takeoff(selection);
            Equal(1, qto.Count, "standalone QTO");
            Near(12d, qto.Single().Quantity, "standalone QTO quantity");
            var boq = session.BuildBoq(selection);
            Near(12d, boq.Single(x => x.Classification == "ARC.WALL" && x.Unit == "m2").Quantity, "standalone BOQ");
            True(session.ExportCsv(selection).Contains("ARC.WALL,m2,12"), "standalone interchange export");

            var scene = session.BuildScene(selection);
            Equal(session.Revision, scene.Revision, "scene generation revision");
            Equal(1, scene.Nodes.Count, "scene node count");
            True(scene.Nodes.Single().Selected, "scene selection");
            Equal(8, scene.Nodes.Single().Mesh.Vertices.Count, "scene mesh vertices");
            Equal(36, scene.Nodes.Single().Mesh.TriangleIndices.Count, "scene mesh triangles");
            Equal(IfcViewCommandKind.FocusSelection, scene.Navigation.Last().Kind, "scene navigation");

            var state = new QuantBimWorkbenchStateCoordinator().Capture(
                session.Document,
                new IfcWorkbenchFilter("IfcWall", "L01", "External", "ARC.WALL"),
                selection);
            Equal(session.Revision, state.Revision, "workbench state revision");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-12) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }
    }
}
