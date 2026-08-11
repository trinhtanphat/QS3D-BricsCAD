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
            var mapped = ExactLayerMapping(project, snapshot);
            if (mapped != null) return new RecognitionResult(snapshot, new[] { mapped });
            return _fallback.Suggest(snapshot);
        }

        public RecognitionBatch SuggestBatch(ProjectState project, IEnumerable<EntitySnapshot> snapshots, double autoAcceptConfidence = 0.92d, double minimumMargin = 0.15d)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (snapshots == null) throw new ArgumentNullException(nameof(snapshots));
            return new RecognitionBatch(snapshots.Select(x => Suggest(project, x)), autoAcceptConfidence, minimumMargin);
        }

        internal static void ValidateLayerMappings(IEnumerable<KeyValuePair<string, string>> mappings, string label)
        {
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));
            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in mappings)
            {
                var pattern = (item.Key ?? string.Empty).Trim();
                if (pattern.Length == 0) throw new InvalidOperationException(label + " contains an empty layer mapping pattern.");
                var key = RecognitionText.Normalize(pattern);
                if (key.Length == 0) throw new InvalidOperationException(label + " contains a layer mapping pattern that normalizes to empty: " + pattern);
                if (!Enum.TryParse(item.Value, true, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category))
                    throw new InvalidOperationException(label + " contains an invalid layer mapping category for " + pattern + ": " + item.Value);
                if (normalized.TryGetValue(key, out var previous))
                    throw new InvalidOperationException(label + " contains ambiguous normalized layer mappings: " + previous + " and " + pattern + ".");
                normalized.Add(key, pattern);
            }
        }

        private static RecognitionCandidate? ExactLayerMapping(ProjectState project, EntitySnapshot snapshot)
        {
            var mappings = project.Metadata
                .Where(x => x.Key.StartsWith(TemplateProfileStore.LayerMappingPrefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => new KeyValuePair<string, string>(x.Key.Substring(TemplateProfileStore.LayerMappingPrefix.Length), x.Value))
                .ToList();
            ValidateLayerMappings(mappings, "Project recognition mappings");

            var normalizedLayer = RecognitionText.Normalize(snapshot.Layer);
            foreach (var item in mappings)
            {
                var pattern = item.Key.Trim();
                if (!string.Equals(RecognitionText.Normalize(pattern), normalizedLayer, StringComparison.OrdinalIgnoreCase)) continue;
                if (!Enum.TryParse(item.Value, true, out ElementCategory category) || !Enum.IsDefined(typeof(ElementCategory), category))
                    throw new InvalidOperationException("Invalid project layer mapping category: " + item.Value);
                if (!RecognitionEngine.IsEntityTypeCompatible(category, snapshot.EntityType)) return null;
                var candidate = new RecognitionCandidate { RuleId = "project-layer:" + pattern, Category = category, Confidence = 0.99d };
                candidate.Evidence.Add("project-layer:" + pattern);
                return candidate;
            }
            return null;
        }
    }
}
