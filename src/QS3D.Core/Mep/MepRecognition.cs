using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Mep
{
    [Flags]
    public enum MepRecognitionSource
    {
        None = 0,
        Layer = 1,
        BlockName = 2,
        LayerOrBlockName = Layer | BlockName
    }

    public enum MepRecognitionDiscipline
    {
        Mep = 0,
        Structure = 1,
        Architecture = 2
    }

    public enum MepRecognitionStatus
    {
        Unmatched = 0,
        Matched = 1,
        Ambiguous = 2
    }

    public sealed class MepRecognitionRule
    {
        private readonly IReadOnlyList<string> _tokens;

        public MepRecognitionRule(
            string id,
            int priority,
            MepRecognitionDiscipline discipline,
            string category,
            IEnumerable<string> tokens,
            MepRecognitionSource source = MepRecognitionSource.LayerOrBlockName,
            MepElementKind? mepKind = null)
        {
            Id = RequireText(id, nameof(id));
            Priority = priority;
            if (!Enum.IsDefined(typeof(MepRecognitionDiscipline), discipline))
                throw new ArgumentOutOfRangeException(nameof(discipline));
            Discipline = discipline;
            Category = RequireText(category, nameof(category));
            if (source == MepRecognitionSource.None ||
                (source & ~MepRecognitionSource.LayerOrBlockName) != MepRecognitionSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));
            Source = source;
            if (discipline == MepRecognitionDiscipline.Mep)
            {
                if (!mepKind.HasValue || !Enum.IsDefined(typeof(MepElementKind), mepKind.Value))
                    throw new ArgumentException("MEP recognition rules require a valid MEP element kind.", nameof(mepKind));
            }
            else if (mepKind.HasValue)
            {
                throw new ArgumentException("Only MEP recognition rules may carry a MEP element kind.", nameof(mepKind));
            }
            MepKind = mepKind;

            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            var normalized = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
            {
                var value = RequireText(token, nameof(tokens));
                if (seen.Add(value)) normalized.Add(value);
            }
            if (normalized.Count == 0)
                throw new ArgumentException("At least one recognition token is required.", nameof(tokens));
            _tokens = new ReadOnlyCollection<string>(normalized.ToArray());
        }

        public string Id { get; }
        public int Priority { get; }
        public MepRecognitionDiscipline Discipline { get; }
        public string Category { get; }
        public MepRecognitionSource Source { get; }
        public MepElementKind? MepKind { get; }
        public IReadOnlyList<string> Tokens => _tokens;

        internal bool Matches(string layer, string blockName)
        {
            if ((Source & MepRecognitionSource.Layer) != 0 && ContainsAny(layer, _tokens)) return true;
            if ((Source & MepRecognitionSource.BlockName) != 0 && ContainsAny(blockName, _tokens)) return true;
            return false;
        }

        private static bool ContainsAny(string source, IReadOnlyList<string> tokens)
        {
            if (string.IsNullOrEmpty(source)) return false;
            for (var i = 0; i < tokens.Count; i++)
                if (source.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        private static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Recognition text is required.", parameterName);
            var trimmed = value.Trim();
            for (var i = 0; i < trimmed.Length; i++)
                if (char.IsControl(trimmed[i]))
                    throw new ArgumentException("Recognition text must not contain control characters.", parameterName);
            return trimmed;
        }
    }

    public sealed class MepRecognitionResult
    {
        private readonly IReadOnlyList<string> _matchedRuleIds;

        internal MepRecognitionResult(
            MepRecognitionStatus status,
            MepRecognitionDiscipline? discipline,
            string? category,
            MepElementKind? mepKind,
            IEnumerable<string> matchedRuleIds)
        {
            Status = status;
            Discipline = discipline;
            Category = category;
            MepKind = mepKind;
            _matchedRuleIds = new ReadOnlyCollection<string>(new List<string>(matchedRuleIds).ToArray());
        }

        public MepRecognitionStatus Status { get; }
        public MepRecognitionDiscipline? Discipline { get; }
        public string? Category { get; }
        public MepElementKind? MepKind { get; }
        public IReadOnlyList<string> MatchedRuleIds => _matchedRuleIds;
    }

    public sealed class MepRecognitionProfile
    {
        private readonly IReadOnlyList<MepRecognitionRule> _rules;

        public MepRecognitionProfile(IEnumerable<MepRecognitionRule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            var snapshot = new List<MepRecognitionRule>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;
            foreach (var rule in rules)
            {
                if (rule == null)
                    throw new ArgumentException("Recognition profile contains a null rule at index " + index + ".", nameof(rules));
                if (!ids.Add(rule.Id))
                    throw new ArgumentException("Duplicate recognition rule id: " + rule.Id + ".", nameof(rules));
                snapshot.Add(rule);
                index++;
            }
            if (snapshot.Count == 0)
                throw new ArgumentException("Recognition profile must contain at least one rule.", nameof(rules));
            snapshot.Sort(CompareRules);
            _rules = new ReadOnlyCollection<MepRecognitionRule>(snapshot.ToArray());
        }

        public IReadOnlyList<MepRecognitionRule> Rules => _rules;

        public MepRecognitionResult Recognize(string? layer, string? blockName)
        {
            var layerText = (layer ?? string.Empty).Trim();
            var blockText = (blockName ?? string.Empty).Trim();
            var highestPriority = int.MinValue;
            var topMatches = new List<MepRecognitionRule>();

            for (var i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                if (!rule.Matches(layerText, blockText)) continue;
                if (rule.Priority < highestPriority) break;
                if (rule.Priority > highestPriority)
                {
                    highestPriority = rule.Priority;
                    topMatches.Clear();
                }
                topMatches.Add(rule);
            }

            if (topMatches.Count == 0)
                return new MepRecognitionResult(MepRecognitionStatus.Unmatched, null, null, null, Array.Empty<string>());

            var first = topMatches[0];
            var ambiguous = false;
            for (var i = 1; i < topMatches.Count; i++)
            {
                if (!SameClassification(first, topMatches[i]))
                {
                    ambiguous = true;
                    break;
                }
            }

            var ruleIds = new string[topMatches.Count];
            for (var i = 0; i < topMatches.Count; i++) ruleIds[i] = topMatches[i].Id;
            if (ambiguous)
                return new MepRecognitionResult(MepRecognitionStatus.Ambiguous, null, null, null, ruleIds);

            return new MepRecognitionResult(
                MepRecognitionStatus.Matched,
                first.Discipline,
                first.Category,
                first.MepKind,
                ruleIds);
        }

        private static int CompareRules(MepRecognitionRule left, MepRecognitionRule right)
        {
            var priority = right.Priority.CompareTo(left.Priority);
            return priority != 0 ? priority : StringComparer.OrdinalIgnoreCase.Compare(left.Id, right.Id);
        }

        private static bool SameClassification(MepRecognitionRule left, MepRecognitionRule right) =>
            left.Discipline == right.Discipline &&
            StringComparer.OrdinalIgnoreCase.Equals(left.Category, right.Category) &&
            left.MepKind == right.MepKind;
    }

    public static class MepRecognitionProfiles
    {
        public static MepRecognitionProfile CreateDefault() => new MepRecognitionProfile(new[]
        {
            Mep("mep.cable-tray", 900, "CableTray", MepElementKind.CableTray, "CABLETRAY", "CABLE_TRAY", "CABLE-TRAY", "TRAY"),
            Mep("mep.conduit", 890, "Conduit", MepElementKind.Conduit, "CONDUIT"),
            Mep("mep.duct", 880, "Duct", MepElementKind.Duct, "DUCT"),
            Mep("mep.pipe", 870, "Pipe", MepElementKind.Pipe, "PIPE", "PIPING"),
            Mep("mep.cable", 860, "Cable", MepElementKind.Cable, "CABLE", "WIRE"),
            Mep("mep.fitting", 850, "Fitting", MepElementKind.Fitting, "FITTING", "ELBOW", "REDUCER", "COUPLING", "TEE_", "TEE-"),
            Mep("mep.accessory", 840, "Accessory", MepElementKind.Accessory, "VALVE", "DAMPER", "ACCESSORY"),
            Mep("mep.equipment", 830, "Equipment", MepElementKind.Equipment, "EQUIP", "AHU", "FCU", "PUMP", "FAN", "CHILLER", "BOILER"),
            Mep("mep.fixture", 820, "Fixture", MepElementKind.Fixture, "FIXTURE", "LUMINAIRE", "LIGHTING", "LIGHT_", "LIGHT-", "SOCKET", "OUTLET", "SWITCH", "SANITARY", "SPRINKLER"),
            Building("structure.beam", 700, MepRecognitionDiscipline.Structure, "Beam", "BEAM"),
            Building("structure.column", 690, MepRecognitionDiscipline.Structure, "Column", "COLUMN"),
            Building("structure.foundation", 680, MepRecognitionDiscipline.Structure, "Foundation", "FOOTING", "FOUNDATION", "PILE"),
            Building("structure.generic", 670, MepRecognitionDiscipline.Structure, "Structure", "STRUCT", "RC_", "RC-"),
            Building("architecture.wall", 600, MepRecognitionDiscipline.Architecture, "Wall", "WALL"),
            Building("architecture.slab", 590, MepRecognitionDiscipline.Architecture, "Slab", "SLAB", "FLOOR"),
            Building("architecture.ceiling", 580, MepRecognitionDiscipline.Architecture, "Ceiling", "CEILING"),
            Building("architecture.roof", 570, MepRecognitionDiscipline.Architecture, "Roof", "ROOF"),
            Building("architecture.generic", 560, MepRecognitionDiscipline.Architecture, "Architecture", "ARCH")
        });

        private static MepRecognitionRule Mep(
            string id,
            int priority,
            string category,
            MepElementKind kind,
            params string[] tokens) =>
            new MepRecognitionRule(id, priority, MepRecognitionDiscipline.Mep, category, tokens, MepRecognitionSource.LayerOrBlockName, kind);

        private static MepRecognitionRule Building(
            string id,
            int priority,
            MepRecognitionDiscipline discipline,
            string category,
            params string[] tokens) =>
            new MepRecognitionRule(id, priority, discipline, category, tokens);
    }
}
