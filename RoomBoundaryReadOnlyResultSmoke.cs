using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryRectangleRemainsReadOnly();
        }

        private static void OrdinaryRectangleRemainsReadOnly()
        {
            var engine = new RoomBoundaryEngine();
            var boundaries = engine.Discover(new[]
            {
                new BoundarySegment(new Point2(0d, 0d), new Point2(4d, 0d), "S1"),
                new BoundarySegment(new Point2(4d, 0d), new Point2(4d, 3d), "S2"),
                new BoundarySegment(new Point2(4d, 3d), new Point2(0d, 3d), "S3"),
                new BoundarySegment(new Point2(0d, 3d), new Point2(0d, 0d), "S4")
            });

            if (boundaries.Count != 1)
                throw new InvalidOperationException("Rectangle discovery must still produce exactly one Room boundary.");

            var boundary = boundaries[0];
            if (Math.Abs(boundary.Area - 12d) > 1e-9d || Math.Abs(boundary.Perimeter - 14d) > 1e-9d)
                throw new InvalidOperationException("Room boundary area/perimeter changed while hardening the result boundary.");
            if (boundary.Vertices.Count != 4 || boundary.SourceIds.Count != 4)
                throw new InvalidOperationException("Room boundary vertex/source snapshots changed while hardening the result boundary.");

            if (!(boundaries is ICollection<RoomBoundary> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Room boundary discovery must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(boundary);
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Room boundary discovery accepted structural mutation through ICollection<T>.");
        }
    }
}
