using System;
using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Admits V2 transport/generation identity and then replays the canonical standalone
    /// publication pipeline. Imported BOQ/evidence bytes are accepted only when they match
    /// the exact IFC generation and selected identities currently open in the workbench.
    /// </summary>
    public sealed class QuantBimSelectionInterchangeV2EvidenceReplay
    {
        private readonly QuantBimSelectionInterchangeV2Admission _admission = new QuantBimSelectionInterchangeV2Admission();
        private readonly QuantBimSelectionExportPublisher _publisher = new QuantBimSelectionExportPublisher();

        public QuantBimSelectionExportBundle AdmitAndReplay(QuantBimStandaloneIfcSession session, string encoded)
        {
            if (session == null) throw new ArgumentNullException("session");
            var admitted = _admission.Admit(session, encoded);
            var canonical = _publisher.Publish(session, admitted.Selection);
            RequireEquivalent(admitted.Package, canonical);
            return canonical;
        }

        private static void RequireEquivalent(QuantBimSelectionInterchangePackage imported, QuantBimSelectionExportBundle canonical)
        {
            if (imported == null) throw new ArgumentNullException("imported");
            if (canonical == null) throw new ArgumentNullException("canonical");

            RequireEqual(imported.BoqCsv, canonical.BoqCsv, "BOQ CSV");
            RequireEqual(imported.EvidenceCsv, canonical.EvidenceCsv, "evidence CSV");
            RequireSequence(imported.SelectionGuids, canonical.SelectionGuids, "selection identities");
        }

        private static void RequireEqual(string imported, string canonical, string label)
        {
            if (!string.Equals(imported, canonical, StringComparison.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange imported " + label + " disagrees with canonical standalone IFC replay.");
        }

        private static void RequireSequence(IEnumerable<string> imported, IEnumerable<string> canonical, string label)
        {
            var left = (imported ?? throw new ArgumentNullException("imported"))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray();
            var right = (canonical ?? throw new ArgumentNullException("canonical"))
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ThenBy(x => x, StringComparer.Ordinal).ToArray();
            if (!left.SequenceEqual(right, StringComparer.Ordinal))
                throw new InvalidOperationException("QuantBIM interchange imported " + label + " disagrees with canonical standalone IFC replay.");
        }
    }
}
