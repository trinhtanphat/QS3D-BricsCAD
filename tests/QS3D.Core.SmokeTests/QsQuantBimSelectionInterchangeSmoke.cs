using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimSelectionInterchangeSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var session = QuantBimStandaloneIfcSession.Parse("selection-interchange.ifc", Ifc());
            var bundle = new QuantBimSelectionExportPublisher().Publish(
                session,
                new IfcSelectionSet("Review set", new[] { "G1" }));

            var encoded = QuantBimSelectionInterchangeCodec.Encode(bundle);
            var decoded = QuantBimSelectionInterchangeCodec.Decode(encoded);
            Equal(bundle.DocumentPath, decoded.DocumentPath, "document path");
            Equal(bundle.Revision, decoded.Revision, "revision");
            Equal(bundle.SelectionName, decoded.SelectionName, "selection name");
            Equal(1, decoded.SelectionGuids.Count, "selection count");
            Equal("G1", decoded.SelectionGuids[0], "selection guid");
            Equal(bundle.BoqCsv, decoded.BoqCsv, "BOQ payload");
            Equal(bundle.EvidenceCsv, decoded.EvidenceCsv, "evidence payload");

            var package = QuantBimSelectionInterchangeCodec.FromBundle(bundle);
            Equal(64, package.BoqSha256.Length, "BOQ hash length");
            Equal(64, package.EvidenceSha256.Length, "evidence hash length");
            Equal(encoded, QuantBimSelectionInterchangeCodec.Encode(package), "deterministic re-encode");

            var zeros = new string('0', 64);
            var tampered = encoded.Replace("BoqSha256=" + package.BoqSha256, "BoqSha256=" + zeros);
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeCodec.Decode(tampered), "tampered BOQ hash");
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeCodec.Decode(encoded.Replace("\n", "\r\n")), "non-canonical line endings");
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeCodec.Decode(encoded.TrimEnd('\n')), "missing terminal newline");
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
                throw new InvalidOperationException("QuantBIM selection interchange smoke failed for " + label + ".");
        }

        private static void Expect<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("QuantBIM selection interchange smoke failed: expected " + typeof(T).Name + " for " + label + ".");
        }
    }
}
