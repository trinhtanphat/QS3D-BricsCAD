using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using QS3D.Core.Geometry;

namespace QS3D.BricsCAD.V25.Cad
{
    /// <summary>
    /// Computes the deterministic persisted fingerprint for one complete multi-region
    /// source topology. Both the materializer and read-only Health use the same
    /// canonical source-handle / geometry-fingerprint representation.
    /// </summary>
    internal static class MultiRegionTopologyFingerprint
    {
        public static string Compute(
            PolygonSourceRegionAssembly2 assembly,
            IReadOnlyDictionary<string, string> fingerprintByHandle)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            if (fingerprintByHandle == null) throw new ArgumentNullException(nameof(fingerprintByHandle));

            var canonicalFingerprints = fingerprintByHandle.ToDictionary(
                pair => CanonicalHandle(pair.Key, "multi-region source fingerprint handle"),
                pair => string.IsNullOrWhiteSpace(pair.Value)
                    ? throw new InvalidOperationException("Multi-region source " + pair.Key + " has no deterministic geometry fingerprint.")
                    : pair.Value.Trim(),
                StringComparer.OrdinalIgnoreCase);

            var canonical = new StringBuilder();
            foreach (var region in assembly.Regions.OrderBy(x => x.RegionId, StringComparer.Ordinal))
            {
                if (canonical.Length > 0) canonical.Append(';');
                canonical.Append("R=").Append(CanonicalHandle(region.RegionId, "multi-region RegionId"));
                AppendSourceFingerprint(canonical, "O", region.OuterSourceId, canonicalFingerprints);
                foreach (var hole in region.HoleSourceIds
                    .Select(handle => CanonicalHandle(handle, "multi-region hole source handle"))
                    .OrderBy(x => x, StringComparer.Ordinal))
                {
                    AppendSourceFingerprint(canonical, "H", hole, canonicalFingerprints);
                }
            }

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static void AppendSourceFingerprint(
            StringBuilder target,
            string role,
            string sourceHandle,
            IReadOnlyDictionary<string, string> fingerprintByHandle)
        {
            var handle = CanonicalHandle(sourceHandle, "multi-region topology source handle");
            string fingerprint;
            if (!fingerprintByHandle.TryGetValue(handle, out fingerprint) || string.IsNullOrWhiteSpace(fingerprint))
                throw new InvalidOperationException("Multi-region topology source " + sourceHandle + " has no deterministic geometry fingerprint.");
            target.Append('|').Append(role).Append('=').Append(handle).Append(':').Append(fingerprint.Trim());
        }

        private static string CanonicalHandle(string handle, string label)
        {
            var canonical = CadHandleService.NormalizeHexHandle(handle);
            if (canonical == null)
                throw new InvalidOperationException(label + " is not a valid positive CAD handle: " + (handle ?? "<null>") + ".");
            return canonical;
        }
    }
}
