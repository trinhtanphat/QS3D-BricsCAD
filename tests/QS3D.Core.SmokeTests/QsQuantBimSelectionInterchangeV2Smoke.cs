using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimSelectionInterchangeV2Smoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var session = QuantBimStandaloneIfcSession.Parse("selection-interchange-v2.ifc", Ifc());
            var bundle = new QuantBimSelectionExportPublisher().Publish(
                session,
                new IfcSelectionSet("Review set", new[] { "G1" }));

            var encoded = QuantBimSelectionInterchangeV2Codec.Encode(bundle);
            var decoded = QuantBimSelectionInterchangeV2Codec.Decode(encoded);
            Equal(bundle.DocumentPath, decoded.DocumentPath, "document path");
            Equal(bundle.Revision, decoded.Revision, "revision");
            Equal(bundle.SelectionName, decoded.SelectionName, "selection name");
            Equal("G1", decoded.SelectionGuids[0], "selection guid");
            Equal(bundle.BoqCsv, decoded.BoqCsv, "BOQ payload");
            Equal(bundle.EvidenceCsv, decoded.EvidenceCsv, "evidence payload");

            var package = QuantBimSelectionInterchangeCodec.FromBundle(bundle);
            Equal(encoded, QuantBimSelectionInterchangeV2Codec.Encode(package), "deterministic V2 re-encode");

            var lines = encoded.Split(new[] { '\n' }, StringSplitOptions.None);
            var payloadBytes = Convert.FromBase64String(lines[2].Substring("Payload=".Length));
            var v1 = Encoding.UTF8.GetString(payloadBytes);
            var revisionField = "Revision=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(bundle.Revision));
            var tamperedRevisionField = "Revision=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(bundle.Revision + "-tampered"));
            var metadataTamperedV1 = v1.Replace(revisionField, tamperedRevisionField);
            if (string.Equals(v1, metadataTamperedV1, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM selection interchange V2 smoke failed to construct revision metadata tamper.");
            var metadataTamperedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(metadataTamperedV1));
            var metadataTamperedV2 = lines[0] + "\n" + lines[1] + "\nPayload=" + metadataTamperedPayload + "\n";
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeV2Codec.Decode(metadataTamperedV2), "metadata tamper bound by whole-envelope digest");

            var zeros = new string('0', 64);
            var digestTampered = encoded.Replace(lines[1], "EnvelopeSha256=" + zeros);
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeV2Codec.Decode(digestTampered), "whole-envelope digest tamper");
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeV2Codec.Decode(encoded.Replace("\n", "\r\n")), "non-canonical line endings");
            Expect<InvalidOperationException>(() => QuantBimSelectionInterchangeV2Codec.Decode(encoded.TrimEnd('\n')), "missing terminal newline");
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
                throw new InvalidOperationException("QuantBIM selection interchange V2 smoke failed for " + label + ".");
        }

        private static void Expect<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("QuantBIM selection interchange V2 smoke failed: expected " + typeof(T).Name + " for " + label + ".");
        }
    }
}
