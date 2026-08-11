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
            if (!Enum.IsDefined(typeof(ElementCategory), category)) throw new ArgumentOutOfRangeException(nameof(category), "Recognition rule category must be defined.");
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
        private ElementCategory _category;

        public string RuleId { get; set; } = string.Empty;
        public ElementCategory Category
        {
            get => _category;
            set
            {
                if (!Enum.IsDefined(typeof(ElementCategory), value)) throw new ArgumentOutOfRangeException(nameof(value), "Recognition candidate category must be defined.");
                _category = value;
            }
        }
        public double Confidence { get; set; }
        public IList<string> Evidence { get; } = new List<string>();
        public string EvidenceText => string.Join("; ", Evidence);
    }

    public sealed class RecognitionResult
    {
        public RecognitionResult(EntitySnapshot snapshot, IReadOnlyList<RecognitionCandidate> candidates)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            ValidateCandidates(candidates);
            Candidates = candidates.ToList().AsReadOnly();
        }
        public EntitySnapshot Snapshot { get; }
        public IReadOnlyList<RecognitionCandidate> Candidates { get; }
        public RecognitionCandidate? TopCandidate => Candidates.Count == 0 ? null : Candidates[0];
        public double Margin => Candidates.Count < 2 ? (TopCandidate?.Confidence ?? 0d) : Candidates[0].Confidence - Candidates[1].Confidence;
        public bool IsCaptureReady => TopCandidate != null && EntitySnapshotCaptureEligibility.IsReady(Snapshot, TopCandidate.Category, out _);
        public string CaptureReadinessReason
        {
            get
            {
                if (TopCandidate == null) return "No recognition candidate is available.";
                EntitySnapshotCaptureEligibility.IsReady(Snapshot, TopCandidate.Category, out var reason);
                return reason;
            }
        }
        public bool RequiresReview
        {
            get
            {
                ValidateCurrentCandidates();
                return TopCandidate == null || TopCandidate.Confidence < 0.82d || Margin < 0.15d || !IsCaptureReady;
            }
        }
        public string Handle => Snapshot.Handle;
        public string SuggestedCategory => TopCandidate?.Category.ToString() ?? string.Empty;
        public double Confidence => TopCandidate?.Confidence ?? 0d;
        public string Evidence => TopCandidate?.EvidenceText ?? string.Empty;

        internal void ValidateCurrentCandidates() => ValidateCandidates(Candidates);

        private static void ValidateCandidates(IEnumerable<RecognitionCandidate> candidates)
        {
            foreach (var candidate in candidates)
            {
                if (candidate == null) throw new ArgumentException("Recognition candidate list cannot contain null.", nameof(candidates));
                if (!Enum.IsDefined(typeof(ElementCategory), candidate.Category))
                    throw new ArgumentOutOfRangeException(nameof(candidates), "Recognition candidate category must be defined.");
                if (double.IsNaN(candidate.Confidence) || double.IsInfinity(candidate.Confidence) || candidate.Confidence < 0d || candidate.Confidence > 1d)
                    throw new ArgumentOutOfRangeException(nameof(candidates), "Recognition confidence must be finite and between 0 and 1.");
            }
        }
    }

    public sealed class RecognitionBatch
    {
        public RecognitionBatch(IEnumerable<RecognitionResult> results, double autoAcceptConfidence = 0.92d, double minimumMargin = 0.15d)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            ValidateProbability(autoAcceptConfidence, nameof(autoAcceptConfidence));
            ValidateProbability(minimumMargin, nameof(minimumMargin));
            var materialized = results.ToList();
            if (materialized.Any(x => x == null)) throw new ArgumentException("Recognition results cannot contain null.", nameof(results));
            foreach (var result in materialized) result.ValidateCurrentCandidates();
            Results = materialized.AsReadOnly();
            AutoAccepted = Results.Where(x => x.TopCandidate is RecognitionCandidate candidate && candidate.Confidence >= autoAcceptConfidence && x.Margin >= minimumMargin && x.IsCaptureReady).ToList().AsReadOnly();
            ReviewRequired = Results.Except(AutoAccepted).ToList().AsReadOnly();
        }
        public IReadOnlyList<RecognitionResult> Results { get; }
        public IReadOnlyList<RecognitionResult> AutoAccepted { get; }
        public IReadOnlyList<RecognitionResult> ReviewRequired { get; }

        private static void ValidateProbability(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d || value > 1d)
                throw new ArgumentOutOfRangeException(name, "Recognition threshold must be finite and between 0 and 1.");
        }
    }

    public sealed class RecognitionEngine
    {
        private readonly IReadOnlyList<RecognitionRule> _rules;
        private static readonly IReadOnlyDictionary<ElementCategory, ISet<string>> DefaultEntityTypes = BuildDefaultEntityTypes();

        public RecognitionEngine(IEnumerable<RecognitionRule>? rules = null)
        {
            var materialized = (rules ?? DefaultRules()).ToList();
            if (materialized.Any(x => x == null)) throw new ArgumentException("Recognition rules cannot contain null.", nameof(rules));
            var duplicate = materialized.GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(x => x.Count() > 1);
            if (duplicate != null) throw new ArgumentException("Duplicate recognition rule id: " + duplicate.Key, nameof(rules));
            _rules = materialized.AsReadOnly();
        }

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
                if (!EntitySnapshotCaptureEligibility.IsReady(snapshot, rule.Category, out var reason) && candidate.Confidence >= 0.20d)
                    candidate.Evidence.Add("capture-blocked:" + reason);
                if (candidate.Confidence >= 0.20d) candidates.Add(candidate);
            }
            return new RecognitionResult(snapshot, candidates.OrderByDescending(x => x.Confidence).ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase).ToList());
        }

        public RecognitionBatch SuggestBatch(IEnumerable<EntitySnapshot> snapshots, double autoAcceptConfidence = 0.92d, double minimumMargin = 0.15d) => new RecognitionBatch((snapshots ?? throw new ArgumentNullException(nameof(snapshots))).Select(Suggest), autoAcceptConfidence, minimumMargin);

        public static bool IsEntityTypeCompatible(ElementCategory category, string entityType)
        {
            var normalized = RecognitionText.Normalize(entityType);
            if (normalized.Length == 0) return false;
            if (category == ElementCategory.GlassWall || category == ElementCategory.WallPier) category = ElementCategory.ArchitecturalWall;
            if (category == ElementCategory.FloorFinish || category == ElementCategory.Waterproofing || category == ElementCategory.Skirting || category == ElementCategory.CeilingFinish)
                category = ElementCategory.Room;
            if (DefaultEntityTypes.TryGetValue(category, out var allowed)) return allowed.Contains(normalized);
            if (category == ElementCategory.Grid) return normalized == "line" || normalized == "arc" || normalized == "polyline";
            if (category == ElementCategory.CustomQuantity)
                return normalized == "line" || normalized == "arc" || normalized == "circle" || normalized == "polyline" || normalized == "region" || normalized == "hatch" || normalized == "solid3d" || normalized == "3dsolid" || normalized == "blockreference" || normalized == "proxyentity";
            return false;
        }

        public static IReadOnlyList<RecognitionRule> DefaultRules() => new[]
        {
            new RecognitionRule("beam", ElementCategory.Beam, new[]{"blt beam","beam","dam","d-beam","kc-dam"}, new[]{"beam","dam"}, new[]{"line","polyline","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("slab", ElementCategory.Slab, new[]{"blt slab","slab","san","floor-struct"}, new[]{"slab","san"}, new[]{"polyline","hatch","region","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("column", ElementCategory.Column, new[]{"blt column","column","cot","kc-cot"}, new[]{"column","cot"}, new[]{"polyline","blockreference","region","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("struct-wall", ElementCategory.StructuralWall, new[]{"blt structural wall","structwall","vach","kc-vach","shearwall"}, new[]{"structural wall","vach","shear wall"}, new[]{"line","polyline","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("arch-wall", ElementCategory.ArchitecturalWall, new[]{"blt arc wall","blt wall","wall","tuong","a-wall"}, new[]{"wall","tuong"}, new[]{"line","polyline","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("wall-finish", ElementCategory.WallFinish, new[]{"blt wall finish","wallfinish","wall finish","hoan thien tuong"}, new[]{"wall finish","hoan thien tuong"}, new[]{"polyline","region","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("opening", ElementCategory.WallOpening, new[]{"blt opening","blt lo mo tuong","opening","lo-mo","void"}, new[]{"opening","lo mo"}, new[]{"blockreference","polyline","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("door", ElementCategory.Door, new[]{"blt door","door","cua","a-door"}, new[]{"door","cua"}, new[]{"blockreference","polyline","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("room", ElementCategory.Room, new[]{"blt room","room","phong","a-room"}, new[]{"room","phong"}, new[]{"polyline","hatch","region","proxyentity"}),
            new RecognitionRule("foundation", ElementCategory.Foundation, new[]{"blt raft foundation","blt strip foundation","blt pile cap","foundation","mong","footing"}, new[]{"foundation","mong","footing"}, new[]{"polyline","region","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("stair", ElementCategory.Stair, new[]{"blt stair","stair","cau-thang"}, new[]{"stair","cau thang"}, new[]{"polyline","blockreference","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("railing", ElementCategory.Railing, new[]{"blt railing","railing","lan-can"}, new[]{"railing","lan can"}, new[]{"line","polyline","solid3d","3dsolid","proxyentity"}),
            new RecognitionRule("earthwork", ElementCategory.Earthwork, new[]{"blt earthwork","earth","excav","dao-dat"}, new[]{"earthwork","excavation","dao dat"}, new[]{"polyline","hatch","region","solid3d","3dsolid","proxyentity"})
        };

        private static RecognitionCandidate Score(RecognitionRule rule, string layer, string text, string entityType)
        {
            var result = new RecognitionCandidate { RuleId = rule.Id, Category = rule.Category };
            var typeMatch = rule.EntityTypes.Count == 0 || rule.EntityTypes.Any(x => string.Equals(x, entityType, StringComparison.OrdinalIgnoreCase));
            if (!typeMatch) return result;
            var layerMatch = BestTerm(rule.LayerTerms, layer);
            var textMatch = BestTerm(rule.TextTerms, text);
            if (!string.IsNullOrEmpty(layerMatch)) { result.Confidence += string.Equals(layer, layerMatch, StringComparison.OrdinalIgnoreCase) ? 0.90d : 0.62d; result.Evidence.Add("layer:" + layerMatch); }
            if (!string.IsNullOrEmpty(textMatch)) { result.Confidence += 0.28d; result.Evidence.Add("text:" + textMatch); }
            if (typeMatch && (!string.IsNullOrEmpty(layerMatch) || !string.IsNullOrEmpty(textMatch))) { result.Confidence += 0.10d; result.Evidence.Add("type:" + entityType); }
            result.Confidence = Math.Min(1d, result.Confidence);
            return result;
        }

        private static IReadOnlyDictionary<ElementCategory, ISet<string>> BuildDefaultEntityTypes()
        {
            return DefaultRules()
                .GroupBy(x => x.Category)
                .ToDictionary(
                    x => x.Key,
                    x => (ISet<string>)new HashSet<string>(x.SelectMany(rule => rule.EntityTypes), StringComparer.OrdinalIgnoreCase));
        }

        private static string BestTerm(IEnumerable<string> terms, string haystack) => terms.Where(x => ContainsTerm(haystack, x)).OrderByDescending(x => x.Length).FirstOrDefault() ?? string.Empty;

        private static bool ContainsTerm(string haystack, string term)
        {
            if (string.IsNullOrWhiteSpace(haystack) || string.IsNullOrWhiteSpace(term)) return false;
            if (string.Equals(haystack, term, StringComparison.OrdinalIgnoreCase)) return true;
            return (" " + haystack + " ").IndexOf(" " + term + " ", StringComparison.OrdinalIgnoreCase) >= 0;
        }

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
