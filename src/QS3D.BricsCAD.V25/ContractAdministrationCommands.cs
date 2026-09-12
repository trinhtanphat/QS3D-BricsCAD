using System;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class ContractAdministrationCommands
    {
        private static ContractAdministrationWindow? _window;
        private static IntPtr _nativeDatabaseIdentity;

        [CommandMethod("QS3DCONTRACTADMIN", CommandFlags.Modal)]
        [CommandMethod("QS3DCLAIMS", CommandFlags.Modal)]
        public void ShowContractAdministration()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var requestedIdentity = GetNativeDatabaseIdentity(document);
                if (_window != null)
                {
                    if (!_window.IsLoaded)
                    {
                        Release(_window);
                    }
                    else if (_nativeDatabaseIdentity == requestedIdentity)
                    {
                        try { _window.Activate(); } catch { }
                        Report(document, "Contract administration workspace activated.");
                        return;
                    }
                    else
                    {
                        var previous = _window;
                        try { previous.Close(); } catch { }
                        if (previous.IsLoaded)
                        {
                            Report(document, "QS3DCONTRACTADMIN: the existing workspace belongs to another drawing and could not close safely.");
                            return;
                        }
                        Release(previous);
                    }
                }

                var candidate = new ContractAdministrationWindow(document, requestedIdentity);
                candidate.Closed += (_, __) => Release(candidate);
                Application.ShowModelessWindow(IntPtr.Zero, candidate, true);
                if (!candidate.IsLoaded)
                {
                    Release(candidate);
                    Report(document, "QS3DCONTRACTADMIN: modeless workspace did not reach a loaded state.");
                    return;
                }

                _window = candidate;
                _nativeDatabaseIdentity = requestedIdentity;
                Report(document, "Contract administration opened: events • notices • EOT • claims • audit.");
            }
            catch (Exception)
            {
                Report(document, "QS3DCONTRACTADMIN: unable to open the contract administration workspace.");
            }
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

        private static void Release(ContractAdministrationWindow candidate)
        {
            if (!ReferenceEquals(_window, candidate)) return;
            _window = null;
            _nativeDatabaseIdentity = IntPtr.Zero;
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\n" + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }
    }
}
