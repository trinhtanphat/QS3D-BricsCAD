using System;
using Teigha.DatabaseServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class GeneratedNativeSourceGuard
    {
        // These RegApp names are written by QS3D native generated-output services.
        // A present marker is sufficient to reject the entity as a source even when
        // its sidecar project is missing or the marker payload is legacy/malformed.
        private static readonly string[] GeneratedOwnershipRegApps =
        {
            "QS3D",
            "QS3D_REBAR",
            "QS3D_CURTAIN_FRAME",
            "QS3D_CURTAIN_PANEL",
            "QS3DDOC"
        };

        public static bool HasKnownOwnershipMarker(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            foreach (var regAppName in GeneratedOwnershipRegApps)
            {
                using (var marker = entity.GetXDataForApplication(regAppName))
                    if (marker != null) return true;
            }
            return false;
        }
    }
}
