using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimIfcSessionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var step = Ifc(3.0);
            var session = QuantBimStandaloneIfcSession.Parse("session.ifc", step);
            var selection = new IfcSelectionSet("Selected", new[] { "G1" });

            Equal("session.ifc", session.Path, "bound path");
            Equal(1, session.Filter(new IfcWorkbenchFilter("IfcWall", "", "", "")).Count, "bound filter");
            Equal("G1", session.PropertyTree("G1")[0].Value, "bound property tree");
            Equal(0, session.Takeoff(selection).Count, "bound qto");

            var scene = session.BuildScene(selection);
            Equal(session.Revision, scene.Revision, "scene revision");
            Equal(1, scene.Nodes.Count, "scene node count");
            Near(3.0, scene.Nodes[0].Mesh.Vertices[4].Z, "scene geometry generation");

            var sameGeneration = new IfcStepStandaloneSource().Parse("session.ifc", step);
            Equal(session.Revision, session.BuildScene(sameGeneration, selection).Revision, "same generation admission");

            Reject("stale revision", delegate
            {
                var stale = new IfcStepStandaloneSource().Parse("session.ifc", Ifc(4.0));
                session.BuildScene(stale, selection);
            });
            Reject("different path", delegate
            {
                var otherPath = new IfcStepStandaloneSource().Parse("other.ifc", step);
                session.BuildScene(otherPath, selection);
            });
            Reject("unknown guid", delegate
            {
                session.BuildScene(new IfcSelectionSet("Unknown", new[] { "MISSING" }));
            });
        }

        private static string Ifc(double depth)
        {
            return "ISO-10303-21;\nHEADER;\nENDSEC;\nDATA;\n" +
                "#10=IFCWALL('G1',$,'Wall',$,$,$,#110,$);\n" +
                "#110=IFCPRODUCTDEFINITIONSHAPE($,$,(#111));\n" +
                "#111=IFCSHAPEREPRESENTATION(#120,'Body','SweptSolid',(#112));\n" +
                "#112=IFCEXTRUDEDAREASOLID(#113,#114,#115," + depth.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");\n" +
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

        private static void Reject(string label, Action action)
        {
            var failed = false;
            try { action(); }
            catch (InvalidOperationException) { failed = true; }
            True(failed, label + " fails closed");
        }

        private static void Near(double expected, double actual, string label)
        {
            if (Math.Abs(expected - actual) > 1e-9) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
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
