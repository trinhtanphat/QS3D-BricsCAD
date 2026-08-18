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
                throw new InvalidOperationException("Project drawing-unit override is invalid: value is not a canonical unit token.");
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

            if (!hasBound)
            {
                projectMetadata[OverrideMetadataKey] = unit.ToString();
                return;
            }

            ApplyAtomicMetadataUpdates(
                projectMetadata,
                new KeyValuePair<string, string>(OverrideMetadataKey, unit.ToString()),
                new KeyValuePair<string, string>(EffectiveUnitMetadataKey, unit.ToString()),
                new KeyValuePair<string, string>(BindingSourceMetadataKey, DrawingUnitResolutionSource.ProjectOverride.ToString()));
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
            ApplyAtomicMetadataUpdates(
                projectMetadata,
                new KeyValuePair<string, string>(BoundMetadataKey, effectiveUnit.ToString()),
                new KeyValuePair<string, string>(EffectiveUnitMetadataKey, effectiveUnit.ToString()),
                new KeyValuePair<string, string>(BindingSourceMetadataKey, source.ToString()));
            return true;
        }

        private static void ApplyAtomicMetadataUpdates(
            IDictionary<string, string> metadata,
            params KeyValuePair<string, string>[] updates)
        {
            var snapshots = new MetadataSnapshot[updates.Length];
            for (var i = 0; i < updates.Length; i++)
            {
                var exists = metadata.TryGetValue(updates[i].Key, out var value);
                snapshots[i] = new MetadataSnapshot(updates[i].Key, exists, value);
            }

            try
            {
                for (var i = 0; i < updates.Length; i++)
                    metadata[updates[i].Key] = updates[i].Value;
            }
            catch
            {
                for (var i = snapshots.Length - 1; i >= 0; i--)
                {
                    try
                    {
                        if (snapshots[i].Exists)
                            metadata[snapshots[i].Key] = snapshots[i].Value!;
                        else
                            metadata.Remove(snapshots[i].Key);
                    }
                    catch
                    {
                        // Preserve the original mutation failure. Rollback is best-effort for a dictionary
                        // that remains permanently unwritable, while recoverable setter failures restore
                        // every touched key to its pre-call state.
                    }
                }
                throw;
            }
        }

        private readonly struct MetadataSnapshot
        {
            public MetadataSnapshot(string key, bool exists, string? value)
            {
                Key = key;
                Exists = exists;
                Value = value;
            }

            public string Key { get; }
            public bool Exists { get; }
            public string? Value { get; }
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
                throw new InvalidOperationException("Project drawing-unit metadata is invalid: " + key + " is not a canonical unit token.");
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
