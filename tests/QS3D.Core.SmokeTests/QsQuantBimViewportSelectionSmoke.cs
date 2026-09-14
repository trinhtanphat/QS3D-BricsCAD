using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimViewportSelectionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var session = QuantBimStandaloneIfcSession.Parse("viewport-selection.ifc", Ifc());
            var workflow = new QuantBimStandaloneViewportSelection(session);
            var camera = new IfcCameraNavigationState(
                0.0, -5.0, 1.5,
                0.0, 0.0, 1.5,
                0.0, 0.0, 1.0,
                5.0, 0.1, 100.0);

            var hit = workflow.Pick(camera, 800.0, 600.0, 400.0, 300.0, 60.0);
            True(hit.HasHit, "center viewport hit");
            Equal("G1", hit.Hit!.Guid, "picked guid");
            Equal("ifc-step://#10", hit.Hit.GeometryReference, "picked geometry reference");
            Equal(1, hit.Selection.Guids.Count, "single selection");
            Equal("G1", hit.Selection.Guids[0], "selection identity");
            Equal("G1", hit.Properties[0].Value, "property projection");
            Equal(0, hit.Takeoff.Count, "qto projection");
            Equal(0, hit.Boq.Count, "boq projection");

            var missCamera = new IfcCameraNavigationState(
                10.0, -5.0, 1.5,
                10.0, 0.0, 1.5,
                0.0, 0.0, 1.0,
                5.0, 0.1, 100.0);
            var miss = workflow.Pick(missCamera, 800.0, 600.0, 400.0, 300.0, 60.0);
            True(!miss.HasHit, "viewport miss");
            Equal(0, miss.Selection.Guids.Count, "miss selection empty");
            Equal(0, miss.Properties.Count, "miss properties empty");
            Equal(0, miss.Takeoff.Count, "miss qto empty");
            Equal(0, miss.Boq.Count, "miss boq empty");
        }

        private static string Ifc()
        {
            return "ISO-10303-21;\nHEADER;\nENDSEC;\nDATA;\n" +
                "#10=IFCWALL('G1',$,'Wall',$,$,$,#110,$);\n" +
                "#110=IFCPRODUCTDEFINITIONSHAPE($,$,(#111));\n" +
                "#111=IFCSHAPEREPRESENTATION(#120,'Body','SweptSolid',(#112));\n" +
                "#112=IFCEXTRUDEDAREASOLID(#113,#114,#115,3.0);\n" +
                "#113=IFCRECTANGLEPROFILEDEF(.AREA.,$,$,2.0,0.2);\n" +
                "#114=IFCAXIS2PLACEMENT3D(#116,$,$);\n" +
                "#115=IFCDIRECTION((0.,0.,1.));\n" +
                "#116=IFCCARTESIANPOINT((0.,0.,0.));\n" +
                "#120=IFCGEOMETRICREPRESENTATIONSUBCONTEXT('Body','Model',*,*,*,*,#121,$,.MODEL_VIEW.,$);\n" +
                "#121=IFCGEOMETRICREPRESENTATIONCONTEXT($,'Model',3,1.E-05,#122,$);\n" +
                "#122=IFCAXIS2PLACEMENT3D(#123,$,$);\n" +
                "#123=IFCCARTESIANPOINT((0.,0.,0.));\n" +
                "ENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
