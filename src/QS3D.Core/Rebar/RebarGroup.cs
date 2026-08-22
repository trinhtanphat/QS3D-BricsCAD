using System;
using System.Globalization;

namespace QS3D.Core.Rebar
{
    public sealed class RebarGroup
    {
        public int? Quantity { get; set; }
        public int? Sets { get; set; }
        public int? BarsPerSet { get; set; }
        public double DiameterMm { get; set; }
        public double? SpacingMm { get; set; }
        public override string ToString()
        {
            var diameter = FormatPlainRoundTrip(DiameterMm);
            if (SpacingMm.HasValue) return "D" + diameter + "@" + FormatPlainRoundTrip(SpacingMm.Value);
            if (Sets.HasValue && BarsPerSet.HasValue) return Sets.Value.ToString(CultureInfo.InvariantCulture) + "x" + BarsPerSet.Value.ToString(CultureInfo.InvariantCulture) + "D" + diameter;
            if (Quantity.HasValue) return Quantity.Value.ToString(CultureInfo.InvariantCulture) + "D" + diameter;
            return "D" + diameter;
        }

        private static string FormatPlainRoundTrip(double value)
        {
            var roundTrip = value.ToString("R", CultureInfo.InvariantCulture);
            var exponentIndex = roundTrip.IndexOf('E');
            if (exponentIndex < 0) exponentIndex = roundTrip.IndexOf('e');
            if (exponentIndex < 0) return roundTrip;

            var exponent = int.Parse(roundTrip.Substring(exponentIndex + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
            var mantissa = roundTrip.Substring(0, exponentIndex);
            var negative = mantissa.StartsWith("-", StringComparison.Ordinal);
            if (negative) mantissa = mantissa.Substring(1);

            var decimalIndex = mantissa.IndexOf('.');
            var digitsBeforeDecimal = decimalIndex < 0 ? mantissa.Length : decimalIndex;
            var digits = decimalIndex < 0 ? mantissa : mantissa.Remove(decimalIndex, 1);
            var decimalPosition = digitsBeforeDecimal + exponent;

            string plain;
            if (decimalPosition <= 0)
                plain = "0." + new string('0', -decimalPosition) + digits;
            else if (decimalPosition >= digits.Length)
                plain = digits + new string('0', decimalPosition - digits.Length);
            else
                plain = digits.Insert(decimalPosition, ".");

            return negative ? "-" + plain : plain;
        }
    }
}
