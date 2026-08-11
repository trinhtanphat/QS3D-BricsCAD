using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class GridSpatialEnumerationCapSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var yielded = 0;
            var line = GridReferenceCurve.Line("BOUND", new Point2(0d, 0d), new Point2(0d, 1d));

            IEnumerable<GridReferenceCurve> Source()
            {
                while (true)
                {
                    yielded++;
                    if (yielded > 2001) throw new Exception("GridSpatialOrderingPlanner enumerated beyond the declared curve cap probe.");
                    yield return line;
                }
            }

            try
            {
                GridSpatialOrderingPlanner.OrderParallelLines(Source(), new Point2(1d, 0d));
            }
            catch (InvalidOperationException)
            {
                if (yielded != 2001) throw new Exception("Expected exactly MaxCurves + 1 yielded curves before rejection.");
                return;
            }

            throw new Exception("Expected oversize Grid spatial ordering source rejection.");
        }
    }
}
