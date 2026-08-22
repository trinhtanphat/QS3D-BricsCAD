using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using QS3D.Core.Domain;
using QS3D.Core.Model;

namespace QS3D.Core.Recognition
{
    public sealed class RecognitionRule
    {
        public RecognitionRule(string id, ElementCategory category, IEnumerable<string>? layerTerms = null, IEnumerable<string>? textTerms = null, IEnumerable<string>? entityTypes = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? throw new ArgumentException("Rule id is required.", nameof(id)) : id.Trim();
            Category = category;
            LayerTerms = NormalizeTerms(layerTerms);
            TextTerms = NormalizeTerms(textTerms);
            EntityTypes = NormalizeTerms(entityTypes);
        }
        public string Id { get; }
        public ElementCategory Category { get; }
        public IReadOnlyList<string> LayerTerms { get; }
        public IReadOnlyList<string> TextTerms { get; }
        public IReadOnlyList<string> EntityTypes { get; }
        private static IReadOnlyList<string> NormalizeTerms(IEnumerable<string>? source) => (source ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(RecognitionText.Normalize).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public sealed class RecognitionCandidate
    {
        public string RuleId { get; set; } = string.Empty;
        public ElementCategory Category { get; set; }
        public double Confidence { get; set; }
        public IList<string> Evidence { get; } = new List<string>();
        public string EvidenceText => string.Join("; ", Evidence);
    }

    public sealed class RecognitionResult
    {
        public RecognitionResult(EntitySnapshot snapshot, IReadOnlyList<RecognitionCandidate> candidates)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            Candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        }
        public EntitySnapshot Snapshot { get; }
        public IReadOnlyList<RecognitionCandidate> Candidates { get; }
        public RecognitionCandidate? TopCandidate => Candidates.Count == 0 ? null : Candidates[0];
        public double Margin => Candidates.Count < 2 ? (TopCandidate?.Confidence ?? 0d) : Candidates[0].Confidence - Candidates[1].Confidence;
        public bool RequiresReview => TopCandidate == null || TopCandidate.Confidence < 0.82d || Margin < 0.15d;
        public string Handle => Snapshot.Handle;
        public string SuggestedCategory => TopCandidate?.Category.ToString() ?? string.Empty;
        public double Confidence => TopCandidate?.Confidence ?? 0d;
        public string Evidence => TopCandidate?.EvidenceText ?? string.Empty;
    }

    public sealed class RecognitionBatch
    {
        public RecognitionBatch(IEnumerable<RecognitionResult> results, double autoAcceptConfidence = 0.92d, double minimumMargin = 0.15d)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (autoAcceptConfidence < 0d || autoAcceptConfidence > 1d) throw new ArgumentOutOfRangeException(nameof(autoAcceptConfidence));
            if (minimumMargin < 0d || minimumMargin > 1d) throw new ArgumentOutOfRangeException(nameof(minimumMargin));
            Results = results.ToList();
            AutoAccepted = Results.Where(x => x.TopCandidate is RecognitionCandidate candidate && candidate.Confidence >= autoAcceptConfidence && x.Margin >= minimumMargin).ToList();
            ReviewRequired = Results.Except(AutoAccepted).ToList();
        }
        public IReadOnlyList<RecognitionResult> Results { get; }
        public IReadOnlyList<RecognitionResult> AutoAccepted { get; }
        public IReadOnlyList<RecognitionResult> ReviewRequired { get; }
    }

    public sealed class RecognitionEngine
    {
        private readonly IReadOnlyList<RecognitionRule> _rules;
        public RecognitionEngine(IEnumerable<RecognitionRule>? rules = null) => _rules = (rules ?? DefaultRules()).ToList();

        public RecognitionResult Suggest(EntitySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var layer = RecognitionText.Normalize(snapshot.Layer);
            var entityType = RecognitionText.Normalize(snapshot.EntityType);
            var contextualText = RecognitionText.Normalize(string.Join(" ", snapshot.Metadata.Where(x => IsTextMetadata(x.Key)).Select(x => x.Value)));
            var candidates = new List<RecognitionCandidate>();
            foreach (var rule in _rules)
            {
                var candidate = Score(rule, layer, contextualText, entityType);
                if (candidate.Confidence >= 0.20d) candidates.Add(candidate);
            }
            return new RecognitionResult(snapshot, candidates.OrderByDescending(x => x.Confidence).ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public RecognitionBatch SuggestBatch(IEnumerable<EntitySnapshot> snapshots, double autoAcceptConfidence = 0.92d, double minimumMargin = 0.15d) => new RecognitionBatch((snapshots ?? throw new ArgumentNullException(nameof(snapshots))).Select(Suggest), autoAcceptConfidence, minimumMargin);

        public static IReadOnlyList<RecognitionRule> DefaultRules() => new[]
        {
            new RecognitionRule("beam", ElementCategory.Beam, new[]{"beam","dam","d-beam","kc-dam"}, new[]{"beam","dam"}, new[]{"line","polyline"}),
            new RecognitionRule("slab", ElementCategory.Slab, new[]{"slab","san","floor-struct"}, new[]{"slab","san"}, new[]{"polyline","hatch","region"}),
            new RecognitionRule("column", ElementCategory.Column, new[]{"column","cot","kc-cot"}, new[]{"column","cot"}, new[]{"polyline","blockreference","region"}),
            new RecognitionRule("struct-wall", ElementCategory.StructuralWall, new[]{"structwall","vach","kc-vach","shearwall"}, new[]{"structural wall","vach","shear wall"}, new[]{"line","polyline"}),
            new RecognitionRule("arch-wall", ElementCategory.ArchitecturalWall, new[]{"wall","tuong","a-wall"}, new[]{"wall","tuong"}, new[]{"line","polyline"}),
            new RecognitionRule("opening", ElementCategory.WallOpening, new[]{"opening","lo-mo","void"}, new[]{"opening","lo mo"}, new[]{"blockreference","polyline"}),
            new RecognitionRule("door", ElementCategory.Door, new[]{"door","cua","a-door"}, new[]{"door","cua"}, new[]{"blockreference","polyline"}),
            new RecognitionRule("room", ElementCategory.Room, new[]{"room","phong","a-room"}, new[]{"room","phong"}, new[]{"polyline","hatch","region"}),
            new RecognitionRule("foundation", ElementCategory.Foundation, new[]{"foundation","mong","footing"}, new[]{"foundation","mong","footing"}, new[]{"polyline","region"}),
            new RecognitionRule("stair", ElementCategory.Stair, new[]{"stair","cau-thang"}, new[]{"stair","cau thang"}, new[]{"polyline","blockreference"}),
            new RecognitionRule("railing", ElementCategory.Railing, new[]{"railing","lan-can"}, new[]{"railing","lan can"}, new[]{"line","polyline"}),
            new RecognitionRule("earthwork", ElementCategory.Earthwork, new[]{"earth","excav","dao-dat"}, new[]{"earthwork","excavation","dao dat"}, new[]{"polyline","hatch","region"})
        };

        private static RecognitionCandidate Score(RecognitionRule rule, string layer, string text, string entityType)
        {
            var result = new RecognitionCandidate { RuleId = rule.Id, Category = rule.Category };
            var layerMatch = BestTerm(rule.LayerTerms, layer);
            var textMatch = BestTerm(rule.TextTerms, text);
            var typeMatch = rule.EntityTypes.Count == 0 || rule.EntityTypes.Any(x => string.Equals(x, entityType, StringComparison.OrdinalIgnoreCase) || entityType.Contains(x));
            if (!string.IsNullOrEmpty(layerMatch)) { result.Confidence += 0.62d; result.Evidence.Add("layer:" + layerMatch); }
            if (!string.IsNullOrEmpty(textMatch)) { result.Confidence += 0.28d; result.Evidence.Add("text:" + textMatch); }
            if (typeMatch && (!string.IsNullOrEmpty(layerMatch) || !string.IsNullOrEmpty(textMatch))) { result.Confidence += 0.10d; result.Evidence.Add("type:" + entityType); }
            result.Confidence = Math.Min(1d, result.Confidence);
            return result;
        }

        private static string BestTerm(IEnumerable<string> terms, string haystack) => terms.Where(x => haystack.Contains(x)).OrderByDescending(x => x.Length).FirstOrDefault() ?? string.Empty;
        private static bool IsTextMetadata(string key) => key.IndexOf("text", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("name", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("block", StringComparison.OrdinalIgnoreCase) >= 0 || key.IndexOf("tag", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    internal static class RecognitionText
    {
        public static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var source = value!.Trim().ToLowerInvariant().Replace('đ', 'd');
            var normalized = source.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var ch in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) builder.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            return string.Join(" ", builder.ToString().Split(new[]{' '}, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
