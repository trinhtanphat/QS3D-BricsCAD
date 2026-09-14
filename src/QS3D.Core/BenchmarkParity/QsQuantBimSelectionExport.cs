using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.BenchmarkParity
{
    /// <summary>
    /// Immutable renderer-neutral publication packet for one standalone IFC selection.
    /// Every projection is derived from the same bound IFC session generation.
    /// </summary>
    public sealed class QuantBimSelectionExportBundle
    {
        public QuantBimSelectionExportBundle(
            string documentPath,
            string revision,
            string selectionName,
            IEnumerable<string> selectionGuids,
            IEnumerable<QuantBimEvidenceLine> evidence,
            IEnumerable<TakeoffInventoryLine> boq,
            string boqCsv,
            string evidenceCsv)
        {
            DocumentPath = QsModelElementSnapshot.Require(documentPath, "documentPath");
            Revision = QsModelElementSnapshot.Require(revision, "revision");
            SelectionName = QsModelElementSnapshot.Require(selectionName, "selectionName");
            SelectionGuids = new ReadOnlyCollection<string>((selectionGuids ?? throw new ArgumentNullException("selectionGuids"))
                .Select(x => QsModelElementSnapshot.Require(x, "selectionGuid"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x, StringComparer.Ordinal)
                .ToList());
            Evidence = new ReadOnlyCollection<QuantBimEvidenceLine>((evidence ?? throw new ArgumentNullException("evidence")).ToList());
            Boq = new ReadOnlyCollection<TakeoffInventoryLine>((boq ?? throw new ArgumentNullException("boq")).ToList());
            BoqCsv = boqCsv ?? throw new ArgumentNullException("boqCsv");
            EvidenceCsv = evidenceCsv ?? throw new ArgumentNullException("evidenceCsv");
        }

        public string DocumentPath { get; private set; }
        public string Revision { get; private set; }
        public string SelectionName { get; private set; }
        public IReadOnlyList<string> SelectionGuids { get; private set; }
        public IReadOnlyList<QuantBimEvidenceLine> Evidence { get; private set; }
        public IReadOnlyList<TakeoffInventoryLine> Boq { get; private set; }
        public string BoqCsv { get; private set; }
        public string EvidenceCsv { get; private set; }
    }

    /// <summary>
    /// Publishes one deterministic export/interchange snapshot from the exact IFC generation
    /// owned by <see cref="QuantBimStandaloneIfcSession"/>. Scene geometry remains navigation
    /// evidence only; quantity authority stays in the canonical IFC QTO/evidence pipeline.
    /// </summary>
    public sealed class QuantBimSelectionExportPublisher
    {
        private readonly QuantBimTraceableTakeoffEngine _evidence = new QuantBimTraceableTakeoffEngine();

        public QuantBimSelectionExportBundle Publish(QuantBimStandaloneIfcSession session, IfcSelectionSet selection)
        {
            if (session == null) throw new ArgumentNullException("session");
            if (selection == null) throw new ArgumentNullException("selection");

            var canonicalSelection = new IfcSelectionSet(selection.Name, selection.Guids);
            var evidence = _evidence.Build(session.Document, canonicalSelection);
            var evidenceBoq = _evidence.BuildBoq(evidence);
            var sessionBoq = session.BuildBoq(canonicalSelection);
            RequireEquivalentBoq(sessionBoq, evidenceBoq);

            var boqCsv = session.ExportCsv(canonicalSelection);
            var evidenceCsv = _evidence.ExportEvidenceCsv(evidence);
            return new QuantBimSelectionExportBundle(
                session.Path,
                session.Revision,
                canonicalSelection.Name,
                canonicalSelection.Guids,
                evidence,
                evidenceBoq,
                boqCsv,
                evidenceCsv);
        }

        public QuantBimSelectionExportBundle Publish(
            QuantBimStandaloneIfcSession session,
            IfcStandaloneDocument candidate,
            IfcSelectionSet selection)
        {
            if (session == null) throw new ArgumentNullException("session");
            session.ValidateGeneration(candidate ?? throw new ArgumentNullException("candidate"));
            return Publish(session, selection);
        }

        private static void RequireEquivalentBoq(
            IEnumerable<TakeoffInventoryLine> sessionBoq,
            IEnumerable<TakeoffInventoryLine> evidenceBoq)
        {
            var left = (sessionBoq ?? throw new ArgumentNullException("sessionBoq"))
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ToList();
            var right = (evidenceBoq ?? throw new ArgumentNullException("evidenceBoq"))
                .OrderBy(x => x.Classification, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Unit, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (left.Count != right.Count)
                throw new InvalidOperationException("QuantBIM selection publication BOQ disagrees with traceable evidence BOQ.");

            for (var index = 0; index < left.Count; index++)
            {
                var a = left[index];
                var b = right[index];
                if (!string.Equals(a.Classification, b.Classification, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(a.Unit, b.Unit, StringComparison.OrdinalIgnoreCase) ||
                    a.Quantity != b.Quantity ||
                    a.SourceCount != b.SourceCount)
                {
                    throw new InvalidOperationException("QuantBIM selection publication BOQ disagrees with traceable evidence BOQ.");
                }
            }
        }
    }
}
