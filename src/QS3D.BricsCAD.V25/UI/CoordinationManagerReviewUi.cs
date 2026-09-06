using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Coordination;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using Teigha.DatabaseServices;
using Teigha.Geometry;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Adds document-bound visual review actions to the persisted Coordination Manager.
    /// This controller deliberately owns only transient CAD presentation state. Persisted
    /// issue identity continues to be semantic/project identity; ObjectIds live only for
    /// the duration of one validated UI action/session.
    /// </summary>
    internal static class CoordinationManagerReviewUi
    {
        public static void Attach(
            CoordinationManagerWindow window,
            Document document,
            string projectId,
            string drawingFingerprint)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (document == null) throw new ArgumentNullException(nameof(document));

            var controller = new Controller(
                window,
                document,
                RequireToken(projectId, nameof(projectId)),
                RequireToken(drawingFingerprint, nameof(drawingFingerprint)));
            controller.Attach();
        }

        private static string RequireToken(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Value is required.", parameterName);
            if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1]))
                throw new ArgumentException("Value must use canonical identity without surrounding whitespace.", parameterName);
            if (value.Any(char.IsControl))
                throw new ArgumentException("Value must not contain control characters.", parameterName);
            return value;
        }

        private sealed class Controller : IDisposable
        {
            [Flags]
            private enum Attachment
            {
                None = 0,
                Highlight = 1 << 0,
                ClearHighlight = 1 << 1,
                Isolate = 1 << 2,
                RestoreIsolation = 1 << 3,
                Section = 1 << 4,
                RestoreView = 1 << 5,
                GridSelection = 1 << 6,
                WindowClosing = 1 << 7,
                WindowClosed = 1 << 8,
                DocumentToBeDeactivated = 1 << 9,
                DocumentActivated = 1 << 10,
                DocumentToBeDestroyed = 1 << 11,
            }

            private readonly CoordinationManagerWindow _window;
            private readonly Document _document;
            private readonly string _projectId;
            private readonly string _drawingFingerprint;
            private readonly TransientReviewSession _session;
            private readonly DockPanel _root;
            private readonly DataGrid _grid;
            private readonly TextBlock _status;
            private readonly Button _highlight;
            private readonly Button _clearHighlight;
            private readonly Button _isolate;
            private readonly Button _restoreIsolation;
            private readonly Button _section;
            private readonly Button _restoreView;
            private Attachment _attachments;
            private bool _attached;
            private bool _cleanupBarrier;
            private bool _disposeInProgress;
            private bool _sessionDisposed;
            private bool _disposed;

            public Controller(
                CoordinationManagerWindow window,
                Document document,
                string projectId,
                string drawingFingerprint)
            {
                _window = window;
                _document = document;
                _projectId = projectId;
                _drawingFingerprint = drawingFingerprint;
                _session = new TransientReviewSession(document);

                _root = window.Content as DockPanel
                    ?? throw new InvalidOperationException("Coordination Manager root layout is unavailable.");
                _grid = _root.Children.OfType<DataGrid>().FirstOrDefault()
                    ?? throw new InvalidOperationException("Coordination Manager grid is unavailable.");

                var review = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };
                DockPanel.SetDock(review, Dock.Top);
                _root.Children.Insert(Math.Min(1, _root.Children.Count), review);

                review.Children.Add(new TextBlock
                {
                    Text = "Review CAD",
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0)
                });

                _highlight = AddButton(review, "Highlight", 86);
                _clearHighlight = AddButton(review, "Clear", 70);
                _isolate = AddButton(review, "Isolate", 78);
                _restoreIsolation = AddButton(review, "Restore", 78);
                _section = AddButton(review, "Section / Focus", 112);
                _restoreView = AddButton(review, "Restore View", 102);

                _status = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                review.Children.Add(_status);
            }

            private bool IsOwnerDocumentActive =>
                ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document);

            public void Attach()
            {
                if (_disposed || _disposeInProgress)
                    throw new ObjectDisposedException(nameof(Controller));
                if (_attached) return;
                if (_attachments != Attachment.None)
                    throw new InvalidOperationException("Coordination review controller has incomplete prior attachment cleanup.");

                try
                {
                    _highlight.Click += OnHighlight;
                    _attachments |= Attachment.Highlight;
                    _clearHighlight.Click += OnClearHighlight;
                    _attachments |= Attachment.ClearHighlight;
                    _isolate.Click += OnIsolate;
                    _attachments |= Attachment.Isolate;
                    _restoreIsolation.Click += OnRestoreIsolation;
                    _attachments |= Attachment.RestoreIsolation;
                    _section.Click += OnSection;
                    _attachments |= Attachment.Section;
                    _restoreView.Click += OnRestoreView;
                    _attachments |= Attachment.RestoreView;
                    _grid.SelectionChanged += OnSelectionChanged;
                    _attachments |= Attachment.GridSelection;
                    _window.Closing += OnWindowClosing;
                    _attachments |= Attachment.WindowClosing;
                    _window.Closed += OnWindowClosed;
                    _attachments |= Attachment.WindowClosed;
                    Bricscad.ApplicationServices.Application.DocumentManager.DocumentToBeDeactivated += OnDocumentToBeDeactivated;
                    _attachments |= Attachment.DocumentToBeDeactivated;
                    Bricscad.ApplicationServices.Application.DocumentManager.DocumentActivated += OnDocumentActivated;
                    _attachments |= Attachment.DocumentActivated;
                    Bricscad.ApplicationServices.Application.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
                    _attachments |= Attachment.DocumentToBeDestroyed;

                    UpdateActionState();
                    _attached = true;
                }
                catch
                {
                    _attached = false;
                    DetachHandlersBestEffort();
                    DisposeSessionBestEffort();
                    _disposed = _attachments == Attachment.None && _sessionDisposed;
                    throw;
                }
            }

            private static Button AddButton(Panel panel, string text, double width)
            {
                var button = new Button
                {
                    Content = text,
                    MinWidth = width,
                    Margin = new Thickness(0, 0, 6, 0)
                };
                panel.Children.Add(button);
                return button;
            }

            private void OnHighlight(object sender, RoutedEventArgs e)
            {
                RunValidated("Highlight", ids => _session.Highlight(ids));
            }

            private void OnClearHighlight(object sender, RoutedEventArgs e)
            {
                RunCleanup("Clear Highlight", () => _session.ClearHighlight());
            }

            private void OnIsolate(object sender, RoutedEventArgs e)
            {
                RunValidated("Isolate", ids => _session.Isolate(ids));
            }

            private void OnRestoreIsolation(object sender, RoutedEventArgs e)
            {
                RunCleanup("Restore Isolation", () => _session.RestoreIsolation());
            }

            private void OnSection(object sender, RoutedEventArgs e)
            {
                RunValidated("Section / Focus", ids => _session.ApplySectionFocus(ids));
            }

            private void OnRestoreView(object sender, RoutedEventArgs e)
            {
                RunCleanup("Restore View", () => _session.RestoreSectionView());
            }

            private void RunCleanup(string actionName, Action effect)
            {
                if (!_attached || _disposeInProgress || _disposed) return;
                if (!IsOwnerDocumentActive)
                {
                    _cleanupBarrier = _session.HasTransientState;
                    UpdateActionState();
                    return;
                }

                try
                {
                    effect();
                    _cleanupBarrier = _session.HasTransientState;
                    SetStatus(_cleanupBarrier
                        ? actionName + " • cleanup còn pending; review mới vẫn bị khóa."
                        : actionName + " • transient review state đã được dọn sạch.");
                }
                catch (Exception ex)
                {
                    _cleanupBarrier = _session.HasTransientState;
                    SetStatus(actionName + " bị từ chối: " + ex.Message);
                }
                finally
                {
                    UpdateActionState();
                }
            }

            private void RunValidated(string actionName, Action<IReadOnlyList<ObjectId>> effect)
            {
                if (!_attached || _disposeInProgress || _disposed) return;
                if (!IsOwnerDocumentActive)
                {
                    _cleanupBarrier = _session.HasTransientState;
                    UpdateActionState();
                    return;
                }
                if (_cleanupBarrier)
                {
                    SetStatus("Review mới bị khóa cho tới khi transient state của row trước được dọn sạch.");
                    UpdateActionState();
                    return;
                }

                try
                {
                    // IMPORTANT: all canonical provenance/relink/full-pair checks complete
                    // before the supplied native CAD effect is invoked.
                    var resolved = ResolveReviewTargets();
                    effect(resolved);
                    SetStatus(actionName + " • " + resolved.Count + " object(s) • validated full pair.");
                }
                catch (Exception ex)
                {
                    _cleanupBarrier = _session.HasTransientState;
                    SetStatus(actionName + " bị từ chối: " + ex.Message);
                }
                finally
                {
                    UpdateActionState();
                }
            }

            private IReadOnlyList<ObjectId> ResolveReviewTargets()
            {
                var selected = _grid.SelectedItem as CoordinationManagerRow
                    ?? throw new InvalidOperationException("Hãy chọn một coordination issue trước.");
                if (!selected.CanLocate)
                    throw new InvalidOperationException("Issue hiện tại không actionable; CAD state không đổi.");

                var project = RequireCurrentProject();
                var snapshot = CoordinationIssuePersistence.Load(project)
                    ?? throw new InvalidOperationException("Coordination persistence không còn tồn tại.");
                var issue = snapshot.Find(selected.IssueId)
                    ?? throw new InvalidOperationException("Issue đã bị xóa hoặc thay thế từ lúc hiển thị.");
                if (issue.UpdatedAtUtc != selected.UpdatedAtUtc)
                    throw new InvalidOperationException("Issue đã thay đổi từ lúc hiển thị; làm mới trước khi review.");

                var relink = snapshot.EvaluateRelink(project, issue.IssueId);
                if (relink.Status != CoordinationRelinkStatus.ReadyForHostValidation &&
                    relink.Status != CoordinationRelinkStatus.Relinked)
                    throw new InvalidOperationException("Issue không thể review trong CAD: " + relink.Status + ".");

                var leftHandles = CanonicalHandles(SourceHandleResolver.Resolve(project, new[] { issue.LeftSemanticId }));
                var rightHandles = CanonicalHandles(SourceHandleResolver.Resolve(project, new[] { issue.RightSemanticId }));
                if (leftHandles.Count == 0 || rightHandles.Count == 0)
                    throw new InvalidOperationException("Issue thiếu source Handle hiện hành ở một hoặc cả hai phía; CAD state không đổi.");

                var handles = leftHandles.Concat(rightHandles)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var resolved = CadHandleService.Resolve(_document, handles);
                if (resolved.Count != handles.Count)
                    throw new InvalidOperationException("Không resolve đủ toàn bộ source Handle hiện hành; CAD state không đổi.");

                return resolved.ToList().AsReadOnly();
            }

            private ProjectState RequireCurrentProject()
            {
                if (!IsOwnerDocumentActive)
                    throw new InvalidOperationException("DWG đã đổi; review action không được phép tác động lên document khác.");

                if (!ProjectContextCoordinator.TryGetReadOnly(_document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng.");

                if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal) ||
                    !string.Equals(project.DrawingFingerprint, _drawingFingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException("Project/Drawing Fingerprint đã đổi; review action bị fail-closed.");
                return project;
            }

            private static IReadOnlyList<string> CanonicalHandles(IEnumerable<string> handles)
            {
                return (handles ?? throw new ArgumentNullException(nameof(handles)))
                    .Select(value => CadHandleService.NormalizeHexHandle(value)
                        ?? throw new InvalidOperationException("Project contains an invalid source CAD Handle."))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();
            }

            private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                if (!_attached || _disposeInProgress || _disposed) return;
                if (!IsOwnerDocumentActive)
                {
                    _cleanupBarrier = _session.HasTransientState;
                    UpdateActionState();
                    return;
                }

                // A previous row must never leak presentation state into the next row.
                var cleanupFailure = _session.TryResetTransientStateBestEffort();
                _cleanupBarrier = cleanupFailure != null || _session.HasTransientState;
                SetStatus(_cleanupBarrier
                    ? "Không thể dọn sạch review state của row trước; chỉ các nút cleanup được phép retry."
                    : string.Empty);
                UpdateActionState();
            }

            private void OnDocumentToBeDeactivated(object sender, DocumentCollectionEventArgs e)
            {
                if (!_attached || _disposeInProgress || _disposed || !ReferenceEquals(e.Document, _document)) return;

                var cleanupFailure = _session.TryResetTransientStateBestEffort();
                _cleanupBarrier = cleanupFailure != null || _session.HasTransientState;
                if (_cleanupBarrier)
                    SetStatus("Không thể dọn sạch transient review state trước khi đổi DWG; review bị khóa tới khi DWG này hoạt động lại.");
                UpdateActionState();
            }

            private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
            {
                if (!_attached || _disposeInProgress || _disposed) return;

                var ownerActive = IsOwnerDocumentActive;
                if (ownerActive)
                {
                    if (_cleanupBarrier || _session.HasTransientState)
                    {
                        var cleanupFailure = _session.TryResetTransientStateBestEffort();
                        _cleanupBarrier = cleanupFailure != null || _session.HasTransientState;
                        SetStatus(_cleanupBarrier
                            ? "Transient review cleanup vẫn pending; review CAD tiếp tục bị khóa."
                            : "Transient review state đã được dọn sạch sau khi DWG hoạt động lại.");
                    }
                    UpdateActionState();
                    return;
                }

                // The new active document is foreign to this controller. Never clean the
                // owner session here: Application-level system-variable APIs would target
                // the foreign active host context. Pre-deactivation owns that cleanup.
                _cleanupBarrier = _session.HasTransientState;
                UpdateActionState();
                if (_cleanupBarrier)
                {
                    _status.Text = "Review CAD đang chờ cleanup trên DWG sở hữu; kích hoạt lại DWG đó để retry.";
                    return;
                }

                if (_window.IsLoaded) _window.Close();
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                if (!_attached || _disposeInProgress || _disposed || !ReferenceEquals(e.Document, _document)) return;
                _session.AbandonDestroyedDocumentState();
                _cleanupBarrier = false;
                if (_window.IsLoaded) _window.Close();
            }

            private void OnWindowClosing(object sender, CancelEventArgs e)
            {
                if (!_attached || _disposeInProgress || _disposed) return;
                if (e.Cancel) return;

                if (!IsOwnerDocumentActive)
                {
                    if (!_session.HasTransientState) return;
                    e.Cancel = true;
                    _cleanupBarrier = true;
                    _status.Text = "Không thể đóng Coordination Manager khi cleanup của DWG sở hữu còn pending; kích hoạt lại DWG đó để retry.";
                    UpdateActionState();
                    return;
                }

                var cleanupFailure = _session.TryResetTransientStateBestEffort();
                if (cleanupFailure == null && !_session.HasTransientState)
                    return;

                e.Cancel = true;
                _cleanupBarrier = true;
                SetStatus("Không thể đóng Coordination Manager khi transient review state còn pending; hãy retry cleanup trước.");
                UpdateActionState();
            }

            private void OnWindowClosed(object sender, EventArgs e)
            {
                Dispose();
            }

            private void UpdateActionState()
            {
                if (_disposeInProgress || _disposed) return;

                var ownerActive = IsOwnerDocumentActive;
                var row = _grid.SelectedItem as CoordinationManagerRow;
                var actionable = row != null && row.CanLocate;
                var mutationsAllowed = actionable && !_cleanupBarrier;
                _highlight.IsEnabled = ownerActive && mutationsAllowed;
                _isolate.IsEnabled = ownerActive && mutationsAllowed;
                _section.IsEnabled = ownerActive && mutationsAllowed;
                _clearHighlight.IsEnabled = ownerActive && _session.HasHighlight;
                _restoreIsolation.IsEnabled = ownerActive && _session.HasIsolation;
                _restoreView.IsEnabled = ownerActive && _session.HasSectionView;
            }

            private void SetStatus(string message)
            {
                _status.Text = message ?? string.Empty;
                if (_status.Text.Length == 0 || !IsOwnerDocumentActive) return;
                try { PaletteCoordinator.SetStatus(_status.Text); } catch { }
                try { _document.Editor.WriteMessage("\nQS3D Coordination review: " + _status.Text); } catch { }
            }

            public void Dispose()
            {
                if (_disposed || _disposeInProgress) return;

                _disposeInProgress = true;
                _attached = false;
                try
                {
                    DetachHandlersBestEffort();
                    DisposeSessionBestEffort();
                    _disposed = _attachments == Attachment.None && _sessionDisposed;
                }
                finally
                {
                    _disposeInProgress = false;
                }
            }

            private void DetachHandlersBestEffort()
            {
                // External BricsCAD publishers are detached first so they cannot keep this
                // document-bound controller alive while local WPF cleanup continues.
                TryDetach(Attachment.DocumentToBeDestroyed, () =>
                    Bricscad.ApplicationServices.Application.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed);
                TryDetach(Attachment.DocumentActivated, () =>
                    Bricscad.ApplicationServices.Application.DocumentManager.DocumentActivated -= OnDocumentActivated);
                TryDetach(Attachment.DocumentToBeDeactivated, () =>
                    Bricscad.ApplicationServices.Application.DocumentManager.DocumentToBeDeactivated -= OnDocumentToBeDeactivated);
                TryDetach(Attachment.WindowClosed, () => _window.Closed -= OnWindowClosed);
                TryDetach(Attachment.WindowClosing, () => _window.Closing -= OnWindowClosing);
                TryDetach(Attachment.GridSelection, () => _grid.SelectionChanged -= OnSelectionChanged);
                TryDetach(Attachment.RestoreView, () => _restoreView.Click -= OnRestoreView);
                TryDetach(Attachment.Section, () => _section.Click -= OnSection);
                TryDetach(Attachment.RestoreIsolation, () => _restoreIsolation.Click -= OnRestoreIsolation);
                TryDetach(Attachment.Isolate, () => _isolate.Click -= OnIsolate);
                TryDetach(Attachment.ClearHighlight, () => _clearHighlight.Click -= OnClearHighlight);
                TryDetach(Attachment.Highlight, () => _highlight.Click -= OnHighlight);
            }

            private void TryDetach(Attachment attachment, Action detach)
            {
                if ((_attachments & attachment) == 0) return;

                try
                {
                    detach();
                    _attachments &= ~attachment;
                }
                catch
                {
                    // Preserve ownership so a later Dispose call can retry this exact detach.
                }
            }

            private void DisposeSessionBestEffort()
            {
                if (_sessionDisposed) return;

                try
                {
                    _session.Dispose();
                    _sessionDisposed = true;
                }
                catch
                {
                    // A later Dispose call retries transient CAD cleanup without masking
                    // failures from another detach or from constructor/attach rollback.
                }
            }
        }

        private sealed class TransientReviewSession : IDisposable
        {
            private readonly Document _document;
            private readonly List<ObjectId> _highlighted = new List<ObjectId>();
            private bool _isolationActive;
            private object? _objectIsolationModeBefore;
            private ViewSnapshot? _viewBeforeSection;
            private bool _destroyed;
            private bool _disposeInProgress;
            private bool _disposed;

            public TransientReviewSession(Document document)
            {
                _document = document ?? throw new ArgumentNullException(nameof(document));
            }

            public bool HasHighlight => _highlighted.Count > 0;
            public bool HasIsolation => _isolationActive || _objectIsolationModeBefore != null;
            public bool HasSectionView => _viewBeforeSection != null;
            public bool HasTransientState => HasHighlight || HasIsolation || HasSectionView;
            private bool IsOwnerDocumentActive =>
                ReferenceEquals(Bricscad.ApplicationServices.Application.DocumentManager.MdiActiveDocument, _document);

            public void Highlight(IReadOnlyList<ObjectId> ids)
            {
                RequireTargets(ids);
                ClearHighlight();
                var pending = new List<ObjectId>();
                try
                {
                    using (_document.LockDocument())
                    using (var transaction = _document.Database.TransactionManager.StartTransaction())
                    {
                        foreach (var id in ids)
                        {
                            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity
                                ?? throw new InvalidOperationException("Resolved CAD object is not an Entity.");
                            entity.Highlight();
                            pending.Add(id);
                        }
                        transaction.Commit();
                    }

                    _highlighted.AddRange(pending);
                }
                catch
                {
                    var rollbackPending = UnhighlightAttemptBestEffort(pending);
                    _highlighted.AddRange(rollbackPending);
                    throw;
                }
            }

            private IReadOnlyList<ObjectId> UnhighlightAttemptBestEffort(IReadOnlyList<ObjectId> pending)
            {
                if (pending == null || pending.Count == 0 || _destroyed)
                    return Array.Empty<ObjectId>();

                var unreleased = new List<ObjectId>();
                try
                {
                    using (_document.LockDocument())
                    using (var transaction = _document.Database.TransactionManager.StartTransaction())
                    {
                        foreach (var id in pending)
                        {
                            try
                            {
                                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity
                                    ?? throw new InvalidOperationException("Highlight rollback target is not an Entity.");
                                entity.Unhighlight();
                            }
                            catch
                            {
                                // Continue compensating the remaining entities, but retain
                                // this native highlight as session-owned cleanup debt.
                                unreleased.Add(id);
                            }
                        }
                        transaction.Commit();
                    }
                }
                catch
                {
                    // If the compensation transaction itself is not confirmed, conservatively
                    // retain the whole attempt so a later ClearHighlight/Dispose can retry it.
                    return pending.ToArray();
                }

                return unreleased.AsReadOnly();
            }

            public void ClearHighlight()
            {
                if (_highlighted.Count == 0) return;
                var pending = _highlighted.ToArray();
                if (_destroyed)
                {
                    _highlighted.Clear();
                    return;
                }

                var released = new List<ObjectId>();
                Exception? cleanupFailure = null;
                using (_document.LockDocument())
                using (var transaction = _document.Database.TransactionManager.StartTransaction())
                {
                    foreach (var id in pending)
                    {
                        try
                        {
                            var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity
                                ?? throw new InvalidOperationException("Owned highlight target is not an Entity.");
                            entity.Unhighlight();
                            released.Add(id);
                        }
                        catch (Exception ex)
                        {
                            cleanupFailure = cleanupFailure ?? ex;
                        }
                    }
                    transaction.Commit();
                }

                foreach (var id in released)
                    _highlighted.Remove(id);

                if (cleanupFailure != null)
                    throw new InvalidOperationException(
                        "Highlight cleanup is incomplete; failed entities remain owned for retry.",
                        cleanupFailure);
            }

            public void Isolate(IReadOnlyList<ObjectId> ids)
            {
                RequireTargets(ids);
                if (!IsOwnerDocumentActive)
                    throw new InvalidOperationException("Owner DWG is not active; isolation mutation is blocked.");
                if (HasIsolation)
                {
                    RestoreIsolation();
                    if (HasIsolation)
                        throw new InvalidOperationException("Previous isolation cleanup is still pending.");
                }

                var impliedSelectionBefore = CadSelectionGuard.ReadImpliedSelection(_document);
                var modeBefore = Bricscad.ApplicationServices.Application.GetSystemVariable("OBJECTISOLATIONMODE");
                try
                {
                    Bricscad.ApplicationServices.Application.SetSystemVariable("OBJECTISOLATIONMODE", 0);
                    _document.Editor.SetImpliedSelection(ids.ToArray());
                    _document.SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);
                }
                catch
                {
                    RestoreImpliedSelectionBestEffort(impliedSelectionBefore);
                    if (!TryRestoreObjectIsolationModeBestEffort(modeBefore))
                        _objectIsolationModeBefore = modeBefore;
                    throw;
                }

                _objectIsolationModeBefore = modeBefore;
                _isolationActive = true;
            }

            public void RestoreIsolation()
            {
                if (!_isolationActive)
                {
                    RestoreObjectIsolationModeBestEffort();
                    return;
                }
                if (_destroyed)
                {
                    _isolationActive = false;
                    _objectIsolationModeBefore = null;
                    return;
                }
                if (!IsOwnerDocumentActive)
                    throw new InvalidOperationException("Owner DWG is not active; isolation cleanup remains pending.");

                _document.SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);
                _isolationActive = false;
                RestoreObjectIsolationModeBestEffort();
            }

            public void ApplySectionFocus(IReadOnlyList<ObjectId> ids)
            {
                RequireTargets(ids);
                RestoreSectionView();

                var bounds = ReadBounds(ids);
                var center = new Point3d(
                    (bounds.MinPoint.X + bounds.MaxPoint.X) * 0.5,
                    (bounds.MinPoint.Y + bounds.MaxPoint.Y) * 0.5,
                    (bounds.MinPoint.Z + bounds.MaxPoint.Z) * 0.5);
                var diagonal = (bounds.MaxPoint - bounds.MinPoint).Length;
                if (!(diagonal > 1e-9) || double.IsNaN(diagonal) || double.IsInfinity(diagonal))
                    throw new InvalidOperationException("Không thể tạo section/focus từ extents suy biến.");

                using (var view = _document.Editor.GetCurrentView())
                {
                    var viewBeforeSection = ViewSnapshot.Capture(view);
                    var direction = view.ViewDirection.GetNormal();
                    var distances = Corners(bounds)
                        .Select(point => (point - center).DotProduct(direction))
                        .ToArray();
                    var margin = Math.Max(diagonal * 0.05, 1e-6);
                    var minDistance = distances.Min() - margin;
                    var maxDistance = distances.Max() + margin;
                    var aspect = view.Height > 1e-9 ? view.Width / view.Height : 1.0;
                    if (!(aspect > 1e-9) || double.IsNaN(aspect) || double.IsInfinity(aspect)) aspect = 1.0;
                    var span = diagonal * 1.25;

                    view.Target = center;
                    view.CenterPoint = new Point2d(0.0, 0.0);
                    if (aspect >= 1.0)
                    {
                        view.Height = span;
                        view.Width = span * aspect;
                    }
                    else
                    {
                        view.Width = span;
                        view.Height = span / aspect;
                    }
                    view.FrontClipAtEye = false;
                    view.BackClipDistance = minDistance;
                    view.FrontClipDistance = maxDistance;
                    view.BackClipEnabled = true;
                    view.FrontClipEnabled = true;
                    try
                    {
                        _document.Editor.SetCurrentView(view);
                    }
                    catch
                    {
                        if (!TryRestoreSectionViewBestEffort(viewBeforeSection))
                            _viewBeforeSection = viewBeforeSection;
                        throw;
                    }
                    _viewBeforeSection = viewBeforeSection;
                }
            }

            public void RestoreSectionView()
            {
                if (_viewBeforeSection == null) return;
                var snapshot = _viewBeforeSection;
                if (_destroyed)
                {
                    _viewBeforeSection = null;
                    return;
                }

                using (var view = _document.Editor.GetCurrentView())
                {
                    snapshot.Apply(view);
                    _document.Editor.SetCurrentView(view);
                }

                _viewBeforeSection = null;
            }

            private bool TryRestoreSectionViewBestEffort(ViewSnapshot snapshot)
            {
                if (snapshot == null || _destroyed) return true;
                try
                {
                    using (var view = _document.Editor.GetCurrentView())
                    {
                        snapshot.Apply(view);
                        _document.Editor.SetCurrentView(view);
                    }
                    return true;
                }
                catch
                {
                    // Compensation remains best-effort so the original native apply failure
                    // stays primary; false transfers the snapshot into persistent retry ownership.
                    return false;
                }
            }

            private Extents3d ReadBounds(IReadOnlyList<ObjectId> ids)
            {
                var hasBounds = false;
                var min = new Point3d();
                var max = new Point3d();
                using (_document.LockDocument())
                using (var transaction = _document.Database.TransactionManager.StartTransaction())
                {
                    foreach (var id in ids)
                    {
                        var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity
                            ?? throw new InvalidOperationException("Resolved CAD object is not an Entity.");
                        Extents3d extents;
                        try
                        {
                            extents = entity.GeometricExtents;
                        }
                        catch (Exception ex)
                        {
                            throw new InvalidOperationException("Không đọc được geometric extents của full-pair target.", ex);
                        }

                        if (!hasBounds)
                        {
                            min = extents.MinPoint;
                            max = extents.MaxPoint;
                            hasBounds = true;
                        }
                        else
                        {
                            min = new Point3d(
                                Math.Min(min.X, extents.MinPoint.X),
                                Math.Min(min.Y, extents.MinPoint.Y),
                                Math.Min(min.Z, extents.MinPoint.Z));
                            max = new Point3d(
                                Math.Max(max.X, extents.MaxPoint.X),
                                Math.Max(max.Y, extents.MaxPoint.Y),
                                Math.Max(max.Z, extents.MaxPoint.Z));
                        }
                    }
                    transaction.Commit();
                }

                if (!hasBounds) throw new InvalidOperationException("Không có extents để section/focus.");
                return new Extents3d(min, max);
            }

            private static IEnumerable<Point3d> Corners(Extents3d bounds)
            {
                var min = bounds.MinPoint;
                var max = bounds.MaxPoint;
                yield return new Point3d(min.X, min.Y, min.Z);
                yield return new Point3d(min.X, min.Y, max.Z);
                yield return new Point3d(min.X, max.Y, min.Z);
                yield return new Point3d(min.X, max.Y, max.Z);
                yield return new Point3d(max.X, min.Y, min.Z);
                yield return new Point3d(max.X, min.Y, max.Z);
                yield return new Point3d(max.X, max.Y, min.Z);
                yield return new Point3d(max.X, max.Y, max.Z);
            }

            private static void RequireTargets(IReadOnlyList<ObjectId> ids)
            {
                if (ids == null || ids.Count == 0)
                    throw new InvalidOperationException("Validated review target set is empty.");
            }

            public void ResetTransientStateBestEffort()
            {
                ResetTransientStateBestEffort(false);
            }

            public Exception? TryResetTransientStateBestEffort()
            {
                return ResetTransientStateBestEffort(false);
            }

            private Exception? ResetTransientStateBestEffort(bool throwOnSectionRestoreFailure)
            {
                Exception? cleanupFailure = null;
                try { ClearHighlight(); } catch (Exception ex) { cleanupFailure = ex; }
                try { RestoreIsolation(); } catch (Exception ex) { cleanupFailure = cleanupFailure ?? ex; }
                try { RestoreSectionView(); }
                catch (Exception ex)
                {
                    cleanupFailure = cleanupFailure ?? ex;
                }

                if (throwOnSectionRestoreFailure && cleanupFailure != null)
                    throw cleanupFailure;
                return cleanupFailure;
            }

            public void AbandonDestroyedDocumentState()
            {
                _destroyed = true;
                _highlighted.Clear();
                _isolationActive = false;
                _viewBeforeSection = null;
                // The owner document is terminal. Do not publish its saved application-level
                // OBJECTISOLATIONMODE through whichever different document is now active.
                _objectIsolationModeBefore = null;
            }

            private void RestoreImpliedSelectionBestEffort(ObjectId[] impliedSelectionBefore)
            {
                if (impliedSelectionBefore == null || _destroyed) return;
                try { _document.Editor.SetImpliedSelection(impliedSelectionBefore); } catch { }
            }

            private void RestoreObjectIsolationModeBestEffort()
            {
                if (_objectIsolationModeBefore == null) return;
                var value = _objectIsolationModeBefore;
                if (TryRestoreObjectIsolationModeBestEffort(value))
                    _objectIsolationModeBefore = null;
            }

            private bool TryRestoreObjectIsolationModeBestEffort(object? modeBefore)
            {
                if (modeBefore == null) return true;
                if (!IsOwnerDocumentActive) return false;
                try
                {
                    Bricscad.ApplicationServices.Application.SetSystemVariable("OBJECTISOLATIONMODE", modeBefore);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            public void Dispose()
            {
                if (_disposed || _disposeInProgress) return;

                _disposeInProgress = true;
                try
                {
                    ResetTransientStateBestEffort(true);
                    _disposed = true;
                }
                finally
                {
                    _disposeInProgress = false;
                }
            }

            private sealed class ViewSnapshot
            {
                private readonly Point3d _target;
                private readonly Point2d _centerPoint;
                private readonly double _height;
                private readonly double _width;
                private readonly bool _frontClipAtEye;
                private readonly double _frontClipDistance;
                private readonly double _backClipDistance;
                private readonly bool _frontClipEnabled;
                private readonly bool _backClipEnabled;

                private ViewSnapshot(
                    Point3d target,
                    Point2d centerPoint,
                    double height,
                    double width,
                    bool frontClipAtEye,
                    double frontClipDistance,
                    double backClipDistance,
                    bool frontClipEnabled,
                    bool backClipEnabled)
                {
                    _target = target;
                    _centerPoint = centerPoint;
                    _height = height;
                    _width = width;
                    _frontClipAtEye = frontClipAtEye;
                    _frontClipDistance = frontClipDistance;
                    _backClipDistance = backClipDistance;
                    _frontClipEnabled = frontClipEnabled;
                    _backClipEnabled = backClipEnabled;
                }

                public static ViewSnapshot Capture(ViewTableRecord view)
                {
                    return new ViewSnapshot(
                        view.Target,
                        view.CenterPoint,
                        view.Height,
                        view.Width,
                        view.FrontClipAtEye,
                        view.FrontClipDistance,
                        view.BackClipDistance,
                        view.FrontClipEnabled,
                        view.BackClipEnabled);
                }

                public void Apply(ViewTableRecord view)
                {
                    view.Target = _target;
                    view.CenterPoint = _centerPoint;
                    view.Height = _height;
                    view.Width = _width;
                    view.FrontClipAtEye = _frontClipAtEye;
                    view.FrontClipDistance = _frontClipDistance;
                    view.BackClipDistance = _backClipDistance;
                    view.FrontClipEnabled = _frontClipEnabled;
                    view.BackClipEnabled = _backClipEnabled;
                }
            }
        }
    }
}