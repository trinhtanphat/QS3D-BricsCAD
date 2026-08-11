using System;
using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public sealed class ElementInstance
    {
        private string _floor;

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
        public double LengthM { get; set; }
        public double AreaM2 { get; set; }
        public double VolumeM3 { get; set; }
        public double GrossConcreteM3 { get; set; }
        public double DeductionM3 { get; set; }
        public double FormworkM2 { get; set; }
        public double DoorAreaM2 { get; set; }
        public double OuterPerimeterM { get; set; }
        public double InnerPerimeterM { get; set; }
        public double SideAreaM2 { get; set; }
        public double BottomAreaM2 { get; set; }
        public double TopAreaM2 { get; set; }
        public double OtherAreaM2 { get; set; }
        public double NetConcreteM3 => GrossConcreteM3 - DeductionM3;

        private static string NormalizeFloor(string value) =>
            string.IsNullOrWhiteSpace(value) ? "Nền 0.00" : value.Trim();
    }
}
