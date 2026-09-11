using System;
using QS3D.Core.Review;

namespace QS3D.Core.Domain
{
    public sealed class ProjectBqReviewLedger
    {
        private readonly ProjectState _project;
        private readonly ProjectMetadataDictionary _metadata;

        private ProjectBqReviewLedger(ProjectState project, ProjectMetadataDictionary metadata)
        {
            _project = project;
            _metadata = metadata;
            _metadata.BindProject(project);
        }

        public static ProjectBqReviewLedger Open(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var metadata = project.Metadata as ProjectMetadataDictionary
                ?? throw new InvalidOperationException("BQ review ledger requires the canonical project metadata store.");
            return new ProjectBqReviewLedger(project, metadata);
        }

        public bool HasValue => ProjectBqReviewLedgerCodec.Read(_metadata) != null;
        public BqReviewLedgerState Current => ProjectBqReviewLedgerCodec.Read(_metadata) ?? BqReviewLedgerState.Empty;

        public void Replace(BqReviewLedgerState state)        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var value = ProjectBqReviewLedgerCodec.Value(state);
            if (_metadata.TryGetValue(ProjectBqReviewLedgerCodec.LedgerKey, out var existing) &&
                string.Equals(existing, value, StringComparison.Ordinal))
                return;

            _metadata.EnsureCanSetOwned(ProjectBqReviewLedgerCodec.LedgerKey);
            _project.Touch();
            _metadata.SetOwned(ProjectBqReviewLedgerCodec.LedgerKey, value);
        }

        public void Upsert(BqReviewEntry entry)
        {
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            Replace(Current.Upsert(entry));
        }

        public bool Remove(string elementId)
        {
            var next = Current.Remove(elementId, out var removed);
            if (!removed) return false;
            if (next.Entries.Count == 0) return Clear();
            Replace(next);
            return true;
        }

        public bool Clear()
        {
            if (!_metadata.ContainsKey(ProjectBqReviewLedgerCodec.LedgerKey))            {
                ProjectBqReviewLedgerCodec.Read(_metadata);
                return false;
            }

            ProjectBqReviewLedgerCodec.Read(_metadata);
            _project.Touch();
            if (!_metadata.RemoveOwned(ProjectBqReviewLedgerCodec.LedgerKey))
                throw new InvalidOperationException("BQ review ledger disappeared during removal.");
            return true;
        }
    }
}
