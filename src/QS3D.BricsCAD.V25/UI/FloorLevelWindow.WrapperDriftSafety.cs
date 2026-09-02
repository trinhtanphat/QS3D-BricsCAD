using System;
using System.Windows.Input;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.UI
{
    public partial class FloorLevelWindow
    {
        private const string WrapperDriftMessage =
            "Level Picker host document wrapper đã thay đổi hoặc không còn live. Cửa sổ đã đóng an toàn; hãy mở lại QS3DLEVELS trong bản vẽ hiện hành.";

        private IntPtr _wrapperDriftNativeDatabaseIdentity;
        private bool _wrapperDriftNativeIdentityCaptured;
        private bool _wrapperDriftCloseRequested;

        protected override void OnInitialized(EventArgs e)
        {
            CaptureWrapperDriftNativeIdentity();
            base.OnInitialized(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            if (!EnsureManagedWrapperAffinity()) return;
            base.OnContentRendered(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            if (!EnsureManagedWrapperAffinity()) return;
            base.OnActivated(e);
        }

        protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
        {
            if (!EnsureManagedWrapperAffinity())
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewMouseDown(e);
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (!EnsureManagedWrapperAffinity())
            {
                e.Handled = true;
                return;
            }

            base.OnPreviewKeyDown(e);
        }

        private void CaptureWrapperDriftNativeIdentity()
        {
            if (_wrapperDriftNativeIdentityCaptured) return;

            var database = _document.Database;
            if (database == null || database.UnmanagedObject == IntPtr.Zero)
                throw new InvalidOperationException("Level Picker requires a live native BricsCAD database.");

            _wrapperDriftNativeDatabaseIdentity = database.UnmanagedObject;
            _wrapperDriftNativeIdentityCaptured = true;
        }

        private bool EnsureManagedWrapperAffinity()
        {
            if (_wrapperDriftCloseRequested) return false;
            if (!_wrapperDriftNativeIdentityCaptured) return CloseForManagedWrapperDrift();

            Document? liveDocument = null;
            try
            {
                foreach (Document candidate in Application.DocumentManager)
                {
                    if (candidate == null || candidate.IsDisposed) continue;
                    try
                    {
                        var database = candidate.Database;
                        if (database != null &&
                            database.UnmanagedObject != IntPtr.Zero &&
                            database.UnmanagedObject == _wrapperDriftNativeDatabaseIdentity)
                        {
                            liveDocument = candidate;
                            break;
                        }
                    }
                    catch
                    {
                        // One unsafe managed wrapper cannot prove native ownership; keep looking.
                    }
                }
            }
            catch
            {
                liveDocument = null;
            }

            if (liveDocument != null && ReferenceEquals(liveDocument, _document))
                return true;

            return CloseForManagedWrapperDrift();
        }

        private bool CloseForManagedWrapperDrift()
        {
            if (_wrapperDriftCloseRequested) return false;
            _wrapperDriftCloseRequested = true;

            try { PaletteCoordinator.SetStatus(WrapperDriftMessage); } catch { }
            try { Close(); } catch { }
            return false;
        }
    }
}
