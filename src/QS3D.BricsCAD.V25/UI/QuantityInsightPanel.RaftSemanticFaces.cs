using System;
using System.Globalization;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private static void AssignRaftQuantitySemanticFaceKeys(ProjectState project, QuantityGeometryExplanation geometry)
        {
            if (project == null || geometry == null) return;
            var element = project.FindElement(geometry.ElementId);
            var family = element == null ? null : project.FindFamily(element.FamilyId);
            if (!RaftFoundationPropertySet.IsRaftElement(element, family)) return;

            // The semantic key is intentionally recomputed from the current exact-BREP snapshot.
            // Native SubentityId is never persisted as identity. FaceId remains the short-lived
            // resolver for one snapshot; SemanticKey is the stable row identity used to re-resolve
            // the corresponding current face after a safe refresh/regeneration boundary.
            var sideFaces = geometry.FormworkFaces
                .Where(x => x != null && string.Equals(x.FaceType, "Side", StringComparison.Ordinal))
                .OrderBy(x => x.FaceId, StringComparer.Ordinal)
                .ToList();

            for (var index = 0; index < sideFaces.Count; index++)
                sideFaces[index].SemanticKey = "Side:OuterLoop:Edge" + index.ToString(CultureInfo.InvariantCulture);
        }

        private static QuantityFormworkFaceExplanation? ResolveFreshRaftQuantityFace(
            QuantityGeometryExplanation? displayed,
            QuantityGeometryExplanation fresh,
            string displayedFaceId)
        {
            if (fresh == null || string.IsNullOrWhiteSpace(displayedFaceId)) return null;
            var displayedFace = displayed?.FormworkFaces
                .SingleOrDefault(x => string.Equals(x.FaceId, displayedFaceId, StringComparison.Ordinal));
            if (displayedFace == null || string.IsNullOrWhiteSpace(displayedFace.SemanticKey))
                return fresh.FormworkFaces.SingleOrDefault(x => string.Equals(x.FaceId, displayedFaceId, StringComparison.Ordinal));

            var semanticMatches = fresh.FormworkFaces
                .Where(x => string.Equals(x.SemanticKey, displayedFace.SemanticKey, StringComparison.Ordinal))
                .ToList();
            return semanticMatches.Count == 1 ? semanticMatches[0] : null;
        }
    }
}