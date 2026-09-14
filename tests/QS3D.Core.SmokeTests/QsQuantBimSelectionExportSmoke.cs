using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimSelectionExportSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var session = QuantBimStandaloneIfcSession.Parse("selection-export.ifc", Ifc());
            var publisher = new QuantBimSelectionExportPublisher();
            var selection = new IfcSelectionSet("Current viewport", new[] { "G1" });
            var bundle = publisher.Publish(session, selection);

            Equal(session.Path, bundle.DocumentPath, "document path");
            Equal(session.Revision, bundle.Revision, "revision");
            Equal("Current viewport", bundle.SelectionName, "selection name");
            Equal(1, bundle.SelectionGuids.Count, "selection count");
            Equal("G1", bundle.SelectionGuids[0], "selection guid");
            True(bundle.BoqCsv.StartsWith("Classification,Unit,Quantity,SourceCount", StringComparison.Ordinal), "BOQ CSV header");
            True(bundle.EvidenceCsv.StartsWith("DocumentPath,Revision,Guid,Entity,Storey,Classification,QuantityName,Quantity,Unit,GeometryReference", StringComparison.Ordinal), "evidence CSV header");

            var stale = new IfcStandaloneDocument(session.Path, session.Revision + "-stale", session.Document.Elements);
            Expect<InvalidOperationException>(() => publisher.Publish(session, stale, selection), "stale generation");
            Expect<InvalidOperationException>(() => publisher.Publish(session, new IfcSelectionSet("Missing", new[] { "MISSING" })), "missing selected guid");
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
                throw new InvalidOperationException("QuantBIM selection export smoke failed for " + label + ".");
        }

        private static void True(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException("QuantBIM selection export smoke failed: " + label + ".");
        }

        private static void Expect<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("QuantBIM selection export smoke failed: expected " + typeof(T).Name + " for " + label + ".");
        }
    }
}
