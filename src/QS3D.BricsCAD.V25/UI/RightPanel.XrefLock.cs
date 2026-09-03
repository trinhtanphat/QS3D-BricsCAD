using System;
using System.Windows;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class RightPanel
    {
        private const string XrefInstanceLockFailureStatus = "Không thể khóa các layer chứa instance của Xref. Trạng thái panel đã được làm mới lại.";
        private const string XrefInstanceUnlockFailureStatus = "Không thể mở khóa các layer chứa instance của Xref. Trạng thái panel đã được làm mới lại.";

        private void OnLockXrefClick(object sender, RoutedEventArgs e) => SetSelectedXrefInstanceLayerLocks(true);

        private void OnUnlockXrefClick(object sender, RoutedEventArgs e) => SetSelectedXrefInstanceLayerLocks(false);

        private void SetSelectedXrefInstanceLayerLocks(bool locked)
        {
            var document = Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
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
            catch (Exception)
            {
                var failureStatus = locked ? XrefInstanceLockFailureStatus : XrefInstanceUnlockFailureStatus;
                _viewModel.Status = failureStatus;
                try
                {
                    RefreshDrawingsOnly();
                    ReloadLayers();
                }
                catch (Exception)
                {
                    _viewModel.Status = failureStatus + RefreshWarningSuffix;
                }
            }
        }
    }
}
