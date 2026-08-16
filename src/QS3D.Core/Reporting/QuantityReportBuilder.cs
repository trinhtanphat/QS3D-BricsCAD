using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QS3D.Core.Domain;

namespace QS3D.Core.Reporting
{
    public static class QuantityReportBuilder
    {
        private struct CompensatedSum
        {
            private double _sum;
            private double _compensation;

            public double Add(double value, string label)
            {
                QuantityReportMath.Finite(value, label);
                var corrected = value - _compensation;
                var next = _sum + corrected;
                if (double.IsNaN(next) || double.IsInfinity(next))
                    throw new OverflowException("Quantity report total overflow: " + label);

                var compensation = (next - _sum) - corrected;
                if (double.IsNaN(compensation) || double.IsInfinity(compensation))
                    throw new OverflowException("Quantity report total compensation overflow: " + label);

                _sum = next;
                _compensation = compensation;
                return _sum == 0d ? 0d : _sum;
            }
        }

        private sealed class GroupAccumulationState
        {
            private CompensatedSum _grossConcreteM3;
            private CompensatedSum _deductionM3;
            private CompensatedSum _netConcreteM3;
            private CompensatedSum _formworkM2;
            private CompensatedSum _lengthM;
            private CompensatedSum _outerPerimeterM;
            private CompensatedSum _innerPerimeterM;
            private CompensatedSum _doorAreaM2;
            private CompensatedSum _sideAreaM2;
            private CompensatedSum _bottomAreaM2;
            private CompensatedSum _topAreaM2;
            private CompensatedSum _otherAreaM2;

            public void Add(QuantityReportRow row, ElementInstance element)
            {
                row.GrossConcreteM3 = _grossConcreteM3.Add(
                    NonNegative(element.GrossConcreteM3, element.Id, "GrossConcreteM3"),
                    element.Id + "/GrossConcreteM3");
                row.DeductionM3 = _deductionM3.Add(
                    NonNegative(element.DeductionM3, element.Id, "DeductionM3"),
                    element.Id + "/DeductionM3");
                row.NetConcreteM3 = _netConcreteM3.Add(
                    NonNegative(element.NetConcreteM3, element.Id, "NetConcreteM3"),
                    element.Id + "/NetConcreteM3");
                row.FormworkM2 = _formworkM2.Add(
                    NonNegative(element.FormworkM2, element.Id, "FormworkM2"),
                    element.Id + "/FormworkM2");
                row.LengthM = _lengthM.Add(
                    NonNegative(element.LengthM, element.Id, "LengthM"),
                    element.Id + "/LengthM");
                row.OuterPerimeterM = _outerPerimeterM.Add(
                    NonNegative(element.OuterPerimeterM, element.Id, "OuterPerimeterM"),
                    element.Id + "/OuterPerimeterM");
                row.InnerPerimeterM = _innerPerimeterM.Add(
                    NonNegative(element.InnerPerimeterM, element.Id, "InnerPerimeterM"),
                    element.Id + "/InnerPerimeterM");
                row.DoorAreaM2 = _doorAreaM2.Add(
                    NonNegative(element.DoorAreaM2, element.Id, "DoorAreaM2"),
                    element.Id + "/DoorAreaM2");
                row.SideAreaM2 = _sideAreaM2.Add(
                    NonNegative(element.SideAreaM2, element.Id, "SideAreaM2"),
                    element.Id + "/SideAreaM2");
                row.BottomAreaM2 = _bottomAreaM2.Add(
                    NonNegative(element.BottomAreaM2, element.Id, "BottomAreaM2"),
                    element.Id + "/BottomAreaM2");
                row.TopAreaM2 = _topAreaM2.Add(
                    NonNegative(element.TopAreaM2, element.Id, "TopAreaM2"),
                    element.Id + "/TopAreaM2");
                row.OtherAreaM2 = _otherAreaM2.Add(
                    NonNegative(element.OtherAreaM2, element.Id, "OtherAreaM2"),
                    element.Id + "/OtherAreaM2");
            }
        }

        public static IReadOnlyList<QuantityReportRow> Group(IEnumerable<ElementInstance> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));
            var order = new List<string>();
            var grouped = new Dictionary<string, QuantityReportRow>(StringComparer.OrdinalIgnoreCase);
            var accumulation = new Dictionary<string, GroupAccumulationState>(StringComparer.OrdinalIgnoreCase);
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
                    accumulation.Add(key, new GroupAccumulationState());
                    order.Add(key);
                }
                row.Count = QuantityReportMath.AddCount(row.Count, 1);
                row.ElementIds.Add(element.Id);
                ReportingRowProvenance.AppendSourceHandles(row.SourceHandles, element.SourceHandles);
                accumulation[key].Add(row, element);
                elementIndex++;
            }
            var result = new List<QuantityReportRow>(order.Count);
            foreach (var key in order) result.Add(grouped[key]);
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
