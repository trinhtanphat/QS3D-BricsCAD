using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace QS3D.Core.Features
{
    public sealed class ParityClosureReport
    {
        internal ParityClosureReport(bool catalogComplete, int applicableCount, int fullPassCount, int hostBoundaryCount)
        {
            CatalogComplete = catalogComplete;
            ApplicableCount = applicableCount;
            FullPassCount = fullPassCount;
            HostBoundaryCount = hostBoundaryCount;
        }

        public bool CatalogComplete { get; }
        public int ApplicableCount { get; }
        public int FullPassCount { get; }
        public int HostBoundaryCount { get; }
        public bool CanClaimFullParity => CatalogComplete && ApplicableCount > 0 && FullPassCount == ApplicableCount;
    }

    public sealed class ParityManifest
    {
        private readonly IReadOnlyList<ParityFeatureRecord> _records;
        private readonly Dictionary<FeatureId, ParityFeatureRecord> _byFeatureId;

        public ParityManifest(IEnumerable<ParityFeatureRecord> records, bool catalogComplete)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            var materialized = records.ToArray();
            if (materialized.Length == 0)
                throw new InvalidOperationException("Parity manifest cannot be empty.");
            if (materialized.Any(x => x == null))
                throw new InvalidOperationException("Parity manifest cannot contain null records.");
            if (materialized.GroupBy(x => x.FeatureId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Parity manifest contains duplicate FeatureId values.");
            if (materialized.GroupBy(x => x.WorkflowKey, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Parity manifest contains duplicate workflow keys.");

            CatalogComplete = catalogComplete;
            _records = new ReadOnlyCollection<ParityFeatureRecord>(materialized);
            _byFeatureId = materialized.ToDictionary(x => x.FeatureId);
        }

        public IReadOnlyList<ParityFeatureRecord> Records => _records;
        public bool CatalogComplete { get; }

        public ParityFeatureRecord GetRequired(FeatureId id)
        {
            if (_byFeatureId.TryGetValue(id, out var record)) return record;
            throw new KeyNotFoundException("Parity feature is not present in the manifest: " + id + ".");
        }

        public ParityClosureReport GetClosureReport()
        {
            var applicable = _records.Count(x => x.IsApplicable);
            var fullPass = _records.Count(x => x.IsApplicable && x.EvidenceStage == ParityEvidenceStage.V25V26ParityPass);
            var hostBoundary = _records.Count(x => x.Applicability == ParityApplicability.NotApplicableByHostBoundary);
            return new ParityClosureReport(CatalogComplete, applicable, fullPass, hostBoundary);
        }
    }
}