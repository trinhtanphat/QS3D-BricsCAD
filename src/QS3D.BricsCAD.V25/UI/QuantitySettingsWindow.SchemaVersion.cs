using System.Globalization;
using QS3D.Core.Reporting;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantitySettingsWindow
    {
        public string SchemaVersionLabel =>
            "Schema v" + QuantityCalculationSettings.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture);
    }
}
