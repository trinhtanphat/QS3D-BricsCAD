using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundarySmoke
    {
        public static void Run()
        {
            Rectangle();
            SharedWallCreatesTwoRooms();
            CrossGridCreatesFourRooms();
            DanglingTJunctionDoesNotCreateFakeRoom();
            DuplicateAndCollinearSegmentsAreStable();
            OrderAndDirectionAreStable();
            OpenNetworkHasNoRoom();
            InvalidCoordinatesAreRejected();
        }

        private static void Rectangle()
        {
            var rooms = RoomBoundaryFinder.Find(RectangleSegments(0, 0, 4, 3));
            Equal(1, rooms.Count); Near(12d, rooms[0].Area); Near(14d, rooms[0].Perimeter); Near(2d, rooms[0].Centroid.X); Near(1.5d, rooms[0].Centroid.Y); Equal(4, rooms[0].SourceIds.Count); Equal(4, rooms[0].Vertices.Count);
        }

        private static void SharedWallCreatesTwoRooms()
        {
            var segments = RectangleSegments(0, 0, 6, 3).ToList(); segments.Add(S(3, 0, 3, 3, "shared"));
            var rooms = RoomBoundaryFinder.Find(segments); Equal(2, rooms.Count); Near(9d, rooms[0].Area); Near(9d, rooms[1].Area); True(rooms.All(x => x.SourceIds.Contains("shared")));
        }

        private static void CrossGridCreatesFourRooms()
        {
            var segments = RectangleSegments(0, 0, 6, 4).ToList(); segments.Add(S(3, 0, 3, 4, "vertical")); segments.Add(S(0, 2, 6, 2, "horizontal"));
            var rooms = RoomBoundaryFinder.Find(segments); Equal(4, rooms.Count); foreach (var room in rooms) Near(6d, room.Area); True(rooms.All(x => x.Vertices.Count == 4));
        }

        private static void DanglingTJunctionDoesNotCreateFakeRoom()
        {
            var segments = RectangleSegments(0, 0, 5, 4).ToList(); segments.Add(S(2.5, 0, 2.5, 2, "spur"));
            var rooms = RoomBoundaryFinder.Find(segments); Equal(1, rooms.Count); Near(20d, rooms[0].Area); Near(18d, rooms[0].Perimeter); True(!rooms[0].SourceIds.Contains("spur"));
        }

        private static void DuplicateAndCollinearSegmentsAreStable()
        {
            var segments = new List<BoundarySegment2> { S(0, 0, 2, 0, "bottom-a"), S(2, 0, 4, 0, "bottom-b"), S(4, 0, 4, 3, "right"), S(4, 3, 0, 3, "top"), S(0, 3, 0, 0, "left"), S(0, 0, 4, 0, "duplicate-bottom") };
            var rooms = RoomBoundaryFinder.Find(segments); Equal(1, rooms.Count); Near(12d, rooms[0].Area); Near(14d, rooms[0].Perimeter); True(rooms[0].SourceIds.Contains("bottom-a")); True(rooms[0].SourceIds.Contains("duplicate-bottom"));
        }

        private static void OrderAndDirectionAreStable()
        {
            var forward = RectangleSegments(0, 0, 4, 3).ToArray();
            var reversed = new BoundarySegment2[forward.Length];
            for (var i = 0; i < forward.Length; i++)
            {
                var source = forward[forward.Length - 1 - i];
                reversed[i] = new BoundarySegment2(source.End, source.Start, source.SourceId);
            }
            var a = RoomBoundaryFinder.Find(forward).Single(); var b = RoomBoundaryFinder.Find(reversed).Single();
            Equal(a.Key, b.Key); Near(a.Area, b.Area); Near(a.Perimeter, b.Perimeter);
        }

        private static void OpenNetworkHasNoRoom() => Equal(0, RoomBoundaryFinder.Find(new[] { S(0, 0, 3, 0, "a"), S(3, 0, 3, 3, "b"), S(3, 3, 0, 3, "c") }).Count);

        private static void InvalidCoordinatesAreRejected()
        {
            Throws<ArgumentOutOfRangeException>(() => RoomBoundaryFinder.Find(new[] { S(0, 0, 1, 0, "a"), new BoundarySegment2(new Point2(double.NaN, 0), new Point2(1, 1), "bad"), S(1, 1, 0, 0, "c") }));
            Throws<InvalidOperationException>(() => RoomBoundaryFinder.Find(RectangleSegments(0, 0, 1, 1), maximumSegments: 3));
        }

        private static IEnumerable<BoundarySegment2> RectangleSegments(double x0, double y0, double x1, double y1) => new[] { S(x0, y0, x1, y0, "bottom"), S(x1, y0, x1, y1, "right"), S(x1, y1, x0, y1, "top"), S(x0, y1, x0, y0, "left") };
        private static BoundarySegment2 S(double x1, double y1, double x2, double y2, string id) => new BoundarySegment2(new Point2(x1, y1), new Point2(x2, y2), id);
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-8) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
