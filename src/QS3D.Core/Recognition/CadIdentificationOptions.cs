using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Recognition
{
    public enum BeamSizeReadingMode
    {
        WidthByHeight = 0,
        HeightByWidth = 1
    }

    public enum BeamEndExtensionMode
    {
        IntoHost = 0,
        NearestHostFace = 1
    }

    public enum CadImportEntityKind
    {
        Geometry = 0,
        Text = 1,
        Hatch = 2
    }

    public sealed class IdentificationColorRule
    {
        public IdentificationColorRule(int colorIndex, string classification)
        {
            if (colorIndex < 0 || colorIndex > 255)
                throw new ArgumentOutOfRangeException(nameof(colorIndex));
            if (string.IsNullOrWhiteSpace(classification))
                throw new ArgumentException("Identification classification is required.", nameof(classification));
            ColorIndex = colorIndex;
            Classification = classification.Trim();
        }

        public int ColorIndex { get; }
        public string Classification { get; }
    }

    public sealed class CadIdentificationOptions
    {
        private const int MaximumColorRules = 256;
        private readonly IReadOnlyDictionary<int, string> _classificationByColor;

        public CadIdentificationOptions(
            bool importHatches = true,
            bool selectByColor = false,
            BeamSizeReadingMode beamSizeReadingMode = BeamSizeReadingMode.WidthByHeight,
            BeamEndExtensionMode beamEndExtensionMode = BeamEndExtensionMode.IntoHost,
            double beamAutoExtensionTolerance = 0d,
            bool identifyPdfText = true,
            bool allowCadEntityRestore = true,
            IEnumerable<IdentificationColorRule>? colorRules = null)
        {
            if (!Enum.IsDefined(typeof(BeamSizeReadingMode), beamSizeReadingMode))
                throw new ArgumentOutOfRangeException(nameof(beamSizeReadingMode));
            if (!Enum.IsDefined(typeof(BeamEndExtensionMode), beamEndExtensionMode))
                throw new ArgumentOutOfRangeException(nameof(beamEndExtensionMode));
            if (double.IsNaN(beamAutoExtensionTolerance) ||
                double.IsInfinity(beamAutoExtensionTolerance) ||
                beamAutoExtensionTolerance < 0d)
                throw new ArgumentOutOfRangeException(nameof(beamAutoExtensionTolerance));

            ImportHatches = importHatches;
            SelectByColor = selectByColor;
            BeamSizeReadingMode = beamSizeReadingMode;
            BeamEndExtensionMode = beamEndExtensionMode;
            BeamAutoExtensionTolerance = beamAutoExtensionTolerance == 0d ? 0d : beamAutoExtensionTolerance;
            IdentifyPdfText = identifyPdfText;
            AllowCadEntityRestore = allowCadEntityRestore;

            var byColor = new Dictionary<int, string>();
            if (colorRules != null)
            {
                var knownCount = TryGetKnownColorRuleCount(
                    colorRules,
                    out var conflictingKnownCounts,
                    out var negativeKnownCount);
                if (negativeKnownCount)
                    throw new InvalidOperationException("Identification color-rule source exposes an invalid negative known Count value.");
                if (conflictingKnownCounts)
                    throw new InvalidOperationException("Identification color-rule source exposes conflicting known Count values.");
                if (knownCount.HasValue && knownCount.Value > MaximumColorRules)
                    throw new InvalidOperationException("Identification color rules support at most 256 entries.");

                var index = 0;
                foreach (var rule in colorRules)
                {
                    if (knownCount.HasValue && index >= knownCount.Value)
                        throw new InvalidOperationException(
                            "Identification color-rule source traversal produced more entries than its known Count reported " + knownCount.Value + ".");
                    if (index == MaximumColorRules)
                        throw new InvalidOperationException("Identification color rules support at most 256 entries.");
                    if (rule == null)
                        throw new ArgumentException("Identification color rules contain a null item at index " + index + ".", nameof(colorRules));
                    if (byColor.ContainsKey(rule.ColorIndex))
                        throw new ArgumentException("Duplicate identification color index: " + rule.ColorIndex + ".", nameof(colorRules));
                    byColor.Add(rule.ColorIndex, rule.Classification);
                    index++;
                }

                if (knownCount.HasValue && index != knownCount.Value)
                    throw new InvalidOperationException(
                        "Identification color-rule source known Count does not match completed traversal cardinality.");
            }
            _classificationByColor = new ReadOnlyDictionary<int, string>(byColor);
        }

        public bool ImportHatches { get; }
        public bool SelectByColor { get; }
        public BeamSizeReadingMode BeamSizeReadingMode { get; }
        public BeamEndExtensionMode BeamEndExtensionMode { get; }
        public double BeamAutoExtensionTolerance { get; }
        public bool IdentifyPdfText { get; }
        public bool AllowCadEntityRestore { get; }
        public IReadOnlyDictionary<int, string> ClassificationByColor => _classificationByColor;

        private static int? TryGetKnownColorRuleCount(
            IEnumerable<IdentificationColorRule> colorRules,
            out bool conflictingKnownCounts,
            out bool negativeKnownCount)
        {
            conflictingKnownCounts = false;
            negativeKnownCount = false;
            int? knownCount = null;

            if (colorRules is ICollection<IdentificationColorRule> genericCollection)
                knownCount = ObserveKnownCount(knownCount, genericCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (colorRules is IReadOnlyCollection<IdentificationColorRule> readOnlyCollection)
                knownCount = ObserveKnownCount(knownCount, readOnlyCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);
            if (colorRules is ICollection nonGenericCollection)
                knownCount = ObserveKnownCount(knownCount, nonGenericCollection.Count, ref conflictingKnownCounts, ref negativeKnownCount);

            return knownCount;
        }

        private static int ObserveKnownCount(
            int? current,
            int observed,
            ref bool conflictingKnownCounts,
            ref bool negativeKnownCount)
        {
            if (observed < 0)
                negativeKnownCount = true;
            if (current.HasValue && current.Value != observed)
                conflictingKnownCounts = true;
            return !current.HasValue || observed > current.Value ? observed : current.Value;
        }
    }

    public sealed class BeamSize
    {
        internal BeamSize(double width, double height)
        {
            Width = width;
            Height = height;
        }

        public double Width { get; }
        public double Height { get; }
    }

    public sealed class CadIdentificationPlanner
    {
        public bool ShouldImport(CadImportEntityKind kind, CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (!Enum.IsDefined(typeof(CadImportEntityKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            return kind != CadImportEntityKind.Hatch || options.ImportHatches;
        }

        public string? ResolveClassificationByColor(int colorIndex, CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (colorIndex < 0 || colorIndex > 255)
                throw new ArgumentOutOfRangeException(nameof(colorIndex));
            if (!options.SelectByColor) return null;
            return options.ClassificationByColor.TryGetValue(colorIndex, out var classification)
                ? classification
                : null;
        }

        public BeamSize ReadBeamSize(double first, double second, CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            first = RequirePositiveFinite(first, nameof(first));
            second = RequirePositiveFinite(second, nameof(second));
            return options.BeamSizeReadingMode == BeamSizeReadingMode.WidthByHeight
                ? new BeamSize(first, second)
                : new BeamSize(second, first);
        }

        public double ResolveBeamExtensionDistance(
            double distanceToHostCenter,
            double distanceToNearestHostFace,
            CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            distanceToHostCenter = RequireNonNegativeFinite(distanceToHostCenter, nameof(distanceToHostCenter));
            distanceToNearestHostFace = RequireNonNegativeFinite(distanceToNearestHostFace, nameof(distanceToNearestHostFace));
            return options.BeamEndExtensionMode == BeamEndExtensionMode.IntoHost
                ? distanceToHostCenter
                : distanceToNearestHostFace;
        }

        public bool CanAutoExtendBeamEnd(double gap, CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            gap = RequireNonNegativeFinite(gap, nameof(gap));
            return gap <= options.BeamAutoExtensionTolerance;
        }

        public bool CanIdentifyPdfText(CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return options.IdentifyPdfText;
        }

        public bool CanRestoreCadEntity(CadIdentificationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return options.AllowCadEntityRestore;
        }

        private static double RequirePositiveFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Beam dimensions must be finite and greater than zero.");
            return value;
        }

        private static double RequireNonNegativeFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Identification distances must be finite and non-negative.");
            return value == 0d ? 0d : value;
        }
    }
}