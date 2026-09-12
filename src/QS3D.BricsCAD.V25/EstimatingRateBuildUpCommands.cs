using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class EstimatingRateBuildUpCommands
    {
        private static EstimatingRateBuildUpWindow? _window;
        private static EstimatingRateBuildUpWindow? _unpublishedCandidate;
        private static EstimatingRateBuildUpWindow? _publicationInFlightCandidate;
        private static EstimatingRateBuildUpWindow? _cleanupInFlightCandidate;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DESTIMATE", CommandFlags.Modal)]
        [CommandMethod("QS3DRATEBUILDUP", CommandFlags.Modal)]
        public void ShowEstimatingWorkspace()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!PrepareUnpublishedCandidate())
                {
                    Report(document, "QS3DESTIMATE: a previous unpublished estimating window has not reached terminal Closed.");
                    return;
                }

                var requestedIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(requestedIdentity))
                {
                    Report(document, "QS3DESTIMATE: the existing estimating window belongs to another drawing and could not close safely.");
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    Report(document, "Estimating rate build-up workspace activated.");
                    return;
                }

                var candidate = new EstimatingRateBuildUpWindow(document, requestedIdentity);
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
                        Report(document, "QS3DESTIMATE: failed to publish the workspace and the candidate could not close safely.");
                        return;
                    }

                    Report(document, "QS3DESTIMATE: unable to open the estimating workspace.");
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
                Report(document, "Estimating workspace opened: rate build-up • provenance • revisions • review/approval.");
            }
            catch (Exception)
            {
                Report(document, "QS3DESTIMATE: unable to open the estimating workspace.");
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

        private static bool CloseUnpublishedCandidate(EstimatingRateBuildUpWindow candidate)
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

        private static void ReleaseCandidate(EstimatingRateBuildUpWindow candidate)
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
                throw new InvalidOperationException("Estimating workspace requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Estimating workspace requires a live native BricsCAD database.");
            return identity;
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
