using System;

namespace QS3D.Core.Features
{
    public sealed class InspectorHostLayout
    {
        internal InspectorHostLayout(
            bool primaryVisible,
            bool secondaryVisible,
            double primaryWidth,
            double secondaryWidth,
            double separatorWidth,
            double minimumCenterWidth)
        {
            PrimaryVisible = primaryVisible;
            SecondaryVisible = secondaryVisible;
            PrimaryWidth = primaryWidth;
            SecondaryWidth = secondaryWidth;
            SeparatorWidth = separatorWidth;
            MinimumCenterWidth = minimumCenterWidth;
        }

        public bool PrimaryVisible { get; }
        public bool SecondaryVisible { get; }
        public double PrimaryWidth { get; }
        public double SecondaryWidth { get; }
        public double SeparatorWidth { get; }
        public double MinimumCenterWidth { get; }
        public int VisibleInspectorCount => (PrimaryVisible ? 1 : 0) + (SecondaryVisible ? 1 : 0);
        public double ReservedInspectorWidth =>
            (PrimaryVisible ? PrimaryWidth + SeparatorWidth : 0d)
            + (SecondaryVisible ? SecondaryWidth + SeparatorWidth : 0d);
    }

    public static class InspectorHostLayoutPlanner
    {
        public const double DefaultPrimaryWidth = 280d;
        public const double DefaultSecondaryWidth = 240d;
        public const double MinimumInspectorWidth = 200d;
        public const double MaximumInspectorWidth = 420d;
        public const double SeparatorWidth = 4d;
        public const double MinimumCenterWidth = 320d;

        public static InspectorHostLayout Plan(
            InteractionSurfaceSnapshot snapshot,
            double availableWidth,
            double preferredPrimaryWidth = DefaultPrimaryWidth,
            double preferredSecondaryWidth = DefaultSecondaryWidth)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (double.IsNaN(availableWidth) || double.IsInfinity(availableWidth) || availableWidth < 0d)
                throw new ArgumentOutOfRangeException(nameof(availableWidth));

            var primaryVisible = snapshot.PrimaryInspector != null;
            var secondaryVisible = snapshot.SecondaryInspector != null;
            var primaryWidth = primaryVisible ? Clamp(preferredPrimaryWidth) : 0d;
            var secondaryWidth = secondaryVisible ? Clamp(preferredSecondaryWidth) : 0d;

            if (primaryVisible && secondaryVisible)
            {
                var availableForInspectors = Math.Max(0d, availableWidth - MinimumCenterWidth - (SeparatorWidth * 2d));
                if (availableForInspectors < MinimumInspectorWidth * 2d)
                {
                    secondaryVisible = false;
                    secondaryWidth = 0d;
                }
                else if (primaryWidth + secondaryWidth > availableForInspectors)
                {
                    var shared = availableForInspectors / 2d;
                    primaryWidth = Math.Max(MinimumInspectorWidth, Math.Min(primaryWidth, shared));
                    secondaryWidth = Math.Max(MinimumInspectorWidth, Math.Min(secondaryWidth, availableForInspectors - primaryWidth));
                }
            }

            if (primaryVisible && !secondaryVisible)
            {
                var availableForPrimary = Math.Max(0d, availableWidth - MinimumCenterWidth - SeparatorWidth);
                if (availableForPrimary < MinimumInspectorWidth)
                {
                    primaryVisible = false;
                    primaryWidth = 0d;
                }
                else
                {
                    primaryWidth = Math.Min(primaryWidth, availableForPrimary);
                }
            }

            if (!primaryVisible && secondaryVisible)
            {
                var availableForSecondary = Math.Max(0d, availableWidth - MinimumCenterWidth - SeparatorWidth);
                if (availableForSecondary < MinimumInspectorWidth)
                {
                    secondaryVisible = false;
                    secondaryWidth = 0d;
                }
                else
                {
                    secondaryWidth = Math.Min(secondaryWidth, availableForSecondary);
                }
            }

            return new InspectorHostLayout(
                primaryVisible,
                secondaryVisible,
                primaryWidth,
                secondaryWidth,
                SeparatorWidth,
                MinimumCenterWidth);
        }

        private static double Clamp(double width)
        {
            if (double.IsNaN(width) || double.IsInfinity(width)) return MinimumInspectorWidth;
            return Math.Max(MinimumInspectorWidth, Math.Min(MaximumInspectorWidth, width));
        }
    }
}
