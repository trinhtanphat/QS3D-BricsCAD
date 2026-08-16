using System;

namespace QS3D.Core.Geometry
{
    /// <summary>
    /// Plans the numeric BIM Detail volume used by the BricsCAD XEM "Cắt theo đối tượng" command.
    /// The planner is host-independent so binary64 precision behavior can be regression-tested
    /// without fabricating a licensed BricsCAD runtime result.
    /// </summary>
    public readonly struct SectionDetailVolumePlan
    {
        public SectionDetailVolumePlan(double firstX, double firstY, double baseZ, double oppositeX, double oppositeY, double height)
        {
            FirstX = firstX;
            FirstY = firstY;
            BaseZ = baseZ;
            OppositeX = oppositeX;
            OppositeY = oppositeY;
            Height = height;
        }

        public double FirstX { get; }
        public double FirstY { get; }
        public double BaseZ { get; }
        public double OppositeX { get; }
        public double OppositeY { get; }
        public double Height { get; }
    }

    public static class SectionDetailVolumePlanner
    {
        public static bool TryCreate(
            double minX,
            double minY,
            double minZ,
            double maxX,
            double maxY,
            double maxZ,
            double minimumSpan,
            out SectionDetailVolumePlan plan)
        {
            plan = default;
            if (!Finite(minX) || !Finite(minY) || !Finite(minZ) ||
                !Finite(maxX) || !Finite(maxY) || !Finite(maxZ) ||
                !Finite(minimumSpan) || minimumSpan < 0d)
                return false;
            if (maxX < minX || maxY < minY || maxZ < minZ)
                return false;

            var spanX = maxX - minX;
            var spanY = maxY - minY;
            var spanZ = maxZ - minZ;
            if (!Finite(spanX) || !Finite(spanY) || !Finite(spanZ) ||
                spanX < 0d || spanY < 0d || spanZ < 0d)
                return false;

            var longest = Math.Max(Math.Max(spanX, spanY), spanZ);
            if (!Finite(longest) || !(longest > minimumSpan))
                return false;

            var horizontalSpan = Math.Max(spanX, spanY);
            var horizontalFivePercent = horizontalSpan * 0.05d;
            var longestOnePercent = longest * 0.01d;
            var verticalFivePercent = spanZ * 0.05d;
            if (!Finite(horizontalFivePercent) || !Finite(longestOnePercent) || !Finite(verticalFivePercent))
                return false;

            var horizontalPadding = Math.Max(horizontalFivePercent, longestOnePercent);
            var verticalPadding = Math.Max(verticalFivePercent, longestOnePercent);
            if (!Finite(horizontalPadding) || !Finite(verticalPadding) ||
                !(horizontalPadding > 0d) || !(verticalPadding > 0d))
                return false;

            var firstX = minX - horizontalPadding;
            var firstY = minY - horizontalPadding;
            var baseZ = minZ - verticalPadding;
            var oppositeX = maxX + horizontalPadding;
            var oppositeY = maxY + horizontalPadding;
            var paddedTopZ = maxZ + verticalPadding;
            if (!Finite(firstX) || !Finite(firstY) || !Finite(baseZ) ||
                !Finite(oppositeX) || !Finite(oppositeY) || !Finite(paddedTopZ))
                return false;

            // A positive requested padding is only valid when binary64 can represent the
            // corresponding coordinate expansion. Otherwise fail closed instead of reporting
            // a padded volume while actually sending the original bound to the native command.
            if (!(firstX < minX) || !(firstY < minY) || !(baseZ < minZ) ||
                !(oppositeX > maxX) || !(oppositeY > maxY) || !(paddedTopZ > maxZ))
                return false;

            var paddedHeight = paddedTopZ - baseZ;
            var minimumHeight = longest * 0.02d;
            if (!Finite(paddedHeight) || !Finite(minimumHeight) ||
                !(paddedHeight > spanZ) || !(paddedHeight > 0d) || !(minimumHeight > 0d))
                return false;

            var height = Math.Max(paddedHeight, minimumHeight);
            var representedTopZ = baseZ + height;
            if (!Finite(height) || !Finite(representedTopZ) ||
                !(height > spanZ) || !(representedTopZ > maxZ))
                return false;

            plan = new SectionDetailVolumePlan(firstX, firstY, baseZ, oppositeX, oppositeY, height);
            return true;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }
}
