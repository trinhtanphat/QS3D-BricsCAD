using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public static class QuantityReportBuilder
    {
        private sealed class QuantityAccumulatorSet
        {
            private QuantityReportMath.FiniteAccumulator _grossConcreteM3;
            private QuantityReportMath.FiniteAccumulator _deductionM3;
            private QuantityReportMath.FiniteAccumulator _netConcreteM3;
            private QuantityReportMath.FiniteAccumulator _formworkM2;
            private QuantityReportMath.FiniteAccumulator _lengthM;
            private QuantityReportMath.FiniteAccumulator _outerPerimeterM;
            private QuantityReportMath.FiniteAccumulator _innerPerimeterM;
            private QuantityReportMath.FiniteAccumulator _doorAreaM2;
            private QuantityReportMath.FiniteAccumulator _sideAreaM2;
            private QuantityReportMath.FiniteAccumulator _bottomAreaM2;
            private QuantityReportMath.FiniteAccumulator _topAreaM2;
            private QuantityReportMath.FiniteAccumulator _otherAreaM2;

            public void Add(ElementInstance element)
            {
                var id = element.Id;
                _grossConcreteM3.Add(NonNegative(element.GrossConcreteM3, id, "GrossConcreteM3"), id + "/GrossConcreteM3");
                _deductionM3.Add(NonNegative(element.DeductionM3, id, "DeductionM3"), id + "/DeductionM3");
                _netConcreteM3.Add(NonNegative(element.NetConcreteM3, id, "NetConcreteM3"), id + "/NetConcreteM3");
                _formworkM2.Add(NonNegative(element.FormworkM2, id, "FormworkM2"), id + "/FormworkM2");
                _lengthM.Add(NonNegative(element.LengthM, id, "LengthM"), id + "/LengthM");
                _outerPerimeterM.Add(NonNegative(element.OuterPerimeterM, id, "OuterPerimeterM"), id + "/OuterPerimeterM");
                _innerPerimeterM.Add(NonNegative(element.InnerPerimeterM, id, "InnerPerimeterM"), id + "/InnerPerimeterM");
                _doorAreaM2.Add(NonNegative(element.DoorAreaM2, id, "DoorAreaM2"), id + "/DoorAreaM2");
                _sideAreaM2.Add(NonNegative(element.SideAreaM2, id, "SideAreaM2"), id + "/SideAreaM2");
                _bottomAreaM2.Add(NonNegative(element.BottomAreaM2, id, "BottomAreaM2"), id + "/BottomAreaM2");
                _topAreaM2.Add(NonNegative(element.TopAreaM2, id, "TopAreaM2"), id + "/TopAreaM2");
                _otherAreaM2.Add(NonNegative(element.OtherAreaM2, id, "OtherAreaM2"), id + "/OtherAreaM2");
            }

            public void Apply(QuantityReportRow row)
            {
                row.GrossConcreteM3 = _grossConcreteM3.Value("GrossConcreteM3");
                row.DeductionM3 = _deductionM3.Value("DeductionM3");
                row.NetConcreteM3 = _netConcreteM3.Value("NetConcreteM3");
                row.FormworkM2 = _formworkM2.Value("FormworkM2");
                row.LengthM = _lengthM.Value("LengthM");
                row.OuterPerimeterM = _outerPerimeterM.Value("OuterPerimeterM");
                row.InnerPerimeterM = _innerPerimeterM.Value("InnerPerimeterM");
                row.DoorAreaM2 = _doorAreaM2.Value("DoorAreaM2");
                row.SideAreaM2 = _sideAreaM2.Value("SideAreaM2");
                row.BottomAreaM2 = _bottomAreaM2.Value("BottomAreaM2");
                row.TopAreaM2 = _topAreaM2.Value("TopAreaM2");
                row.OtherAreaM2 = _otherAreaM2.Value("OtherAreaM2");
            }
        }

        public static IReadOnlyList<QuantityReportRow> Group(IEnumerable<ElementInstance> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var order = new List<string>();
            var grouped = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var accumulators = new Dictionary<string, QuantityAccumulatorSet>(StringComparer.OrdinalIgnoreCase);
            var seenElementIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var elementIndex = 0;
            foreach (var element in elements)
            {
                if (element == null)
                    throw new ArgumentException("Quantity report elements cannot contain null entries. Invalid element index: " + elementIndex + ".", nameof(elements));
                if (!seenElementIds.Add(element.Id))
                    throw new InvalidOperationException("Quantity report contains duplicate element id: " + element.Id + ".");
                var material = NormalizeMaterial(element.Family.Material);
                var key = GroupKey(element.Floor, element.Family.Category.ToString(), element.Family.Name, material);
                if (!grouped.TryGetValue(key, out var row))
                {
                    row = new QuantityReportRow
                    {
                        Floor = element.Floor,
                        Category = element.Family.Category.ToString(),
                        FamilyName = element.Family.Name,
                        Material = material
                    };
                    grouped.Add(key, row);
                    accumulators.Add(key, new QuantityAccumulatorSet());
                    order.Add(key);
                }
                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                accumulators[key].Add(element);
                elementIndex++;
            }
            var result = new List<QuantityReportRow>(order.Count);
            foreach (var key in order)
            {
                var row = grouped[key];
                accumulators[key].Apply(row);
                result.Add(row);
            }
            return result.AsReadOnly();
        }

        private static string GroupKey(params string[] tokens)
        {
            var key = new StringBuilder();
            foreach (var raw in tokens)
            {
                var token = raw ?? string.Empty;
                key.Append(token.Length.ToString(CultureInfo.InvariantCulture))
                    .Append(':')
                    .Append(token);
            }
            return key.ToString();
        }

        private static string NormalizeMaterial(string material) =>
            string.IsNullOrWhiteSpace(material) ? "Khác" : material.Trim();

        private static double NonNegative(double value, string elementId, string quantity) =>
            QuantityReportMath.NonNegative(value, elementId + "/" + quantity);
    }
}
