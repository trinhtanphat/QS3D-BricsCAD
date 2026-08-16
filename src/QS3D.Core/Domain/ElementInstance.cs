using System;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public sealed class ElementInstance
    {
        private string _floor;
        private double _lengthM;
        private double _areaM2;
        private double _volumeM3;
        private double _grossConcreteM3;
        private double _deductionM3;
        private double _formworkM2;
        private double _doorAreaM2;
        private double _outerPerimeterM;
        private double _innerPerimeterM;
        private double _sideAreaM2;
        private double _bottomAreaM2;
        private double _topAreaM2;
        private double _otherAreaM2;

        public ElementInstance(string id, FamilyDefinition family, string floor)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Element id is required.", nameof(id));
            Id = id.Trim();
            Family = family ?? throw new ArgumentNullException(nameof(family));
            _floor = NormalizeFloor(floor);
            SourceHandles = new List<string>();
        }

        public string Id { get; }
        public FamilyDefinition Family { get; }
        public string Floor
        {
            get => _floor;
            set => _floor = NormalizeFloor(value);
        }
        public IList<string> SourceHandles { get; }
        public double LengthM
        {
            get => _lengthM;
            set => _lengthM = RequireNonNegativeFinite(value, nameof(LengthM));
        }
        public double AreaM2
        {
            get => _areaM2;
            set => _areaM2 = RequireNonNegativeFinite(value, nameof(AreaM2));
        }
        public double VolumeM3
        {
            get => _volumeM3;
            set => _volumeM3 = RequireNonNegativeFinite(value, nameof(VolumeM3));
        }
        public double GrossConcreteM3
        {
            get => _grossConcreteM3;
            set => _grossConcreteM3 = RequireNonNegativeFinite(value, nameof(GrossConcreteM3));
        }
        public double DeductionM3
        {
            get => _deductionM3;
            set => _deductionM3 = RequireNonNegativeFinite(value, nameof(DeductionM3));
        }
        public double FormworkM2
        {
            get => _formworkM2;
            set => _formworkM2 = RequireNonNegativeFinite(value, nameof(FormworkM2));
        }
        public double DoorAreaM2
        {
            get => _doorAreaM2;
            set => _doorAreaM2 = RequireNonNegativeFinite(value, nameof(DoorAreaM2));
        }
        public double OuterPerimeterM
        {
            get => _outerPerimeterM;
            set => _outerPerimeterM = RequireNonNegativeFinite(value, nameof(OuterPerimeterM));
        }
        public double InnerPerimeterM
        {
            get => _innerPerimeterM;
            set => _innerPerimeterM = RequireNonNegativeFinite(value, nameof(InnerPerimeterM));
        }
        public double SideAreaM2
        {
            get => _sideAreaM2;
            set => _sideAreaM2 = RequireNonNegativeFinite(value, nameof(SideAreaM2));
        }
        public double BottomAreaM2
        {
            get => _bottomAreaM2;
            set => _bottomAreaM2 = RequireNonNegativeFinite(value, nameof(BottomAreaM2));
        }
        public double TopAreaM2
        {
            get => _topAreaM2;
            set => _topAreaM2 = RequireNonNegativeFinite(value, nameof(TopAreaM2));
        }
        public double OtherAreaM2
        {
            get => _otherAreaM2;
            set => _otherAreaM2 = RequireNonNegativeFinite(value, nameof(OtherAreaM2));
        }
        public double NetConcreteM3
        {
            get
            {
                var gross = GrossConcreteM3;
                var deduction = DeductionM3;
                var value = gross - deduction;
                if (double.IsNaN(value) || double.IsInfinity(value))
                    throw new OverflowException("Net concrete volume must be finite.");
                if (deduction > 0d && value == gross)
                    throw new OverflowException("Net concrete deduction is below numeric resolution and cannot be represented safely.");
                return Math.Max(0d, value);
            }
        }

        private static string NormalizeFloor(string value) =>
            string.IsNullOrWhiteSpace(value) ? "Nền 0.00" : value.Trim();

        private static double RequireNonNegativeFinite(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new ArgumentOutOfRangeException(parameterName, "Element measurement must be finite and non-negative.");
            return value == 0d ? 0d : value;
        }
    }
}
