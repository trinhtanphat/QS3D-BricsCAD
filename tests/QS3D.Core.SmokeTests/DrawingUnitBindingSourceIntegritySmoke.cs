using System;
using System.Collections.Generic;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class DrawingUnitBindingSourceIntegritySmoke
    {
        internal static void Run()
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sentinel"] = "unchanged"
            };

            Throws<ArgumentOutOfRangeException>(() =>
                DrawingUnitResolutionPolicy.BindQuantityUnit(
                    metadata,
                    false,
                    LengthUnit.Meter,
                    (DrawingUnitResolutionSource)999));

            if (metadata.Count != 1 || !metadata.TryGetValue("sentinel", out var sentinel) || sentinel != "unchanged")
                throw new InvalidOperationException("Invalid binding-source rejection mutated project metadata.");

            if (metadata.ContainsKey(DrawingUnitResolutionPolicy.BoundMetadataKey) ||
                metadata.ContainsKey(DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey) ||
                metadata.ContainsKey(DrawingUnitResolutionPolicy.BindingSourceMetadataKey))
                throw new InvalidOperationException("Invalid binding-source rejection persisted partial unit metadata.");

            var valid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!DrawingUnitResolutionPolicy.BindQuantityUnit(
                    valid,
                    false,
                    LengthUnit.Meter,
                    DrawingUnitResolutionSource.ProjectOverride))
                throw new InvalidOperationException("Valid binding source was unexpectedly rejected.");

            if (!valid.TryGetValue(DrawingUnitResolutionPolicy.BindingSourceMetadataKey, out var source) ||
                source != DrawingUnitResolutionSource.ProjectOverride.ToString())
                throw new InvalidOperationException("Valid binding source was not persisted canonically.");
        }

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException)
            {
                return;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }
}
