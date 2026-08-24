using System.Windows;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class QuantityInsightPanel
    {
        private void OnExcelTemplateExportClick(object sender, RoutedEventArgs e)
        {
            DispatchExistingCommand(
                "QS3DEXCELTEMPLATE ",
                "Xuất theo mẫu Excel: chọn scope, Detail/Group, template XLSX, mapping Default/Custom và output; QS3D dùng quantity/provenance canonical hiện hành."
            );
        }
    }
}
