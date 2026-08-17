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
            for (var index = 0; index < canonicalHandle.Length; index++)
            {
                if (char.IsControl(canonicalHandle[index]))
                    throw new ArgumentException("Takeoff handle must not contain control characters.", nameof(handle));
            }

            Handle = canonicalHandle;
            Kind = kind;
            Value = value == 0d ? 0d : value;
            Unit = unit.Trim();
        }

        public string Handle { get; }
        public TakeoffKind Kind { get; }
        public double Value { get; }
        public string Unit { get; }
    }
}
