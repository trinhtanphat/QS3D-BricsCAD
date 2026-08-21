using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;

namespace QS3D.Core.Legacy
{
    public enum BltLegacyEvidenceMode
    {
        Insufficient = 0,
        MetadataOnly = 1,
        SemanticReconstructed = 2,
        ExactGeometry = 3,
        ExactLegacyQuantity = 4
    }

    public static class BltLegacyMetadataKeys
    {
        public const string SourceSystem = "BLT.SourceSystem";
        public const string AdapterVersion = "BLT.AdapterVersion";
        public const string MetricEvidence = "BLT.MetricEvidence";
        public const string CategoryEvidence = "BLT.CategoryEvidence";
        public const string ConcreteM3 = "BLT.LegacyConcreteM3";
        public const string FormworkM2 = "BLT.LegacyFormworkM2";
        public const string FloorHint = "BLT.FloorHint";
        public const string FamilyHint = "BLT.FamilyHint";
        public const string ElementNameHint = "BLT.ElementNameHint";
        public const string MaterialHint = "BLT.MaterialHint";
        public const string ProbeMetricEvidence = "LegacyProbe.MetricEvidence";
    }

    public sealed class BltLegacyElementCandidate
    {
        internal BltLegacyElementCandidate(
            EntitySnapshot snapshot,
            bool hasLegacySignal,
            ElementCategory? category,
            string categoryEvidence,
            BltLegacyEvidenceMode evidenceMode,
            double? legacyConcreteM3,
            double? legacyFormworkM2,
            string floorHint,
            string familyHint,
            string elementNameHint,
            string materialHint,
            string reason)
        {
            Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
            HasLegacySignal = hasLegacySignal;
            Category = category;
            CategoryEvidence = categoryEvidence ?? string.Empty;
            EvidenceMode = evidenceMode;
            LegacyConcreteM3 = legacyConcreteM3;
            LegacyFormworkM2 = legacyFormworkM2;
            FloorHint = floorHint ?? string.Empty;
            FamilyHint = familyHint ?? string.Empty;
            ElementNameHint = elementNameHint ?? string.Empty;
            MaterialHint = materialHint ?? string.Empty;
            Reason = reason ?? string.Empty;
        }

        public EntitySnapshot Snapshot { get; }
        public bool HasLegacySignal { get; }
        public ElementCategory? Category { get; }
        public string CategoryEvidence { get; }
        public BltLegacyEvidenceMode EvidenceMode { get; }
        public double? LegacyConcreteM3 { get; }
        public double? LegacyFormworkM2 { get; }
        public string FloorHint { get; }
        public string FamilyHint { get; }
        public string ElementNameHint { get; }
        public string MaterialHint { get; }
        public string Reason { get; }

        public bool CanImport
        {
            get
            {
                if (!HasLegacySignal || !Category.HasValue) return false;
                if (EvidenceMode != BltLegacyEvidenceMode.ExactGeometry &&
                    EvidenceMode != BltLegacyEvidenceMode.ExactLegacyQuantity &&
                    EvidenceMode != BltLegacyEvidenceMode.SemanticReconstructed)
                    return false;
                return EntitySnapshotCaptureEligibility.IsReady(Snapshot, Category.Value, out _);
            }
        }
    }

    /// <summary>
    /// Clean-room adapter for evidence already exposed through QS3D host snapshots.
    /// It never depends on BLT binaries and never guesses an integer category code whose
    /// meaning has not been independently established. Unknown/ambiguous evidence fails closed.
    /// </summary>
    public static class BltLegacyEntityAdapter
    {
        private const string AdapterVersionValue = "1";
        private static readonly Regex EmbeddedPair = new Regex(
            @"(?<key>[\p{L}\p{N}_ .\-/]+)\s*[:=]\s*(?<value>[^;|\r\n]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly string[] CategoryKeyAliases =
        {
            "category", "categorycode", "loaicaukien", "loaick", "elementtype", "blttype"
        };

        private static readonly string[] ConcreteMetricAliases =
        {
            "concretem3", "netconcretem3", "betongm3", "btm3", "thetichm3", "volumem3"
        };

        private static readonly string[] FormworkMetricAliases =
        {
            "formworkm2", "coppham2", "dientichcoppham2", "vkm2"
        };

        public static BltLegacyElementCandidate Adapt(EntitySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var legacySignal = HasBltSignal(snapshot);
            var category = DetectCategory(snapshot, out var categoryEvidence, out var ambiguousCategory);
            var concrete = TryExtractMetric(snapshot.Metadata, ConcreteMetricAliases, out var concreteM3) ? (double?)concreteM3 : null;
            var formwork = TryExtractMetric(snapshot.Metadata, FormworkMetricAliases, out var formworkM2) ? (double?)formworkM2 : null;
            var floor = ExtractTextHint(snapshot.Metadata, "floor", "level", "tang");
            var family = ExtractTextHint(snapshot.Metadata, "family", "familyname");
            var elementName = ExtractTextHint(snapshot.Metadata, "elementname", "tencaukien");
            var material = ExtractTextHint(snapshot.Metadata, "material", "vatlieu");

            var evidence = ResolveEvidence(snapshot, concrete.HasValue || formwork.HasValue);
            string reason;
            if (!legacySignal)
                reason = "No explicit BLT/BLT3D marker was found in the runtime class, proxy metadata, XData or extension-dictionary evidence.";
            else if (ambiguousCategory)
                reason = "BLT evidence names more than one supported structural category; category inference is intentionally blocked.";
            else if (!category.HasValue)
                reason = "BLT marker found, but no independently supported Column/Beam/Slab/Foundation/StructuralWall category evidence was found.";
            else if (evidence == BltLegacyEvidenceMode.Insufficient || evidence == BltLegacyEvidenceMode.MetadataOnly)
                reason = "BLT category is known, but no authoritative geometry or explicit unit-labelled legacy quantity is available.";
            else if (!EntitySnapshotCaptureEligibility.IsReady(snapshot, category.Value, out var eligibilityReason))
                reason = eligibilityReason;
            else
                reason = string.Empty;

            if (legacySignal)
            {
                SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.SourceSystem, "BLT3D");
                SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.AdapterVersion, AdapterVersionValue);
                SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.MetricEvidence, evidence.ToString());
                if (categoryEvidence.Length != 0) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.CategoryEvidence, categoryEvidence);
                if (concrete.HasValue) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.ConcreteM3, concrete.Value.ToString("R", CultureInfo.InvariantCulture));
                if (formwork.HasValue) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.FormworkM2, formwork.Value.ToString("R", CultureInfo.InvariantCulture));
                if (floor.Length != 0) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.FloorHint, floor);
                if (family.Length != 0) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.FamilyHint, family);
                if (elementName.Length != 0) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.ElementNameHint, elementName);
                if (material.Length != 0) SetCanonical(snapshot.Metadata, BltLegacyMetadataKeys.MaterialHint, material);
            }

            return new BltLegacyElementCandidate(
                snapshot,
                legacySignal,
                category,
                categoryEvidence,
                evidence,
                concrete,
                formwork,
                floor,
                family,
                elementName,
                material,
                reason);
        }

        private static BltLegacyEvidenceMode ResolveEvidence(EntitySnapshot snapshot, bool hasExplicitQuantity)
        {
            if (hasExplicitQuantity) return BltLegacyEvidenceMode.ExactLegacyQuantity;
            if (snapshot.Metadata.TryGetValue(BltLegacyMetadataKeys.ProbeMetricEvidence, out var raw) &&
                Enum.TryParse(raw, true, out BltLegacyEvidenceMode parsed))
                return parsed;
            if (snapshot.Metadata.TryGetValue(BltLegacyMetadataKeys.MetricEvidence, out raw) &&
                Enum.TryParse(raw, true, out parsed))
                return parsed;
            return HasAnyPositiveMetric(snapshot) ? BltLegacyEvidenceMode.MetadataOnly : BltLegacyEvidenceMode.Insufficient;
        }

        private static bool HasAnyPositiveMetric(EntitySnapshot snapshot) =>
            Positive(snapshot.LengthDrawingUnits) ||
            Positive(snapshot.AreaDrawingUnitsSquared) ||
            Positive(snapshot.SurfaceAreaDrawingUnitsSquared) ||
            Positive(snapshot.VolumeDrawingUnitsCubed);

        private static bool Positive(double? value) => value.HasValue && value.Value > 0d && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);

        private static bool HasBltSignal(EntitySnapshot snapshot)
        {
            if (ContainsBltMarker(snapshot.EntityType)) return true;
            foreach (var pair in snapshot.Metadata)
            {
                if (ContainsBltMarker(pair.Key) || ContainsBltMarker(pair.Value)) return true;
            }
            return false;
        }

        private static bool ContainsBltMarker(string? value)
        {
            var normalized = Normalize(value);
            return normalized.Contains("blt3d") || normalized.StartsWith("blt") || normalized.Contains(" blt");
        }

        private static ElementCategory? DetectCategory(EntitySnapshot snapshot, out string evidence, out bool ambiguous)
        {
            var matches = new Dictionary<ElementCategory, string>();
            AddCategoryTextMatch(snapshot.EntityType, "runtime-class", matches);
            foreach (var pair in snapshot.Metadata)
            {
                var pairValue = pair.Value ?? string.Empty;
                AddCategoryTextMatch(pair.Key, "metadata-key:" + Bound(pair.Key, 80), matches);
                AddCategoryTextMatch(pairValue, "metadata-value:" + Bound(pairValue, 120), matches);
                AddExplicitCategoryCode(pair.Key, pairValue, matches);
                foreach (Match match in EmbeddedPair.Matches(pairValue))
                {
                    var key = match.Groups["key"].Value;
                    var value = match.Groups["value"].Value;
                    AddCategoryTextMatch(value, "embedded:" + Bound(key, 40), matches);
                    AddExplicitCategoryCode(key, value, matches);
                }
            }

            ambiguous = matches.Count > 1;
            if (matches.Count != 1)
            {
                evidence = ambiguous
                    ? string.Join(" | ", matches.OrderBy(x => x.Key.ToString(), StringComparer.Ordinal).Select(x => x.Key + "=" + x.Value))
                    : string.Empty;
                return null;
            }

            var only = matches.First();
            evidence = only.Value;
            return only.Key;
        }

        private static void AddExplicitCategoryCode(string? key, string? value, IDictionary<ElementCategory, string> matches)
        {
            var normalizedKey = NormalizeKey(key);
            if (!CategoryKeyAliases.Any(alias => normalizedKey.Contains(alias))) return;
            var normalizedValue = (value ?? string.Empty).Trim();
            if (!int.TryParse(normalizedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var code)) return;

            // These are the only integer aliases already independently established by
            // QuantityCalculationRuleSet. Do not guess the remaining BLT integer codes.
            if (code == 601) Add(matches, ElementCategory.Column, "legacy-code:601");
            else if (code == 701) Add(matches, ElementCategory.StructuralWall, "legacy-code:701");
        }

        private static void AddCategoryTextMatch(string? raw, string source, IDictionary<ElementCategory, string> matches)
        {
            var value = Normalize(raw);
            if (value.Length == 0) return;

            if (HasAlias(value, "bltcolumn", "column", "cot", "cotbtct")) Add(matches, ElementCategory.Column, source);
            if (HasAlias(value, "bltbeam", "beam", "dam", "dambtct")) Add(matches, ElementCategory.Beam, source);
            if (HasAlias(value, "bltslab", "slab", "san", "sanbtct")) Add(matches, ElementCategory.Slab, source);
            if (HasAlias(value, "bltfoundation", "foundation", "footing", "mong", "mongcoc", "daicoc", "dammong", "mongbang", "mongbe")) Add(matches, ElementCategory.Foundation, source);
            if (HasAlias(value, "bltstructuralwall", "structuralwall", "vach", "vachbt", "vachbtct")) Add(matches, ElementCategory.StructuralWall, source);
        }

        private static bool HasAlias(string normalizedText, params string[] aliases)
        {
            var compact = Compact(normalizedText);
            foreach (var alias in aliases)
            {
                var normalizedAlias = Compact(Normalize(alias));
                if (normalizedAlias.Length < 3) continue;
                if (compact.Contains(normalizedAlias)) return true;
            }
            return false;
        }

        private static void Add(IDictionary<ElementCategory, string> matches, ElementCategory category, string evidence)
        {
            if (!matches.ContainsKey(category)) matches.Add(category, evidence ?? string.Empty);
        }

        private static bool TryExtractMetric(IDictionary<string, string> metadata, string[] aliases, out double value)
        {
            value = 0d;
            foreach (var pair in metadata)
            {
                var pairValue = pair.Value ?? string.Empty;
                if (KeyMatches(pair.Key, aliases) && TryFiniteNonNegative(pairValue, out value)) return true;
                foreach (Match match in EmbeddedPair.Matches(pairValue))
                {
                    if (!KeyMatches(match.Groups["key"].Value, aliases)) continue;
                    if (TryFiniteNonNegative(match.Groups["value"].Value, out value)) return true;
                }
            }
            return false;
        }

        private static string ExtractTextHint(IDictionary<string, string> metadata, params string[] aliases)
        {
            foreach (var pair in metadata)
            {
                var pairValue = pair.Value ?? string.Empty;
                if (KeyMatches(pair.Key, aliases) && !string.IsNullOrWhiteSpace(pairValue)) return Bound(pairValue.Trim(), 200);
                foreach (Match match in EmbeddedPair.Matches(pairValue))
                {
                    if (!KeyMatches(match.Groups["key"].Value, aliases)) continue;
                    var value = (match.Groups["value"].Value ?? string.Empty).Trim();
                    if (value.Length != 0) return Bound(value, 200);
                }
            }
            return string.Empty;
        }

        private static bool KeyMatches(string? raw, IEnumerable<string> aliases)
        {
            var key = NormalizeKey(raw);
            return aliases.Any(alias => key.Contains(NormalizeKey(alias)));
        }

        private static bool TryFiniteNonNegative(string? raw, out double value)
        {
            value = 0d;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var text = raw.Trim();
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                var comma = text.Replace(',', '.');
                if (!double.TryParse(comma, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) return false;
            }
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0d;
        }

        private static void SetCanonical(IDictionary<string, string> metadata, string key, string value)
        {
            metadata[key] = Bound(value ?? string.Empty, 512);
        }

        private static string NormalizeKey(string? value) => Compact(Normalize(value));

        private static string Normalize(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                var category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category != UnicodeCategory.NonSpacingMark) builder.Append(character == 'đ' ? 'd' : character);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Compact(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
                if (char.IsLetterOrDigit(character)) builder.Append(character);
            return builder.ToString();
        }

        private static string Bound(string? value, int max)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length <= max ? value : value.Substring(0, max);
        }
    }
}
