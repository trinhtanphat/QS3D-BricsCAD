using System;

namespace QS3D.Core.Geometry
{
    /// <summary>
    /// Host-neutral dimensional contract for a centered single footing.
    /// All values are stored in meters. The lower stage is a rectangular prism
    /// L1 x W1 x H1; when H2 is positive the upper stage linearly tapers from
    /// L1 x W1 to L2 x W2 over H2.
    /// </summary>
    public sealed class SingleFootingDimensions
    {
        public SingleFootingDimensions(double l1M, double w1M, double l2M, double w2M, double h1M, double h2M)
        {
            L1M = RequirePositiveFinite(l1M, nameof(l1M));
            W1M = RequirePositiveFinite(w1M, nameof(w1M));
            L2M = RequirePositiveFinite(l2M, nameof(l2M));
            W2M = RequirePositiveFinite(w2M, nameof(w2M));
            H1M = RequirePositiveFinite(h1M, nameof(h1M));
            H2M = RequireNonNegativeFinite(h2M, nameof(h2M));

            if (L2M > L1M)
                throw new ArgumentOutOfRangeException(nameof(l2M), l2M, "Single footing top length L2 must not exceed base length L1.");
            if (W2M > W1M)
                throw new ArgumentOutOfRangeException(nameof(w2M), w2M, "Single footing top width W2 must not exceed base width W1.");
        }

        public double L1M { get; }
        public double W1M { get; }
        public double L2M { get; }
        public double W2M { get; }
        public double H1M { get; }
        public double H2M { get; }
        public double TotalHeightM => RequireFinite(H1M + H2M, "single footing total height");

        /// <summary>
        /// Exact volume for a centered rectangular prism plus a bilinearly shrinking
        /// rectangular loft. This remains correct when the L and W taper ratios differ.
        /// </summary>
        public double VolumeM3
        {
            get
            {
                var lower = RequireFinite(L1M * W1M * H1M, "single footing lower volume");
                if (H2M == 0d) return lower;

                var deltaL = L1M - L2M;
                var deltaW = W1M - W2M;
                var integratedArea =
                    L1M * W1M -
                    0.5d * (L1M * deltaW + W1M * deltaL) +
                    (deltaL * deltaW) / 3d;
                var upper = RequireFinite(H2M * integratedArea, "single footing tapered volume");
                return RequireFinite(lower + upper, "single footing volume");
            }
        }

        public bool HasTaper => H2M > 0d && (L2M < L1M || W2M < W1M);

        private static double RequirePositiveFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || !(value > 0d))
                throw new ArgumentOutOfRangeException(name, value, "Single footing dimension must be finite and greater than zero.");
            return value;
        }

        private static double RequireNonNegativeFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(name, value, "Single footing H2 must be finite and greater than or equal to zero.");
            return value;
        }

        private static double RequireFinite(double value, string label)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new OverflowException(label + " is not representable as a finite double.");
            return value;
        }
    }
}