using System;
using System.Collections.Generic;

namespace QS3D.Core.Units
{
    public enum DrawingUnitResolutionSource
    {
        NativeInsunits,
        ProjectOverride
    }

    public sealed class DrawingUnitResolution
    {
        internal DrawingUnitResolution(LengthUnit unit, DrawingUnitResolutionSource source)
        {
            Unit = unit;
            Source = source;
        }

        public LengthUnit Unit { get; }
        public DrawingUnitResolutionSource Source { get; }
    }

    public static class DrawingUnitResolutionPolicy
    {
        public const string OverrideMetadataKey = "QS3D.DrawingUnitOverride.v1";
        public const string BoundMetadataKey = "QS3D.DrawingUnitBound.v1";
        public const string BindingSourceMetadataKey = "QS3D.DrawingUnitBindingSource.v1";
        public const string EffectiveUnitMetadataKey = "QS3D.DrawingUnit";
        public const string LegacyAssumptionMetadataKey = "QS3D.DrawingUnitAssumption";

        public static bool TryResolve(
            LengthUnit? nativeUnit,
            IDictionary<string, string> projectMetadata,
            out DrawingUnitResolution resolution)
        {
            if (projectMetadata == null) throw new ArgumentNullException(nameof(projectMetadata));
            if (nativeUnit.HasValue)
            {
                Validate(nativeUnit.Value);
                resolution = new DrawingUnitResolution(nativeUnit.Value, DrawingUnitResolutionSource.NativeInsunits);
                return true;
            }

            if (!projectMetadata.TryGetValue(OverrideMetadataKey, out var raw))
            {
                resolution = null!;
                return false;
            }
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Project drawing-unit override is invalid: value is blank.");

            if (!TryParseNamedUnitToken(raw, out var parsed))
                throw new InvalidOperationException("Project drawing-unit override is invalid: " + raw + ".");
            resolution = new DrawingUnitResolution(parsed, DrawingUnitResolutionSource.ProjectOverride);
            return true;
        }

        public static void SetProjectOverride(IDictionary<string, string> projectMetadata, LengthUnit unit)
        {
            if (projectMetadata == null) throw new ArgumentNullException(nameof(projectMetadata));
            Validate(unit);
            var hasBound = TryReadCanonical(projectMetadata, BoundMetadataKey, out var bound);
            if (hasBound && bound != unit)
                throw new InvalidOperationException("Drawing unit " + unit + " does not match quantities bound to " + bound + ". Remeasure source geometry before changing units.");

            var preserveUnboundLegacyEvidence =
                !hasBound && projectMetadata.ContainsKey(EffectiveUnitMetadataKey);
            projectMetadata[OverrideMetadataKey] = unit.ToString();
            if (preserveUnboundLegacyEvidence) return;

            projectMetadata[EffectiveUnitMetadataKey] = unit.ToString();
            projectMetadata[BindingSourceMetadataKey] = DrawingUnitResolutionSource.ProjectOverride.ToString();
        }

        public static void ValidateQuantityCompatibility(
            IDictionary<string, string> projectMetadata,
            bool hasElements,
            LengthUnit effectiveUnit)
        {
            if (projectMetadata == null) throw new ArgumentNullException(nameof(projectMetadata));
            Validate(effectiveUnit);
            if (TryReadCanonical(projectMetadata, BoundMetadataKey, out var bound))
            {
                if (bound != effectiveUnit)
                    throw new InvalidOperationException("Drawing unit " + effectiveUnit + " does not match quantities bound to " + bound + ". Remeasure source geometry before changing units.");
                return;
            }
            if (!hasElements) return;
            if (TryReadLegacyEffectiveUnit(projectMetadata, out var legacy) && legacy == effectiveUnit) return;
            throw new InvalidOperationException("Existing semantic quantities have no trustworthy drawing-unit binding. Keep the legacy unit or remeasure source geometry before export.");
        }

        public static bool BindQuantityUnit(
            IDictionary<string, string> projectMetadata,
            bool hasElements,
            LengthUnit effectiveUnit,
            DrawingUnitResolutionSource source)
        {
            if (!Enum.IsDefined(typeof(DrawingUnitResolutionSource), source)) throw new ArgumentOutOfRangeException(nameof(source));
            ValidateQuantityCompatibility(projectMetadata, hasElements, effectiveUnit);
            if (TryReadCanonical(projectMetadata, BoundMetadataKey, out _)) return false;
            projectMetadata[BoundMetadataKey] = effectiveUnit.ToString();
            projectMetadata[EffectiveUnitMetadataKey] = effectiveUnit.ToString();
            projectMetadata[BindingSourceMetadataKey] = source.ToString();
            return true;
        }

        private static bool TryReadLegacyEffectiveUnit(IDictionary<string, string> metadata, out LengthUnit unit)
        {
            unit = default(LengthUnit);
            if (!metadata.TryGetValue(EffectiveUnitMetadataKey, out var raw) || string.IsNullOrWhiteSpace(raw)) return false;
            var token = raw.Trim();
            var suffix = token.IndexOf(' ');
            if (suffix > 0) token = token.Substring(0, suffix);
            return Enum.TryParse(token, true, out unit) && Enum.IsDefined(typeof(LengthUnit), unit);
        }

        private static bool TryReadCanonical(IDictionary<string, string> metadata, string key, out LengthUnit unit)
        {
            unit = default(LengthUnit);
            if (!metadata.TryGetValue(key, out var raw)) return false;
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidOperationException("Project drawing-unit metadata is invalid: " + key + " is blank.");
            if (!TryParseNamedUnitToken(raw, out unit))
                throw new InvalidOperationException("Project drawing-unit metadata is invalid: " + key + "=" + raw + ".");
            return true;
        }

        private static bool TryParseNamedUnitToken(string raw, out LengthUnit unit)
        {
            unit = default(LengthUnit);
            var token = raw ?? string.Empty;
            if (!string.Equals(token, token.Trim(), StringComparison.Ordinal)) return false;
            if (!Enum.TryParse(token, true, out unit) || !Enum.IsDefined(typeof(LengthUnit), unit)) return false;
            var name = Enum.GetName(typeof(LengthUnit), unit);
            return name != null && string.Equals(token, name, StringComparison.OrdinalIgnoreCase);
        }

        private static void Validate(LengthUnit unit)
        {
            if (!Enum.IsDefined(typeof(LengthUnit), unit)) throw new ArgumentOutOfRangeException(nameof(unit));
        }
    }
}
