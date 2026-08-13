using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SourceHandleBoundaryResourceBoundSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            BoundaryCapacityIsAccepted();
            BoundaryOverflowFailsClosed();
            OrdinaryCanonicalBoundaryHandlesRemainResolvable();
        }

        private static void BoundaryCapacityIsAccepted()
        {
            var project = ProjectWithBoundaryHandles("LOCATE-BOUND-5000", 5000);
            var handles = SourceHandleResolver.Resolve(project, new[] { "ROOM-1" });
            if (handles.Count != 5000 ||
                !string.Equals(handles[0], "H0000", StringComparison.Ordinal) ||
                !string.Equals(handles[4999], "H4999", StringComparison.Ordinal))
                throw new InvalidOperationException("Locate boundary-handle capacity changed while bounding persisted tokenization.");
        }

        private static void BoundaryOverflowFailsClosed()
        {
            var project = ProjectWithBoundaryHandles("LOCATE-BOUND-5001", 5001);
            try
            {
                SourceHandleResolver.Resolve(project, new[] { "ROOM-1" });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf("cannot exceed 5000", StringComparison.OrdinalIgnoreCase) >= 0) return;
                throw new InvalidOperationException("Locate boundary-handle overflow failed for the wrong reason.", ex);
            }
            throw new InvalidOperationException("Locate accepted more boundary source handles than Room boundary discovery can support.");
        }

        private static void OrdinaryCanonicalBoundaryHandlesRemainResolvable()
        {
            var project = new ProjectState("LOCATE-BOUND-ORDINARY", "Locate boundary resource bound smoke");
            var room = new ProjectElement("ROOM-1", ElementCategory.Room);
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = "AA;BB;CC";
            project.Elements.Add(room);

            var handles = SourceHandleResolver.Resolve(project, new[] { "ROOM-1" });
            if (handles.Count != 3 ||
                !string.Equals(handles[0], "AA", StringComparison.Ordinal) ||
                !string.Equals(handles[1], "BB", StringComparison.Ordinal) ||
                !string.Equals(handles[2], "CC", StringComparison.Ordinal))
                throw new InvalidOperationException("Ordinary canonical Locate boundary handles changed while bounding tokenization.");
        }

        private static ProjectState ProjectWithBoundaryHandles(string projectId, int count)
        {
            var project = new ProjectState(projectId, "Locate boundary resource bound smoke");
            var room = new ProjectElement("ROOM-1", ElementCategory.Room);
            room.Properties[AutoRoomLifecycle.BoundarySourceHandlesKey] = string.Join(";", Enumerable.Range(0, count).Select(x => "H" + x.ToString("D4")));
            project.Elements.Add(room);
            return project;
        }
    }
}
