using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Templates;

namespace QS3D.Core.Recognition
{
    public sealed class ProjectRecognitionService
    {
        private readonly RecognitionEngine _fallback = new RecognitionEngine();

        public RecognitionResult Suggest(ProjectState project, EntitySnapshot snapshot)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var mapped = ExactLayerMapping(project, snapshot.Layer);
            var fallback = _fallback.Suggest(snapshot);
            if (mapped == null) return fallback;

            var candidates = new List<RecognitionCandidate> { mapped };
            foreach (var candidate in fallback.Candidates)
                if (candidate.Category != mapped.Category) candidates.Add(candidate);
            return new RecognitionResult(snapshot, candidates);
        }

        public RecognitionBatch SuggestBatch(ProjectState project, IEnumerable<EntitySnapshot> snapshots, double autoAcceptConfidence = 0.92d, double minimumMargin = 0.15d)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            return new RecognitionBatch(snapshots.Select(x => Suggest(project, x)), autoAcceptConfidence, minimumMargin);
        }

        private static RecognitionCandidate? ExactLayerMapping(ProjectState project, string layer)
        {
            var normalizedLayer = RecognitionText.Normalize(layer);
            foreach (var item in project.Metadata.Where(x => x.Key.StartsWith(TemplateProfileStore.LayerMappingPrefix, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                var pattern = item.Key.Substring(TemplateProfileStore.LayerMappingPrefix.Length).Trim();
                if (pattern.Length == 0 || !string.Equals(RecognitionText.Normalize(pattern), normalizedLayer, StringComparison.OrdinalIgnoreCase)) continue;
                if (!Enum.TryParse(item.Value, true, out ElementCategory category)) continue;
                var candidate = new RecognitionCandidate { RuleId = "project-layer:" + pattern, Category = category, Confidence = 0.99d };
                candidate.Evidence.Add("project-layer:" + pattern);
                return candidate;
            }
            return null;
        }
    }
}
