using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace QS3D.Core.Mep
{
    public enum MepElementKind
    {
        Equipment = 0,
        Fixture = 1,
        Duct = 2,
        Pipe = 3,
        CableTray = 4,
        Conduit = 5,
        Cable = 6,
        Fitting = 7,
        Accessory = 8
    }

    public sealed class MepElement
    {
        public MepElement(
            string elementId,
            MepElementKind kind,
            string system,
            string specification,
            string region,
            int count = 1,
            double lengthM = 0d,
            double areaM2 = 0d,
            double volumeM3 = 0d)
        {
            ElementId = MepContract.RequireText(elementId, nameof(elementId));
            if (!Enum.IsDefined(typeof(MepElementKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            Kind = kind;
            System = MepContract.RequireText(system, nameof(system));
            Specification = MepContract.RequireText(specification, nameof(specification));
            Region = MepContract.RequireText(region, nameof(region));
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            Count = count;
            LengthM = MepContract.RequireNonNegativeFinite(lengthM, nameof(lengthM));
            AreaM2 = MepContract.RequireNonNegativeFinite(areaM2, nameof(areaM2));
            VolumeM3 = MepContract.RequireNonNegativeFinite(volumeM3, nameof(volumeM3));
        }

        public string ElementId { get; }
        public MepElementKind Kind { get; }
        public string System { get; }
        public string Specification { get; }
        public string Region { get; }
        public int Count { get; }
        public double LengthM { get; }
        public double AreaM2 { get; }
        public double VolumeM3 { get; }
    }

    public sealed class MepQuantityGroup
    {
        internal MepQuantityGroup(
            MepElementKind kind,
            string system,
            string specification,
            string region,
            int elementCount,
            int quantityCount,
            double lengthM,
            double areaM2,
            double volumeM3)
        {
            Kind = kind;
            System = system;
            Specification = specification;
            Region = region;
            ElementCount = elementCount;
            QuantityCount = quantityCount;
            LengthM = lengthM;
            AreaM2 = areaM2;
            VolumeM3 = volumeM3;
        }

        public MepElementKind Kind { get; }
        public string System { get; }
        public string Specification { get; }
        public string Region { get; }
        public int ElementCount { get; }
        public int QuantityCount { get; }
        public double LengthM { get; }
        public double AreaM2 { get; }
        public double VolumeM3 { get; }
    }

    public sealed class MepQuantityService
    {
        public IReadOnlyList<MepQuantityGroup> Aggregate(IEnumerable<MepElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var builders = new Dictionary<string, AggregateBuilder>(StringComparer.Ordinal);
            var index = 0;
            foreach (var element in elements)
            {
                if (element == null)
                    throw new ArgumentException("MEP takeoff contains a null element at index " + index + ".", nameof(elements));
                if (!ids.Add(element.ElementId))
                    throw new ArgumentException("Duplicate MEP element id: " + element.ElementId + ".", nameof(elements));

                var key = BuildKey(element);
                if (!builders.TryGetValue(key, out var builder))
                {
                    builder = new AggregateBuilder(element);
                    builders.Add(key, builder);
                }
                builder.Add(element);
                index++;
            }

            var result = new List<MepQuantityGroup>(builders.Count);
            foreach (var builder in builders.Values)
                result.Add(builder.Build());
            result.Sort(CompareGroups);
            return new ReadOnlyCollection<MepQuantityGroup>(result.ToArray());
        }

        private static string BuildKey(MepElement element) =>
            element.Region.ToUpperInvariant() + "\u001f" +
            element.System.ToUpperInvariant() + "\u001f" +
            element.Specification.ToUpperInvariant() + "\u001f" +
            ((int)element.Kind).ToString(System.Globalization.CultureInfo.InvariantCulture);

        private static int CompareGroups(MepQuantityGroup left, MepQuantityGroup right)
        {
            var compare = StringComparer.OrdinalIgnoreCase.Compare(left.Region, right.Region);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.System, right.System);
            if (compare != 0) return compare;
            compare = StringComparer.OrdinalIgnoreCase.Compare(left.Specification, right.Specification);
            if (compare != 0) return compare;
            return left.Kind.CompareTo(right.Kind);
        }

        private sealed class AggregateBuilder
        {
            private readonly MepElementKind _kind;
            private readonly string _system;
            private readonly string _specification;
            private readonly string _region;
            private int _elementCount;
            private int _quantityCount;
            private double _lengthM;
            private double _lengthCompensation;
            private double _areaM2;
            private double _areaCompensation;
            private double _volumeM3;
            private double _volumeCompensation;

            internal AggregateBuilder(MepElement seed)
            {
                _kind = seed.Kind;
                _system = seed.System;
                _specification = seed.Specification;
                _region = seed.Region;
            }

            internal void Add(MepElement element)
            {
                checked
                {
                    _elementCount++;
                    _quantityCount += element.Count;
                }
                _lengthM = MepContract.CheckedCompensatedAdd(
                    _lengthM,
                    ref _lengthCompensation,
                    element.LengthM,
                    "MEP aggregated length");
                _areaM2 = MepContract.CheckedCompensatedAdd(
                    _areaM2,
                    ref _areaCompensation,
                    element.AreaM2,
                    "MEP aggregated area");
                _volumeM3 = MepContract.CheckedCompensatedAdd(
                    _volumeM3,
                    ref _volumeCompensation,
                    element.VolumeM3,
                    "MEP aggregated volume");
            }

            internal MepQuantityGroup Build() => new MepQuantityGroup(
                _kind,
                _system,
                _specification,
                _region,
                _elementCount,
                _quantityCount,
                MepContract.CheckedCompensatedValue(_lengthM, _lengthCompensation, "MEP aggregated length"),
                MepContract.CheckedCompensatedValue(_areaM2, _areaCompensation, "MEP aggregated area"),
                MepContract.CheckedCompensatedValue(_volumeM3, _volumeCompensation, "MEP aggregated volume"));
        }
    }

    internal static class MepContract
    {
        internal static string RequireText(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("MEP identity/classification text is required.", parameterName);
            var trimmed = value.Trim();
            for (var i = 0; i < trimmed.Length; i++)
            {
                if (char.IsControl(trimmed[i]))
                    throw new ArgumentException("MEP identity/classification text must not contain control characters.", parameterName);
            }
            return trimmed;
        }

        internal static double RequireNonNegativeFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(parameterName, "MEP quantity values must be finite and non-negative.");
            return value == 0d ? 0d : value;
        }

        internal static double CheckedCompensatedAdd(
            double sum,
            ref double compensation,
            double value,
            string label)
        {
            var result = sum + value;
            var correction = Math.Abs(sum) >= Math.Abs(value)
                ? (sum - result) + value
                : (value - result) + sum;
            var nextCompensation = compensation + correction;
            if (double.IsNaN(result) ||
                double.IsInfinity(result) ||
                double.IsNaN(nextCompensation) ||
                double.IsInfinity(nextCompensation))
            {
                throw new OverflowException(label + " exceeded the representable numeric range.");
            }

            compensation = nextCompensation == 0d ? 0d : nextCompensation;
            return result == 0d ? 0d : result;
        }

        internal static double CheckedCompensatedValue(double sum, double compensation, string label)
        {
            var result = sum + compensation;
            if (double.IsNaN(result) || double.IsInfinity(result))
                throw new OverflowException(label + " exceeded the representable numeric range.");
            return result == 0d ? 0d : result;
        }
    }
}
