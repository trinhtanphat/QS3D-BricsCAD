using System;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Documentation
{
    public sealed class SemanticScheduleService
    {
        private readonly SemanticDocumentationCatalogStore _store = new SemanticDocumentationCatalogStore();

        public SemanticDocumentationTable BuildTable(ProjectState project, string scheduleId)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (string.IsNullOrWhiteSpace(scheduleId)) throw new ArgumentException("Semantic schedule id is required.", nameof(scheduleId));
            var id = scheduleId.Trim();
            var catalog = _store.Load(project);
            var matches = catalog.Schedules
                .Where(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length == 0) throw new InvalidOperationException("Semantic schedule does not exist: " + id + ".");
            if (matches.Length > 1) throw new InvalidOperationException("Semantic schedule id is ambiguous: " + id + ".");
            return SemanticSchedulePlanner.BuildTable(project, matches[0], catalog.Views);
        }
    }
}
