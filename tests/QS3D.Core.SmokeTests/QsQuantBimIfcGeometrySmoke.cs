using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimIfcGeometrySmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var step = Ifc(3.0, "#115=IFCDIRECTION((0.,0.,1.));", "$", "$");
            var resolver = new IfcStepGeometryResolver(step);
            var mesh = resolver.Resolve("ifc-step://#10");
            Equal(8, mesh.Vertices.Count, "box vertex count");
            Equal(36, mesh.TriangleIndices.Count, "box triangle index count");
            Near(-1.0, mesh.Vertices[0].X, "half width");
            Near(-0.1, mesh.Vertices[0].Y, "half depth");
            Near(3.0, mesh.Vertices[4].Z, "extrusion height");

            var source = new IfcStepStandaloneSource();
            var document = source.Parse("geometry.ifc", step);
            var scene = new QuantBimStandaloneSceneBuilder(resolver).Build(document, new IfcSelectionSet("Wall", new[] { "G1" }));
            Equal(1, scene.Nodes.Count, "scene node count");
            Equal("G1", scene.Nodes[0].Guid, "scene guid");
            True(scene.Nodes[0].Selected, "selection projection");

            Reject("non-vertical extrusion", Ifc(3.0, "#115=IFCDIRECTION((1.,0.,1.));", "$", "$"));
            Reject("rotated placement axis", Ifc(3.0, "#115=IFCDIRECTION((0.,0.,1.));", "#117", "$"));
            Reject("zero depth", Ifc(0.0, "#115=IFCDIRECTION((0.,0.,1.));", "$", "$"));
            RejectReference("unsupported scheme", resolver, "mesh://#10");
        }

        private static string Ifc(double depth, string direction, string axis, string refDirection)
        {
            return "ISO-10303-21;\nHEADER;\nENDSEC;\nDATA;\n" +
                "#10=IFCWALL('G1',$,'Wall',$,$,$,#110,$);\n" +
                "#110=IFCPRODUCTDEFINITIONSHAPE($,$,(#111));\n" +
                "#111=IFCSHAPEREPRESENTATION(#120,'Body','SweptSolid',(#112));\n" +
                "#112=IFCEXTRUDEDAREASOLID(#113,#114,#115," + depth.ToString(System.Globalization.CultureInfo.InvariantCulture) + ");\n" +
                "#113=IFCRECTANGLEPROFILEDEF(.AREA.,$,$,2.0,0.2);\n" +
                "#114=IFCAXIS2PLACEMENT3D(#116," + axis + "," + refDirection + ");\n" +
                direction + "\n" +
                "#116=IFCCARTESIANPOINT((0.,0.,0.));\n" +
                "#117=IFCDIRECTION((0.,1.,0.));\n" +
                "#120=IFCGEOMETRICREPRESENTATIONSUBCONTEXT('Body','Model',*,*,*,*,#121,$,.MODEL_VIEW.,$);\n" +
                "#121=IFCGEOMETRICREPRESENTATIONCONTEXT($,'Model',3,1.E-05,#122,$);\n" +
                "#122=IFCAXIS2PLACEMENT3D(#123,$,$);\n" +
                "#123=IFCCARTESIANPOINT((0.,0.,0.));\n" +
                "ENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Reject(string label, string step)
        {
            var failed = false;
            try { new IfcStepGeometryResolver(step).Resolve("ifc-step://#10"); }
            catch (InvalidDataException) { failed = true; }
            True(failed, label + " fails closed");
        }

        private static void RejectReference(string label, IIfcGeometryResolver resolver, string reference)
        {
            var failed = false;
            try { resolver.Resolve(reference); }
            catch (InvalidDataException) { failed = true; }
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
