using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ContractAdministrationCommands
    {
        private static ContractAdministrationWindow? _window;
        private static ContractAdministrationWindow? _unpublishedCandidate;
        private static ContractAdministrationWindow? _publicationInFlightCandidate;
        private static ContractAdministrationWindow? _cleanupInFlightCandidate;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DCONTRACTADMIN", CommandFlags.Modal)]
        [CommandMethod("QS3DCLAIMS", CommandFlags.Modal)]
        public void ShowContractAdministration()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                if (!PrepareUnpublishedCandidate())
                {
                    Report(document, "QS3DCONTRACTADMIN: a previous unpublished workspace has not reached terminal Closed.");
                    return;
                }

                var requestedIdentity = GetNativeDatabaseIdentity(document);
                if (!PreparePublishedWindow(requestedIdentity))
                {
                    Report(document, "QS3DCONTRACTADMIN: the existing workspace belongs to another drawing and could not close safely.");
                    return;
                }

                if (_window != null)
                {
                    try { _window.Activate(); } catch { }
                    Report(document, "Contract administration workspace activated.");
                    return;
                }

                var candidate = new ContractAdministrationWindow(document, requestedIdentity);
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
                        Report(document, "QS3DCONTRACTADMIN: publication failed and the candidate could not close safely.");
                        return;
                    }

                    Report(document, "QS3DCONTRACTADMIN: unable to open the contract administration workspace.");
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
                Report(document, "Contract administration opened: events • notices • EOT • claims • audit.");
            }
            catch (Exception)
            {
                Report(document, "QS3DCONTRACTADMIN: unable to open the contract administration workspace.");
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

        private static bool CloseUnpublishedCandidate(ContractAdministrationWindow candidate)
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

        private static void ReleaseCandidate(ContractAdministrationWindow candidate)
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
                throw new InvalidOperationException("Contract administration requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Contract administration requires a live native BricsCAD database.");
            return identity;
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
