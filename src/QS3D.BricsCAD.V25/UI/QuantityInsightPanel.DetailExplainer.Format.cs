using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void SetQuantityDetailValue(string key, double value, string unit)
        {
            if (_quantityDetailValues.TryGetValue(key, out var text))
                text.Text = value.ToString("0.###", CultureInfo.CurrentCulture) + " " + unit;
        }

        private void SetQuantityDetailNullable(string key, double? value, string unit)
        {
            if (_quantityDetailValues.TryGetValue(key, out var text))
                text.Text = value.HasValue ? value.Value.ToString("0.###", CultureInfo.CurrentCulture) + " " + unit : "—";
        }

        private static string QuantityDetailJoin(IEnumerable<string> values)
        {
            var data = (values ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return data.Length == 0 ? "—" : string.Join(", ", data);
        }

        private static string QuantityDetailContext(QuantityReportRow row)
        {
            var data = new[] { row.Floor, row.Zone, row.Category, row.FamilyName, row.Material }
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.CurrentCultureIgnoreCase).ToArray();
            return data.Length == 0 ? "Chưa có metadata phân loại" : string.Join(" • ", data);
        }
    }
}
