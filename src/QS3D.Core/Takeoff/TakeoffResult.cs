using System;

namespace QS3D.Core.Takeoff
{
    public sealed class TakeoffResult
    {
        public TakeoffResult(string handle, TakeoffKind kind, double value, string unit)
        {
            if (string.IsNullOrWhiteSpace(handle)) throw new ArgumentException("Takeoff handle is required.", nameof(handle));
            if (!Enum.IsDefined(typeof(TakeoffKind), kind)) throw new ArgumentOutOfRangeException(nameof(kind));
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(nameof(value), "Takeoff value must be finite and non-negative.");
            if (string.IsNullOrWhiteSpace(unit)) throw new ArgumentException("Takeoff unit is required.", nameof(unit));

            var canonicalHandle = handle.Trim();
            if (!string.Equals(handle, canonicalHandle, StringComparison.Ordinal))
                throw new ArgumentException("Takeoff handle must not contain surrounding whitespace.", nameof(handle));
            EnsureValidUnicodeScalarText(canonicalHandle, nameof(handle), "Takeoff handle");

            for (var index = 0; index < canonicalHandle.Length; index++)
            {
                if (char.IsControl(canonicalHandle[index]))
                    throw new ArgumentException("Takeoff handle must not contain control characters.", nameof(handle));
                if (char.IsWhiteSpace(canonicalHandle[index]))
                    throw new ArgumentException("Takeoff handle must not contain whitespace.", nameof(handle));
            }

            var canonicalUnit = unit.Trim();
            if (!string.Equals(unit, canonicalUnit, StringComparison.Ordinal))
                throw new ArgumentException("Takeoff unit must not contain surrounding whitespace.", nameof(unit));
            EnsureValidUnicodeScalarText(canonicalUnit, nameof(unit), "Takeoff unit");

            for (var index = 0; index < canonicalUnit.Length; index++)
            {
                if (char.IsWhiteSpace(canonicalUnit[index]) || char.IsControl(canonicalUnit[index]))
                    throw new ArgumentException("Takeoff unit must not contain whitespace or control characters.", nameof(unit));
            }

            if (!string.Equals(canonicalUnit, canonicalUnit.ToLowerInvariant(), StringComparison.Ordinal))
                throw new ArgumentException("Takeoff unit must use canonical lower-case text.", nameof(unit));

            Handle = canonicalHandle;
            Kind = kind;
            Value = value == 0d ? 0d : value;
            Unit = canonicalUnit;
        }

        public string Handle { get; }
        public TakeoffKind Kind { get; }
        public double Value { get; }
        public string Unit { get; }

        private static void EnsureValidUnicodeScalarText(string value, string parameterName, string label)
        {
            for (var index = 0; index < value.Length; index++)
            {
                var current = value[index];
                if (!char.IsSurrogate(current))
                    continue;

                if (char.IsHighSurrogate(current) &&
                    index + 1 < value.Length &&
                    char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                    continue;
                }

                throw new ArgumentException(label + " must contain valid Unicode scalar text.", parameterName);
            }
        }
    }
}
