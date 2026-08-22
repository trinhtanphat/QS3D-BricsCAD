using System.Collections.Generic;
namespace QS3D.Core.Reporting
{
    public sealed class QuantityReportRow
    {
        public QuantityReportRow() { ElementIds = new List<string>(); }
        public string Floor { get; set; } = string.Empty; public string Category { get; set; } = string.Empty; public string FamilyName { get; set; } = string.Empty; public int Count { get; set; } public double GrossConcreteM3 { get; set; } public double DeductionM3 { get; set; } public double NetConcreteM3 { get; set; } public double FormworkM2 { get; set; } public double LengthM { get; set; } public double OuterPerimeterM { get; set; } public double InnerPerimeterM { get; set; } public double DoorAreaM2 { get; set; } public double SideAreaM2 { get; set; } public double BottomAreaM2 { get; set; } public double TopAreaM2 { get; set; } public double OtherAreaM2 { get; set; } public double SteelWeightKg { get; set; } public IList<string> ElementIds { get; }
    }
}
