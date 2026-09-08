using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class CommercialQsCommands
    {
        private static CommercialQsWindow? _window;
        private static CommercialQsWindow? _unpublishedCandidate;
        private static CommercialQsWindow? _publicationInFlightCandidate;
        private static CommercialQsWindow? _cleanupInFlightCandidate;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DCOMMERCIAL", CommandFlags.Modal)]
        public void ShowCommercialQsWorkspace()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!PrepareUnpublishedCandidate())
                {
                    Report(document, "QS3DCOMMERCIAL: a previous unpublished Commercial QS window has not reached terminal Closed.");
                    return;
                }

                var requestedIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(requestedIdentity))
                {
                    Report(document, "QS3DCOMMERCIAL: the existing Commercial QS window belongs to another drawing and could not close safely.");
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    Report(document, "Commercial QS workspace activated.");
                    return;
                }

                var candidate = new CommercialQsWindow(document);
                candidate.Closed += (_, __) => ReleaseCandidate(candidate);
                _unpublishedCandidate = candidate;
                _publicationInFlightCandidate = candidate;
                try
                {
                    Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                }
                catch (Exception)
                {
                    if (!CloseUnpublishedCandidate(candidate))
                    {
                        Report(document, "QS3DCOMMERCIAL: failed to publish the window and the candidate could not close safely.");
                        return;
                    }

                    Report(document, "QS3DCOMMERCIAL: unable to open the Commercial QS workspace.");
                    return;
                }
                finally
                {
                    if (ReferenceEquals(_publicationInFlightCandidate, candidate))
                        _publicationInFlightCandidate = null;
                }

                if (!candidate.IsLoaded)
                {
                    CloseUnpublishedCandidate(candidate);
                    return;
                }

                _window = candidate;
                _nativeDatabaseIdentity = requestedIdentity;
                if (ReferenceEquals(_unpublishedCandidate, candidate))
                    _unpublishedCandidate = null;
                Report(document, "Commercial QS workspace opened: Variation • IPC • Final Account • Tender • CVR • XLSX.");
            }
            catch (Exception)
            {
                Report(document, "QS3DCOMMERCIAL: unable to open the Commercial QS workspace.");
            }
        }

        private static bool PrepareUnpublishedCandidate()
        {
            if (_cleanupInFlightCandidate != null || _publicationInFlightCandidate != null)
                return false;
            var candidate = _unpublishedCandidate;
            return candidate == null || CloseUnpublishedCandidate(candidate);
        }

        private static bool PreparePublishedWindow(IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;
            if (!published.IsLoaded)
            {
                ReleaseCandidate(published);
                return true;
            }
            if (_nativeDatabaseIdentity == requestedNativeDatabaseIdentity)
                return true;

            _cleanupInFlightCandidate = published;
            try
            {
                published.Close();
            }
            catch
            {
                if (!published.IsLoaded)
                {
                    ReleaseCandidate(published);
                    return true;
                }
                return false;
            }
            finally
            {
                if (ReferenceEquals(_cleanupInFlightCandidate, published))
                    _cleanupInFlightCandidate = null;
            }

            if (published.IsLoaded) return false;
            ReleaseCandidate(published);
            return true;
        }

        private static bool CloseUnpublishedCandidate(CommercialQsWindow candidate)
        {
            _cleanupInFlightCandidate = candidate;
            try
            {
                candidate.Close();
            }
            catch
            {
                if (!candidate.IsLoaded)
                {
                    ReleaseCandidate(candidate);
                    return true;
                }
                _unpublishedCandidate = candidate;
                return false;
            }
            finally
            {
                if (ReferenceEquals(_cleanupInFlightCandidate, candidate))
                    _cleanupInFlightCandidate = null;
            }

            if (!candidate.IsLoaded)
            {
                ReleaseCandidate(candidate);
                return true;
            }

            _unpublishedCandidate = candidate;
            return false;
        }

        private static void ReleaseCandidate(CommercialQsWindow candidate)
        {
            if (ReferenceEquals(_window, candidate))
            {
                _window = null;
                _nativeDatabaseIdentity = IntPtr.Zero;
            }
            if (ReferenceEquals(_unpublishedCandidate, candidate))
                _unpublishedCandidate = null;
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Commercial QS requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Commercial QS requires a live native BricsCAD database.");
            return identity;
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
