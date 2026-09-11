using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;

namespace QS3D.Core.Intelligence
{
    /// <summary>
    /// Deterministic, offline QS classifier used as a safe fallback when no external AI provider is configured.
    /// The vocabulary intentionally covers common English and Vietnamese construction terminology.
    /// </summary>
    public sealed class RuleBasedQsClassifier : IQsClassificationProvider
    {
        private readonly IReadOnlyList<Rule> _rules;

        public RuleBasedQsClassifier()
            : this(CreateDefaultRules())
        {
        }

        internal RuleBasedQsClassifier(IEnumerable<Rule> rules)
        {
            if (rules == null) throw new ArgumentNullException(nameof(rules));
            var snapshot = new List<Rule>();
            foreach (var rule in rules)
            {
                if (rule == null) throw new ArgumentException("Classification rule must not be null.", nameof(rules));
                snapshot.Add(rule);
            }
            _rules = new ReadOnlyCollection<Rule>(snapshot.ToArray());
        }

        public QsClassificationResult? Classify(QsQuantityRecord record)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            var searchable = BuildSearchableText(record);
            Rule? best = null;
            List<string>? bestMatches = null;
            var bestScore = -1;
            var bestSpecificity = -1;

            for (var i = 0; i < _rules.Count; i++)
            {
                var rule = _rules[i];
                var matches = new List<string>();
                var specificity = 0;
                for (var j = 0; j < rule.Terms.Count; j++)
                {
                    var term = rule.Terms[j];
                    if (searchable.IndexOf(term, StringComparison.Ordinal) < 0) continue;
                    matches.Add(term);
                    specificity += term.Length;
                }

                if (matches.Count == 0) continue;
                var score = matches.Count;
                if (score < bestScore || (score == bestScore && specificity <= bestSpecificity)) continue;
                best = rule;
                bestMatches = matches;
                bestScore = score;
                bestSpecificity = specificity;
            }

            if (best == null || bestMatches == null) return null;
            var confidence = 0.68d + Math.Min(0.22d, bestMatches.Count * 0.06d) + Math.Min(0.09d, bestSpecificity / 250d);
            if (confidence > 0.99d) confidence = 0.99d;
            return new QsClassificationResult(best.Discipline, best.ClassificationCode, confidence, bestMatches);
        }

        public IReadOnlyList<QsQuantityRecord> ClassifyMissing(IEnumerable<QsQuantityRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));
            var result = new List<QsQuantityRecord>();
            foreach (var record in records)
            {
                if (record == null) throw new ArgumentException("QS record must not be null.", nameof(records));
                if (!string.IsNullOrEmpty(record.ClassificationCode))
                {
                    result.Add(record);
                    continue;
                }

                var classification = Classify(record);
                result.Add(classification == null
                    ? record
                    : record.WithClassification(classification.Discipline, classification.ClassificationCode));
            }
            return new ReadOnlyCollection<QsQuantityRecord>(result.ToArray());
        }

        private static string BuildSearchableText(QsQuantityRecord record)
        {
            var builder = new StringBuilder();
            Append(builder, record.Category);
            Append(builder, record.Name);
            Append(builder, record.Discipline);
            Append(builder, record.QuantityType);
            foreach (var pair in record.Properties)
            {
                Append(builder, pair.Key);
                Append(builder, pair.Value);
            }
            return Fold(builder.ToString());
        }

        private static void Append(StringBuilder builder, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            if (builder.Length > 0) builder.Append(' ');
            builder.Append(value);
        }

        private static string Fold(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            for (var i = 0; i < normalized.Length; i++)
            {
                var c = normalized[i];
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                builder.Append(char.ToLowerInvariant(c));
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static IReadOnlyList<Rule> CreateDefaultRules()
        {
            return new ReadOnlyCollection<Rule>(new[]
            {
                new Rule("Structure", "STR.REBAR", "reinforcement", "rebar", "steel bar", "cot thep", "thep thanh"),
                new Rule("Structure", "STR.FORMWORK", "formwork", "shuttering", "van khuon", "cop pha", "coffa"),
                new Rule("Structure", "STR.FOUNDATION", "foundation", "footing", "pile cap", "raft", "mong", "dai mong"),
                new Rule("Structure", "STR.BEAM", "concrete beam", "rc beam", "beam", "dam be tong", "dam"),
                new Rule("Structure", "STR.COLUMN", "concrete column", "rc column", "column", "cot be tong", "cot"),
                new Rule("Structure", "STR.SLAB", "concrete slab", "rc slab", "slab", "san be tong", "san"),
                new Rule("Structure", "STR.WALL", "shear wall", "concrete wall", "rc wall", "vach", "tuong be tong"),
                new Rule("Structure", "STR.STEEL", "structural steel", "steel structure", "steel frame", "ket cau thep", "khung thep"),
                new Rule("Architecture", "ARC.MASONRY", "masonry", "brickwork", "blockwork", "tuong gach", "xay gach", "gach"),
                new Rule("Architecture", "ARC.WALL", "architectural wall", "partition wall", "partition", "tuong ngan", "wall"),
                new Rule("MEP", "MEP.CABLE_TRAY", "cable tray", "cable ladder", "mang cap", "thang cap"),
                new Rule("MEP", "MEP.DUCT", "air duct", "ductwork", "duct", "ong gio"),
                new Rule("MEP", "MEP.PIPE", "chilled water pipe", "fire pipe", "plumbing pipe", "pipeline", "pipe", "ong nuoc", "ong cap", "ong thoat"),
                new Rule("MEP", "MEP.EQUIPMENT", "mep equipment", "mechanical equipment", "electrical equipment", "fcu", "ahu", "pump", "equipment", "thiet bi"),
                new Rule("Civil", "CIVIL.EXCAVATION", "excavation", "earth excavation", "dao dat", "dao mong"),
                new Rule("Civil", "CIVIL.BACKFILL", "backfill", "earth fill", "dap dat", "lap dat"),
                new Rule("Civil", "CIVIL.CUTFILL", "cut and fill", "cut fill", "earthwork balance", "san lap", "dao dap"),
                new Rule("Civil", "CIVIL.TERRAIN", "terrain", "topography", "surface model", "dia hinh", "dia mao"),
                new Rule("Civil", "CIVIL.ROAD", "roadwork", "road", "pavement", "asphalt", "duong giao thong", "mat duong")
            });
        }

        internal sealed class Rule
        {
            internal Rule(string discipline, string classificationCode, params string[] terms)
            {
                Discipline = QsQuantityRecord.RequireText(discipline, nameof(discipline));
                ClassificationCode = QsQuantityRecord.RequireToken(classificationCode, nameof(classificationCode));
                if (terms == null || terms.Length == 0) throw new ArgumentException("Rule terms are required.", nameof(terms));
                var normalized = new List<string>();
                for (var i = 0; i < terms.Length; i++)
                {
                    var term = Fold(QsQuantityRecord.RequireText(terms[i], nameof(terms)));
                    if (!normalized.Contains(term)) normalized.Add(term);
                }
                Terms = new ReadOnlyCollection<string>(normalized.ToArray());
            }

            internal string Discipline { get; }
            internal string ClassificationCode { get; }
            internal IReadOnlyList<string> Terms { get; }
        }
    }
}
