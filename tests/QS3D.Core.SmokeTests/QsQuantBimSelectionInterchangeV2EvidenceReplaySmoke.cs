using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsQuantBimSelectionInterchangeV2EvidenceReplaySmoke
    {
        [ModuleInitializer]
        internal static void Run()
        {
            var session = QuantBimStandaloneIfcSession.Parse("replay-bound.ifc", Ifc("G1"));
            var selection = new IfcSelectionSet("Replay set", new[] { "G1" });
            var canonical = new QuantBimSelectionExportPublisher().Publish(session, selection);
            var encoded = QuantBimSelectionInterchangeV2Codec.Encode(canonical);
            var replayed = new QuantBimSelectionInterchangeV2EvidenceReplay().AdmitAndReplay(session, encoded);
            Equal(canonical.BoqCsv, replayed.BoqCsv, "canonical BOQ replay");
            Equal(canonical.EvidenceCsv, replayed.EvidenceCsv, "canonical evidence replay");

            var tampered = new QuantBimSelectionExportBundle(
                canonical.DocumentPath,
                canonical.Revision,
                canonical.SelectionName,
                canonical.SelectionGuids,
                canonical.Evidence,
                canonical.Boq,
                canonical.BoqCsv + "tampered",
                canonical.EvidenceCsv);
            var tamperedEncoded = QuantBimSelectionInterchangeV2Codec.Encode(tampered);
            Expect<InvalidOperationException>(
                () => new QuantBimSelectionInterchangeV2EvidenceReplay().AdmitAndReplay(session, tamperedEncoded),
                "internally valid but semantically tampered BOQ evidence");
        }

        private static string Ifc(params string[] guids)
        {
            var rows = string.Join("\n", guids.Select((guid, index) =>
                "#" + (10 + index) + "=IFCWALL('" + guid + "',$,'Wall',$,$,$,$,$);"));
            return "ISO-10303-21;\nHEADER;\nENDSEC;\nDATA;\n" + rows + "\nENDSEC;\nEND-ISO-10303-21;\n";
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!object.Equals(expected, actual))
                throw new InvalidOperationException("QuantBIM V2 evidence replay smoke failed for " + label + ".");
        }

        private static void Expect<T>(Action action, string label) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new InvalidOperationException("QuantBIM V2 evidence replay smoke failed: expected " + typeof(T).Name + " for " + label + ".");
        }
    }
}
