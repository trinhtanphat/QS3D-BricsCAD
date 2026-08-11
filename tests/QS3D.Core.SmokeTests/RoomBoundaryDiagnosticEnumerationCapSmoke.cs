using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomBoundaryDiagnosticEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var segment = new BoundarySegment(new Point2(0d, 0d), new Point2(1d, 0d), "BOUND");

            IEnumerable<BoundarySegment> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 5001) throw new Exception("RoomBoundaryDiagnosticService enumerated beyond the Room boundary segment cap probe.");
                    yield return segment;
                }
            }

            try
            {
                new RoomBoundaryDiagnosticService().Analyze(Source());
            }
            catch (InvalidOperationException)
            {
                if (yielded != 5001) throw new Exception("Expected exactly Room boundary MaxInputSegments + 1 yielded segments before rejection.");
                return;
            }

            throw new Exception("Expected oversize Room boundary diagnostic source rejection.");
        }
    }
}
