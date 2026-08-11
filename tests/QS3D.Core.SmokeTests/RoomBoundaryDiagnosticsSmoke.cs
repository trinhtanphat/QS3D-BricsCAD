using System;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryDiagnosticsSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            ClassifiesNoInputAndInsufficientSegments();
            ClassifiesOpenNetwork();
            ExplainsMinimumAreaRejection();
            AcceptedFaceProvenanceIsDeterministicAndPrivacySafe();
        }

        private static void ClassifiesNoInputAndInsufficientSegments()
        {
            var service = new RoomBoundaryDiagnosticService();
            var empty = service.Analyze(Array.Empty<BoundarySegment>()).Report;
            Equal(RoomBoundaryDiagnosticReason.NoInput, empty.Reason);
            Equal(0, empty.InputSegmentCount);
            Equal(0, empty.CandidateBoundaryCount);

            var shortNetwork = service.Analyze(new[]
            {
                Segment(0, 0, 1, 0, "H-A"),
                Segment(1, 0, 2, 0, "H-B")
            }).Report;
            Equal(RoomBoundaryDiagnosticReason.InsufficientSegments, shortNetwork.Reason);
            Equal(2, shortNetwork.InputSegmentCount);
            Equal(2, shortNetwork.UniqueSourceCount);
            Equal(0, shortNetwork.CandidateBoundaryCount);
        }

        private static void ClassifiesOpenNetwork()
        {
            var report = new RoomBoundaryDiagnosticService().Analyze(new[]
            {
                Segment(0, 0, 1, 0, "H-A"),
                Segment(1, 0, 2, 0, "H-B"),
                Segment(2, 0, 2, 1, "H-C")
            }).Report;
            Equal(RoomBoundaryDiagnosticReason.NoClosedFace, report.Reason);
            Equal(3, report.InputSegmentCount);
            Equal(0, report.CandidateBoundaryCount);
            Equal(false, report.HasAcceptedBoundaries);
        }

        private static void ExplainsMinimumAreaRejection()
        {
            var analysis = new RoomBoundaryDiagnosticService().Analyze(Square(), minimumArea: 2d);
            var report = analysis.Report;
            Equal(RoomBoundaryDiagnosticReason.BelowMinimumArea, report.Reason);
            Equal(1, report.CandidateBoundaryCount);
            Equal(0, report.AcceptedBoundaryCount);
            Equal(1, report.RejectedByMinimumAreaCount);
            Equal(1d, report.MaxCandidateArea);
            Equal(false, report.Faces[0].Accepted);
            Equal(0, analysis.AcceptedBoundaries.Count);
        }

        private static void AcceptedFaceProvenanceIsDeterministicAndPrivacySafe()
        {
            var service = new RoomBoundaryDiagnosticService();
            var first = service.Analyze(Square(), minimumArea: 0.5d);
            var second = service.Analyze(Square().AsEnumerable().Reverse(), minimumArea: 0.5d);
            var firstReport = first.Report;
            var secondReport = second.Report;

            Equal(RoomBoundaryDiagnosticReason.Ready, firstReport.Reason);
            Equal(1, firstReport.AcceptedBoundaryCount);
            Equal(4, firstReport.UniqueSourceCount);
            Equal(4, firstReport.Faces[0].SourceCount);
            Equal(firstReport.Faces[0].FaceFingerprint, secondReport.Faces[0].FaceFingerprint);
            Equal(firstReport.Faces[0].SourceFingerprint, secondReport.Faces[0].SourceFingerprint);
            Equal(64, firstReport.Faces[0].FaceFingerprint.Length);
            Equal(64, firstReport.Faces[0].SourceFingerprint.Length);
            if (firstReport.Faces[0].SourceFingerprint.IndexOf("HANDLE", StringComparison.OrdinalIgnoreCase) >= 0)
                throw new Exception("Diagnostic fingerprint leaked a raw source id.");
            Equal(1, first.AcceptedBoundaries.Count);
            Equal(first.AcceptedBoundaries[0].Key, second.AcceptedBoundaries[0].Key);
        }

        private static BoundarySegment[] Square() => new[]
        {
            Segment(0, 0, 1, 0, "HANDLE-A"),
            Segment(1, 0, 1, 1, "HANDLE-B"),
            Segment(1, 1, 0, 1, "HANDLE-C"),
            Segment(0, 1, 0, 0, "HANDLE-D")
        };

        private static BoundarySegment Segment(double ax, double ay, double bx, double by, string sourceId)
            => new BoundarySegment(new Point2(ax, ay), new Point2(bx, by), sourceId);

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }
    }
}
