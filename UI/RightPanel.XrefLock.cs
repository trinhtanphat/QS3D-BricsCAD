using System;
using System.Windows;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel
    {
        private void OnLockXrefClick(object sender, RoutedEventArgs e) => SetSelectedXrefInstanceLayerLocks(true);

        private void OnUnlockXrefClick(object sender, RoutedEventArgs e) => SetSelectedXrefInstanceLayerLocks(false);

        private void SetSelectedXrefInstanceLayerLocks(bool locked)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            var item = SelectedXref();
            if (document == null || item == null) return;

            try
            {
                var affected = XrefService.SetInstanceLayersLocked(document, item.Name, locked);
                var status = affected == 0
                    ? "Xref " + item.Name + " chưa có instance trong space hiện tại; không đổi layer nào."
                    : (locked ? "Đã khóa " : "Đã mở khóa ") + affected + " layer chứa instance của Xref " + item.Name + ".";
                RefreshAfterXrefMutation(status);
            }
            catch (Exception ex)
            {
                _viewModel.Status = (locked ? "Không thể khóa Xref: " : "Không thể mở khóa Xref: ") + ex.Message;
                try
                {
                    RefreshDrawingsOnly();
                    ReloadLayers();
                }
                catch { }
            }
        }
    }
}
