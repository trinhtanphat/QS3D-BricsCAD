using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimSelectionInterchangeV2AdmissionSmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var session = QuantBimStandaloneIfcSession.Parse("generation-bound.ifc", Ifc("G1"));
            var bundle = new QuantBimSelectionExportPublisher().Publish(
                session,
                new IfcSelectionSet("Review set", new[] { "G1" }));
            var encoded = QuantBimSelectionInterchangeV2Codec.Encode(bundle);
            var admitted = new QuantBimSelectionInterchangeV2Admission().Admit(session, encoded);
            Equal("G1", admitted.Selection.Guids.Single(), "canonical selection guid");
            Equal(bundle.Revision, admitted.Package.Revision, "bound revision");

            var staleRevision = QuantBimStandaloneIfcSession.Parse("generation-bound.ifc", Ifc("G2"));
            Expect<InvalidOperationException>(() => new QuantBimSelectionInterchangeV2Admission().Admit(staleRevision, encoded), "stale revision");

            var otherPath = QuantBimStandaloneIfcSession.Parse("other.ifc", Ifc("G1"));
            Expect<InvalidOperationException>(() => new QuantBimSelectionInterchangeV2Admission().Admit(otherPath, encoded), "foreign path");

            var unknownBundle = new QuantBimSelectionExportBundle(
                bundle.DocumentPath,
                bundle.Revision,
                bundle.SelectionName,
                new[] { "UNKNOWN" },
                bundle.Evidence,
                bundle.Boq,
                bundle.BoqCsv,
                bundle.EvidenceCsv);
            var unknownEncoded = QuantBimSelectionInterchangeV2Codec.Encode(unknownBundle);
            Expect<InvalidOperationException>(() => new QuantBimSelectionInterchangeV2Admission().Admit(session, unknownEncoded), "unknown selection guid");

            Expect<System.IO.InvalidDataException>(
                () => QuantBimStandaloneIfcSession.Parse("generation-bound.ifc", Ifc("G1", "G1")),
                "duplicate IFC GlobalId parser invariant");
        }

        private static string Ifc(params string[] guids)
        {
            var rows = string.Join("\n", guids.Select((guid, index) =>
                "#" + (10 + index) + "=IFCWALL('" + guid + "',$,'Wall',$,$,$,$,$);"));
            return "ISO-10303-21;\nHEADER;\nENDSEC;\nDATA;\n" + rows + "\nENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException("QuantBIM V2 admission smoke failed for " + label + ".");
        }

        private static void Expect<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("QuantBIM V2 admission smoke failed: expected " + typeof(T).Name + " for " + label + ".");
        }
    }
}
