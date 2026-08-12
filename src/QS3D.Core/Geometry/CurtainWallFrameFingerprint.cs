using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace QS3D.Core.Geometry
{
    public sealed class CurtainWallFrameFingerprintInput
    {
        public double LengthM { get; set; }
        public double HeightM { get; set; }
        public double BottomOffsetM { get; set; }
        public double MaxPanelWidthM { get; set; }
        public double MaxPanelHeightM { get; set; }
        public double PerimeterFrameWidthM { get; set; }
        public double MullionWidthM { get; set; }
        public double TransomWidthM { get; set; }
        public double FrameDepthM { get; set; }
    }

    public static class CurtainWallFrameFingerprint
    {
        public static string Compute(CurtainWallFrameFingerprintInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            Validate(input.LengthM, nameof(input.LengthM), true);
            Validate(input.HeightM, nameof(input.HeightM), true);
            Validate(input.BottomOffsetM, nameof(input.BottomOffsetM), false);
            Validate(input.MaxPanelWidthM, nameof(input.MaxPanelWidthM), true);
            Validate(input.MaxPanelHeightM, nameof(input.MaxPanelHeightM), true);
            Validate(input.PerimeterFrameWidthM, nameof(input.PerimeterFrameWidthM), false, true);
            Validate(input.MullionWidthM, nameof(input.MullionWidthM), false, true);
            Validate(input.TransomWidthM, nameof(input.TransomWidthM), false, true);
            Validate(input.FrameDepthM, nameof(input.FrameDepthM), true);

            var canonical = string.Join("|", new[]
            {
                "CURTAIN_FRAME_V1",
                R(input.LengthM), R(input.HeightM), R(input.BottomOffsetM),
                R(input.MaxPanelWidthM), R(input.MaxPanelHeightM),
                R(input.PerimeterFrameWidthM), R(input.MullionWidthM), R(input.TransomWidthM), R(input.FrameDepthM)
            });
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var text = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        private static string R(double value) => (value == 0d ? 0d : value).ToString("R", CultureInfo.InvariantCulture);

        private static void Validate(double value, string name, bool positive, bool nonNegative = false)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) throw new ArgumentOutOfRangeException(name, "Value must be finite.");
            if (positive && value <= 0d) throw new ArgumentOutOfRangeException(name, "Value must be > 0.");
            if (nonNegative && value < 0d) throw new ArgumentOutOfRangeException(name, "Value must be >= 0.");
        }
    }
}
