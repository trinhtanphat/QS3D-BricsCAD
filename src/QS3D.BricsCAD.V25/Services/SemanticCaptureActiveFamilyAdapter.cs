using System;
using System.Linq;
using Bricscad.ApplicationServices;
using QS3D.Core.Domain;
using QS3D.Core.Model;

namespace QS3D.BricsCAD.V25.Services
{
    internal static class SemanticCaptureActiveFamilyAdapter
    {
        public static bool CaptureSnapshot(Document document, ProjectState project, EntitySnapshot snapshot, ProjectFamily family)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (family == null) throw new ArgumentNullException(nameof(family));

            var owners = project.Elements
                .Where(x => x.Category == family.Category && x.SourceHandles.Any(h => string.Equals(h, snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                .Take(2)
                .ToList();
            if (owners.Count > 1) throw new InvalidOperationException("SE source ownership is ambiguous.");
            if (owners.Count == 1) owners[0].FamilyId = family.Id;

            if (!SemanticCaptureService.CaptureSnapshot(document, snapshot, family.Category)) return false;

            owners = project.Elements
                .Where(x => x.Category == family.Category && x.SourceHandles.Any(h => string.Equals(h, snapshot.Handle, StringComparison.OrdinalIgnoreCase)))
                .Take(2)
                .ToList();
            if (owners.Count != 1 || !string.Equals(owners[0].FamilyId, family.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("SE active Family binding failed.");
            return true;
        }
    }
}
