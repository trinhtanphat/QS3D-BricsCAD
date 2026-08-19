using System.Collections.Generic;
using System.Linq;

namespace QS3D.Core.Reporting
{
    public sealed class QuantityReportRow
    {
        public QuantityReportRow() { ElementIds = new List<string>(); SourceHandles = new List<string>(); }
        public string Floor { get; set; } = string.Empty;
        public string Zone { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string FamilyId { get; set; } = string.Empty;
        public string FamilyName { get; set; } = string.Empty;
        public string ElementName { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string DrawingFingerprint { get; set; } = string.Empty;
        public int Count { get; set; }
        public double GrossConcreteM3 { get; set; }
        public double DeductionM3 { get; set; }
        public double NetConcreteM3 { get; set; }
        public double FormworkM2 { get; set; }
        public double LengthM { get; set; }
        public double OuterPerimeterM { get; set; }
        public double InnerPerimeterM { get; set; }
        public double DoorAreaM2 { get; set; }
        public double SideAreaM2 { get; set; }
        public double BottomAreaM2 { get; set; }
        public double TopAreaM2 { get; set; }
        public double OtherAreaM2 { get; set; }
        public bool HasGrossConcreteM3Evidence { get; set; } = true;
        public bool HasDeductionM3Evidence { get; set; } = true;
        public bool HasNetConcreteM3Evidence { get; set; } = true;
        public bool HasFormworkM2Evidence { get; set; } = true;
        public bool HasLengthMEvidence { get; set; } = true;
        public bool HasOuterPerimeterMEvidence { get; set; } = true;
        public bool HasInnerPerimeterMEvidence { get; set; } = true;
        public bool HasDoorAreaM2Evidence { get; set; } = true;
        public bool HasSideAreaM2Evidence { get; set; } = true;
        public bool HasBottomAreaM2Evidence { get; set; } = true;
        public bool HasTopAreaM2Evidence { get; set; } = true;
        public bool HasOtherAreaM2Evidence { get; set; } = true;
        public double? DensityKgM3 { get; set; }
        public double? MassKg { get; set; }
        public IList<string> ElementIds { get; }
        public IList<string> SourceHandles { get; }
        public string FloorZoneText => string.IsNullOrWhiteSpace(Floor)
            ? Zone
            : string.IsNullOrWhiteSpace(Zone) ? Floor : Floor + " / " + Zone;
        public string ElementIdText => string.Join(";", ElementIds.Where(x => !string.IsNullOrWhiteSpace(x)));
        public string SourceHandleText => string.Join(";", SourceHandles.Where(x => !string.IsNullOrWhiteSpace(x)));
    }
}
