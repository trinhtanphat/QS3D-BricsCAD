using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class EstimatingRateBuildUpCommands
    {
        private sealed class WindowOwner
        {
            private readonly WeakReference<Document> _document;

            public WindowOwner(Document document, IntPtr nativeDatabaseIdentity)
            {
                _document = new WeakReference<Document>(document);
                NativeDatabaseIdentity = nativeDatabaseIdentity;
            }

            public IntPtr NativeDatabaseIdentity { get; }

            public bool Matches(Document document, IntPtr nativeDatabaseIdentity)
            {
                return NativeDatabaseIdentity == nativeDatabaseIdentity &&
                       _document.TryGetTarget(out var ownedDocument) &&
                       ReferenceEquals(ownedDocument, document);
            }
        }

        private static EstimatingRateBuildUpWindow? _window;
        private static WindowOwner? _windowOwner;
        private static EstimatingRateBuildUpWindow? _unpublishedCandidate;
        private static WindowOwner? _unpublishedOwner;
        private static EstimatingRateBuildUpWindow? _publicationInFlightCandidate;
        private static EstimatingRateBuildUpWindow? _cleanupInFlightCandidate;

        [CommandMethod("QS3DESTIMATE", CommandFlags.Modal)]
        [CommandMethod("QS3DRATEBUILDUP", CommandFlags.Modal)]
        public void ShowEstimatingWorkspace()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var requestedIdentity = GetNativeDatabaseIdentity(document);
                if (!IsActiveDocumentGeneration(document, requestedIdentity))
                    return;

                if (!PrepareUnpublishedCandidate())
                {
                    ReportIfActive(document, requestedIdentity, "QS3DESTIMATE: a previous unpublished estimating window has not reached terminal Closed.");
                    return;
                }

                if (!IsActiveDocumentGeneration(document, requestedIdentity))
                    return;

                if (!PreparePublishedWindow(document, requestedIdentity))
                {
                    ReportIfActive(document, requestedIdentity, "QS3DESTIMATE: the existing estimating window belongs to another drawing and could not close safely.");
                    return;
                }

                if (!IsActiveDocumentGeneration(document, requestedIdentity))
                    return;

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    ReportIfActive(document, requestedIdentity, "Estimating rate build-up workspace activated.");
                    return;
                }

                var candidate = new EstimatingRateBuildUpWindow(document, requestedIdentity);
                var candidateOwner = new WindowOwner(document, requestedIdentity);
                candidate.Closed += (_, __) => ReleaseCandidate(candidate);
                _unpublishedCandidate = candidate;
                _unpublishedOwner = candidateOwner;
                _publicationInFlightCandidate = candidate;
                try
                {
                    if (!IsActiveDocumentGeneration(document, requestedIdentity))
                    {
                        CloseUnpublishedCandidate(candidate);
                        return;
                    }

                    Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                }
                catch (Exception)
                {
                    if (!CloseUnpublishedCandidate(candidate))
                    {
                        ReportIfActive(document, requestedIdentity, "QS3DESTIMATE: failed to publish the workspace and the candidate could not close safely.");
                        return;
                    }

                    ReportIfActive(document, requestedIdentity, "QS3DESTIMATE: unable to open the estimating workspace.");
                    return;
                }
                finally
                {
                    if (ReferenceEquals(_publicationInFlightCandidate, candidate))
                        _publicationInFlightCandidate = null;
                }

                if (!candidate.IsLoaded || !IsActiveDocumentGeneration(document, requestedIdentity))
                {
                    CloseUnpublishedCandidate(candidate);
                    return;
                }

                _window = candidate;
                _windowOwner = candidateOwner;
                if (ReferenceEquals(_unpublishedCandidate, candidate))
                {
                    _unpublishedCandidate = null;
                    _unpublishedOwner = null;
                }

                ReportIfActive(document, requestedIdentity, "Estimating workspace opened: rate build-up • provenance • revisions • review/approval.");
            }
            catch (Exception)
            {
                TryReportCurrentDocument(document, "QS3DESTIMATE: unable to open the estimating workspace.");
            }
        }

        private static bool PrepareUnpublishedCandidate()
        {
            if (_cleanupInFlightCandidate != null || _publicationInFlightCandidate != null)
                return false;
            var candidate = _unpublishedCandidate;
            return candidate == null || CloseUnpublishedCandidate(candidate);
        }

        private static bool PreparePublishedWindow(Document requestedDocument, IntPtr requestedNativeDatabaseIdentity)
        {
            var published = _window;
            if (published == null) return true;
            if (!published.IsLoaded)
            {
                ReleaseCandidate(published);
                return true;
            }

            var owner = _windowOwner;
            if (owner != null &&
                owner.Matches(requestedDocument, requestedNativeDatabaseIdentity) &&
                IsActiveDocumentGeneration(requestedDocument, requestedNativeDatabaseIdentity))
            {
                return true;
            }

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
                _windowOwner = null;
            }
            if (ReferenceEquals(_unpublishedCandidate, candidate))
            {
                _unpublishedCandidate = null;
                _unpublishedOwner = null;
            }
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

        private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)
        {
            if (nativeDatabaseIdentity == IntPtr.Zero ||
                !ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument))
            {
                return false;
            }

            try
            {
                return document.Database != null &&
                       document.Database.UnmanagedObject == nativeDatabaseIdentity;
            }
            catch
            {
                return false;
            }
        }

        private static void ReportIfActive(Document document, IntPtr nativeDatabaseIdentity, string message)
        {
            if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))
                return;
            Report(document, message);
        }

        private static void TryReportCurrentDocument(Document document, string message)
        {
            IntPtr nativeDatabaseIdentity;
            try { nativeDatabaseIdentity = GetNativeDatabaseIdentity(document); }
            catch { return; }
            ReportIfActive(document, nativeDatabaseIdentity, message);
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
