using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class EstimatingRateWorkspaceCommands
    {
        private static EstimatingRateWorkspaceWindow? _window;
        private static EstimatingRateWorkspaceWindow? _pendingWindow;
        private static IntPtr _nativeDatabaseIdentity;
        private static IntPtr _pendingNativeDatabaseIdentity;
        private static bool _publicationInFlight;

        [CommandMethod("QS3DESTIMATING", CommandFlags.Modal)]
        public void ShowEstimatingRateWorkspace()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var identity = NativeDatabaseIdentity(document);
                if (!PreparePendingWindow(document)) return;
                if (!PrepareExistingWindow(identity, document)) return;
                if (_window != null)
                {
                    _window.RefreshWorkspace();
                    try { _window.Activate(); } catch { }
                    Report(document, "Estimating & Rate Build-up workspace activated.");
                    return;
                }

                var candidate = new EstimatingRateWorkspaceWindow(document);
                candidate.Closed += (_, __) => Release(candidate);
                _pendingWindow = candidate;
                _pendingNativeDatabaseIdentity = identity;
                if (!HasExactAffinity(document, identity))
                {
                    DiscardPending(candidate);
                    Report(document, "QS3DESTIMATING: active drawing changed before publication.");
                    return;
                }

                _publicationInFlight = true;
                try
                {
                    Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                }
                catch
                {
                    DiscardPending(candidate);
                    Report(document, "QS3DESTIMATING: unable to publish the estimating workspace.");
                    return;
                }
                finally
                {
                    _publicationInFlight = false;
                }
                if (!ReferenceEquals(_pendingWindow, candidate) ||
                    _pendingNativeDatabaseIdentity != identity ||
                    !candidate.IsLoaded ||
                    !HasExactAffinity(document, identity))
                {
                    DiscardPending(candidate);
                    Report(document, "QS3DESTIMATING: publication lost exact drawing ownership.");
                    return;
                }

                _window = candidate;
                _nativeDatabaseIdentity = identity;
                _pendingWindow = null;
                _pendingNativeDatabaseIdentity = IntPtr.Zero;
                Report(document, "Estimating & Rate Build-up workspace opened.");
            }
            catch (Exception ex)
            {
                Report(document, "QS3DESTIMATING: " + ex.Message);
            }
        }

        private static bool PreparePendingWindow(Document document)
        {
            if (_publicationInFlight)
            {
                Report(document, "QS3DESTIMATING: modeless publication is already in progress.");
                return false;
            }
            if (_pendingWindow == null) return true;
            if (!_pendingWindow.IsLoaded)
            {
                _pendingWindow = null;
                _pendingNativeDatabaseIdentity = IntPtr.Zero;
                return true;
            }
            Report(document, "QS3DESTIMATING: a pending estimating workspace already owns publication.");
            return false;
        }

        private static bool PrepareExistingWindow(IntPtr identity, Document document)
        {
            if (_window == null) return true;
            if (!_window.IsLoaded)
            {
                Release(_window);
                return true;
            }
            if (_nativeDatabaseIdentity == identity) return true;

            var previous = _window;
            try { previous.Close(); }
            catch
            {
                Report(document, "QS3DESTIMATING: existing workspace belongs to another drawing and could not close safely.");
                return false;
            }

            if (previous.IsLoaded) return false;
            Release(previous);
            return true;
        }

        private static bool HasExactAffinity(Document document, IntPtr identity)
        {
            var active = Application.DocumentManager.MdiActiveDocument;
            return ReferenceEquals(active, document) &&
                   document.Database != null &&
                   document.Database.UnmanagedObject == identity &&
                   identity != IntPtr.Zero;
        }

        private static IntPtr NativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Estimating workspace requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Estimating workspace requires a live native BricsCAD database.");
            return identity;
        }

        private static void DiscardPending(EstimatingRateWorkspaceWindow candidate)
        {
            try { candidate.Close(); } catch { }
            if (ReferenceEquals(_pendingWindow, candidate))
            {
                _pendingWindow = null;
                _pendingNativeDatabaseIdentity = IntPtr.Zero;
            }
        }

        private static void Release(EstimatingRateWorkspaceWindow candidate)
        {
            if (ReferenceEquals(_window, candidate))
            {
                _window = null;
                _nativeDatabaseIdentity = IntPtr.Zero;
            }
            if (ReferenceEquals(_pendingWindow, candidate) && !_publicationInFlight)
            {
                _pendingWindow = null;
                _pendingNativeDatabaseIdentity = IntPtr.Zero;
            }
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
