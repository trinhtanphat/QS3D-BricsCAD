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

            // Snapshot every caller-mutable scalar exactly once. Validation and hashing must
            // describe one logical input state rather than re-reading a mutable property bag.
            var lengthM = input.LengthM;
            var heightM = input.HeightM;
            var bottomOffsetM = input.BottomOffsetM;
            var maxPanelWidthM = input.MaxPanelWidthM;
            var maxPanelHeightM = input.MaxPanelHeightM;
            var perimeterFrameWidthM = input.PerimeterFrameWidthM;
            var mullionWidthM = input.MullionWidthM;
            var transomWidthM = input.TransomWidthM;
            var frameDepthM = input.FrameDepthM;

            Validate(lengthM, nameof(input.LengthM), true);
            Validate(heightM, nameof(input.HeightM), true);
            Validate(bottomOffsetM, nameof(input.BottomOffsetM), false);
            Validate(maxPanelWidthM, nameof(input.MaxPanelWidthM), true);
            Validate(maxPanelHeightM, nameof(input.MaxPanelHeightM), true);
            Validate(perimeterFrameWidthM, nameof(input.PerimeterFrameWidthM), false, true);
            Validate(mullionWidthM, nameof(input.MullionWidthM), false, true);
            Validate(transomWidthM, nameof(input.TransomWidthM), false, true);
            Validate(frameDepthM, nameof(input.FrameDepthM), true);
            ValidateRepresentableEnvelope(lengthM, heightM, bottomOffsetM);

            var canonical = string.Join("|", new[]
            {
                "CURTAIN_FRAME_V1",
                R(lengthM), R(heightM), R(bottomOffsetM),
                R(maxPanelWidthM), R(maxPanelHeightM),
                R(perimeterFrameWidthM), R(mullionWidthM), R(transomWidthM), R(frameDepthM)
            });
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var text = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes) text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
                return text.ToString();
            }
        }

        private static void ValidateRepresentableEnvelope(double lengthM, double heightM, double bottomOffsetM)
        {
            var top = bottomOffsetM + heightM;
            if (double.IsNaN(top) || double.IsInfinity(top))
                throw new OverflowException("Curtain frame fingerprint top elevation must remain finite.");
            if (!(top > bottomOffsetM))
                throw new OverflowException("Curtain frame fingerprint height is below the representable elevation resolution.");

            var grossArea = lengthM * heightM;
            if (double.IsNaN(grossArea) || double.IsInfinity(grossArea))
                throw new OverflowException("Curtain frame fingerprint gross area must remain finite.");
            if (grossArea == 0d && lengthM != 0d && heightM != 0d)
                throw new OverflowException("Curtain frame fingerprint gross area underflowed to zero.");
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