using System;
using System.Globalization;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class Point2InvariantFormattingSmoke
    {
        public static void Run()
        {
            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                var commaCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                commaCulture.NumberFormat.NumberDecimalSeparator = ",";
                CultureInfo.CurrentCulture = commaCulture;

                if (1.25d.ToString() != "1,25")
                    throw new Exception("Point2 invariant-format smoke did not install a comma-decimal ambient culture.");

                var actual = new Point2(1.25d, -2.5d).ToString();
                if (!string.Equals("(1.25, -2.5)", actual, StringComparison.Ordinal))
                    throw new Exception("Point2.ToString() must be culture-invariant. Actual: " + actual);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
