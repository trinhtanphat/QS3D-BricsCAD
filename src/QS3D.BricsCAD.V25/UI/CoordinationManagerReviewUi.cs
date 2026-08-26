using System;
using System.Collections.Generic;
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
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Adds document-bound visual review actions to the persisted Coordination Manager.
    /// The modeless controller keeps only stable/portable identity between callbacks. Native
    /// Document/ObjectId wrappers are resolved for one validated action and are never retained.
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
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", parameterName);
            return value.Trim();
        }

        private static IntPtr GetNativeDatabaseIdentity(Document document)
        {
            if (document.IsDisposed)
                throw new InvalidOperationException("Coordination review requires a live BricsCAD document.");
            var database = document.Database;
            if (database == null)
                throw new InvalidOperationException("Coordination review requires a BricsCAD document database.");
            var identity = database.UnmanagedObject;
            if (identity == IntPtr.Zero)
                throw new InvalidOperationException("Coordination review requires a live native BricsCAD database.");
            return identity;
        }

        private sealed class Controller : IDisposable
        {
            private readonly CoordinationManagerWindow _window;
            private readonly IntPtr _nativeDatabaseIdentity;
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
            private bool _disposed;

            public Controller(
                CoordinationManagerWindow window,
                Document document,
                string projectId,
                string drawingFingerprint)
            {
                _window = window;
                _nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);
                _projectId = projectId;
                _drawingFingerprint = drawingFingerprint;
                _session = new TransientReviewSession();

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

            public void Attach()
            {
                _highlight.Click += OnHighlight;
                _clearHighlight.Click += OnClearHighlight;
                _isolate.Click += OnIsolate;
                _restoreIsolation.Click += OnRestoreIsolation;
                _section.Click += OnSection;
                _restoreView.Click += OnRestoreView;
                _grid.SelectionChanged += OnSelectionChanged;
                _window.Closed += OnWindowClosed;
                BcadApplication.DocumentManager.DocumentToBeDeactivated += OnDocumentToBeDeactivated;
                BcadApplication.DocumentManager.DocumentActivated += OnDocumentActivated;
                BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;
                UpdateActionState();
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
                RunValidated("Highlight", (document, ids, handles) => _session.Highlight(document, ids, handles));
            }

            private void OnClearHighlight(object sender, RoutedEventArgs e)
            {
                RunValidated("Clear Highlight", (document, ids, handles) => _session.ClearHighlight(document));
            }

            private void OnIsolate(object sender, RoutedEventArgs e)
            {
                RunValidated("Isolate", (document, ids, handles) => _session.Isolate(document, ids));
            }

            private void OnRestoreIsolation(object sender, RoutedEventArgs e)
            {
                RunValidated("Restore Isolation", (document, ids, handles) => _session.RestoreIsolation(document));
            }

            private void OnSection(object sender, RoutedEventArgs e)
            {
                RunValidated("Section / Focus", (document, ids, handles) => _session.ApplySectionFocus(document, ids));
            }

            private void OnRestoreView(object sender, RoutedEventArgs e)
            {
                RunValidated("Restore View", (document, ids, handles) => _session.RestoreSectionView(document));
            }

            private void RunValidated(
                string actionName,
                Action<Document, IReadOnlyList<ObjectId>, IReadOnlyList<string>> effect)
            {
                try
                {
                    // All canonical provenance/relink/full-pair checks complete before the supplied
                    // native CAD effect. Document/ObjectId values are method-local only.
                    var resolved = ResolveReviewTargets(out var document, out var handles);
                    effect(document, resolved, handles);
                    SetStatus(actionName + " • " + resolved.Count + " object(s) • validated full pair.");
                }
                catch (Exception ex)
                {
                    SetStatus(actionName + " bị từ chối: " + ex.Message);
                }
                finally
                {
                    UpdateActionState();
                }
            }

            private IReadOnlyList<ObjectId> ResolveReviewTargets(
                out Document document,
                out IReadOnlyList<string> handles)
            {
                var selected = _grid.SelectedItem as CoordinationManagerRow
                    ?? throw new InvalidOperationException("Hãy chọn một coordination issue trước.");
                if (!selected.CanLocate)
                    throw new InvalidOperationException("Issue hiện tại không actionable; CAD state không đổi.");

                var project = RequireCurrentProject(out document);
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

                handles = leftHandles.Concat(rightHandles)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList()
                    .AsReadOnly();
                var resolved = CadHandleService.Resolve(document, handles);
                if (resolved.Count != handles.Count)
                    throw new InvalidOperationException("Không resolve đủ toàn bộ source Handle hiện hành; CAD state không đổi.");

                return resolved.ToList().AsReadOnly();
            }

            private ProjectState RequireCurrentProject(out Document document)
            {
                document = BcadApplication.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("Không có active DWG để review.");
                if (!IsOriginDocument(document))
                    throw new InvalidOperationException("DWG đã đổi; review action không được phép tác động lên document khác.");

                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("QS3D project hiện hành không còn khả dụng.");

                if (!string.Equals(project.ProjectId, _projectId, StringComparison.Ordinal) ||
                    !string.Equals(project.DrawingFingerprint, _drawingFingerprint, StringComparison.Ordinal))
                    throw new InvalidOperationException("Project/Drawing Fingerprint đã đổi; review action bị fail-closed.");
                return project;
            }

            private bool TryResolveBoundDocument(out Document document)
            {
                document = null!;
                try
                {
                    foreach (Document candidate in BcadApplication.DocumentManager)
                    {
                        if (!IsOriginDocument(candidate)) continue;
                        document = candidate;
                        return true;
                    }
                }
                catch
                {
                    document = null!;
                }
                return false;
            }

            private bool TryResolveCurrentDocument(out Document document)
            {
                document = BcadApplication.DocumentManager.MdiActiveDocument;
                return document != null && IsOriginDocument(document);
            }

            private bool IsOriginDocument(Document document)
            {
                if (document == null || document.IsDisposed) return false;
                try
                {
                    var database = document.Database;
                    return database != null &&
                           database.UnmanagedObject != IntPtr.Zero &&
                           database.UnmanagedObject == _nativeDatabaseIdentity;
                }
                catch
                {
                    return false;
                }
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
                // A previous row must never leak presentation state into the next row.
                if (TryResolveCurrentDocument(out var document))
                    _session.ResetTransientStateBestEffort(document);
                else
                    _session.AbandonUnavailableDocumentState();
                SetStatus(string.Empty);
                UpdateActionState();
            }

            private void OnDocumentToBeDeactivated(object sender, DocumentCollectionEventArgs e)
            {
                if (_disposed || !IsOriginDocument(e.Document)) return;
                // This event fires while the bound DWG is still current, which is the safe point to
                // restore highlight/isolation/view state without touching the incoming document.
                _session.ResetTransientStateBestEffort(e.Document);
                if (_window.IsLoaded) _window.Close();
            }

            private void OnDocumentActivated(object sender, DocumentCollectionEventArgs e)
            {
                if (_disposed || IsOriginDocument(e.Document)) return;
                // The deactivation path normally performed cleanup. This fallback deliberately
                // abandons only managed state rather than mutating the newly active document.
                _session.AbandonUnavailableDocumentState();
                if (_window.IsLoaded) _window.Close();
            }

            private void OnDocumentToBeDestroyed(object sender, DocumentCollectionEventArgs e)
            {
                if (_disposed || !IsOriginDocument(e.Document)) return;
                _session.AbandonDestroyedDocumentState();
                if (_window.IsLoaded) _window.Close();
            }

            private void OnWindowClosed(object sender, EventArgs e)
            {
                Dispose();
            }

            private void UpdateActionState()
            {
                var row = _grid.SelectedItem as CoordinationManagerRow;
                var actionable = row != null && row.CanLocate && TryResolveCurrentDocument(out _);
                _highlight.IsEnabled = actionable;
                _isolate.IsEnabled = actionable;
                _section.IsEnabled = actionable;
                _clearHighlight.IsEnabled = actionable && _session.HasHighlight;
                _restoreIsolation.IsEnabled = actionable && _session.HasIsolation;
                _restoreView.IsEnabled = actionable && _session.HasSectionView;
            }

            private void SetStatus(string message)
            {
                _status.Text = message ?? string.Empty;
                if (_status.Text.Length == 0) return;
                try { PaletteCoordinator.SetStatus(_status.Text); } catch { }
                if (!TryResolveCurrentDocument(out var document)) return;
                try { document.Editor.WriteMessage("\nQS3D Coordination review: " + _status.Text); } catch { }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                BcadApplication.DocumentManager.DocumentToBeDeactivated -= OnDocumentToBeDeactivated;
                BcadApplication.DocumentManager.DocumentActivated -= OnDocumentActivated;
                BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;
                _grid.SelectionChanged -= OnSelectionChanged;
                _window.Closed -= OnWindowClosed;

                if (TryResolveCurrentDocument(out var document))
                    _session.ResetTransientStateBestEffort(document);
                else
                    _session.AbandonUnavailableDocumentState();
                _session.Dispose();
            }
        }

        private sealed class TransientReviewSession : IDisposable
        {
            private readonly List<string> _highlightedHandles = new List<string>();
            private bool _isolationActive;
            private object? _objectIsolationModeBefore;
            private ViewSnapshot? _viewBeforeSection;
            private bool _disposed;

            public bool HasHighlight => _highlightedHandles.Count > 0;
            public bool HasIsolation => _isolationActive;
            public bool HasSectionView => _viewBeforeSection != null;

            public void Highlight(
                Document document,
                IReadOnlyList<ObjectId> ids,
                IReadOnlyList<string> handles)
            {
                RequireTargets(ids);
                if (handles == null || handles.Count != ids.Count)
                    throw new InvalidOperationException("Validated review target identity is incomplete.");
                ClearHighlight(document);

                var appliedHandles = new List<string>();
                try
                {
                    using (document.LockDocument())
                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        for (var index = 0; index < ids.Count; index++)
                        {
                            var entity = transaction.GetObject(ids[index], OpenMode.ForRead, false) as Entity
                                ?? throw new InvalidOperationException("Resolved CAD object is not an Entity.");
                            entity.Highlight();
                            appliedHandles.Add(handles[index]);
                        }
                        transaction.Commit();
                    }
                    _highlightedHandles.AddRange(appliedHandles);
                }
                catch
                {
                    BestEffortUnhighlight(document, appliedHandles);
                    throw;
                }
            }

            public void ClearHighlight(Document document)
            {
                if (_highlightedHandles.Count == 0) return;
                var pending = _highlightedHandles.ToArray();
                _highlightedHandles.Clear();
                BestEffortUnhighlight(document, pending);
            }

            private static void BestEffortUnhighlight(Document document, IReadOnlyList<string> handles)
            {
                if (handles == null || handles.Count == 0) return;
                IReadOnlyList<ObjectId> resolved;
                try
                {
                    resolved = CadHandleService.Resolve(document, handles).ToList().AsReadOnly();
                }
                catch
                {
                    return;
                }

                try
                {
                    using (document.LockDocument())
                    using (var transaction = document.Database.TransactionManager.StartTransaction())
                    {
                        foreach (var id in resolved)
                        {
                            try
                            {
                                var entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                                entity?.Unhighlight();
                            }
                            catch
                            {
                                // Cleanup remains best effort for erased/closed transient targets.
                            }
                        }
                        transaction.Commit();
                    }
                }
                catch
                {
                    // Do not turn transient cleanup into a cross-document or shutdown failure.
                }
            }

            public void Isolate(Document document, IReadOnlyList<ObjectId> ids)
            {
                RequireTargets(ids);
                if (_isolationActive) RestoreIsolation(document);

                _objectIsolationModeBefore = BcadApplication.GetSystemVariable("OBJECTISOLATIONMODE");
                BcadApplication.SetSystemVariable("OBJECTISOLATIONMODE", 0);
                document.Editor.SetImpliedSelection(ids.ToArray());
                document.SendStringToExecute("_.ISOLATEOBJECTS ", true, false, false);
                _isolationActive = true;
            }

            public void RestoreIsolation(Document document)
            {
                if (!_isolationActive) return;
                try
                {
                    document.SendStringToExecute("_.UNISOLATEOBJECTS ", true, false, false);
                }
                finally
                {
                    _isolationActive = false;
                    RestoreObjectIsolationModeBestEffort(document);
                }
            }

            public void ApplySectionFocus(Document document, IReadOnlyList<ObjectId> ids)
            {
                RequireTargets(ids);
                RestoreSectionView(document);

                var bounds = ReadBounds(document, ids);
                var center = new Point3d(
                    (bounds.MinPoint.X + bounds.MaxPoint.X) * 0.5,
                    (bounds.MinPoint.Y + bounds.MaxPoint.Y) * 0.5,
                    (bounds.MinPoint.Z + bounds.MaxPoint.Z) * 0.5);
                var diagonal = (bounds.MaxPoint - bounds.MinPoint).Length;
                if (!(diagonal > 1e-9) || double.IsNaN(diagonal) || double.IsInfinity(diagonal))
                    throw new InvalidOperationException("Không thể tạo section/focus từ extents suy biến.");

                using (var view = document.Editor.GetCurrentView())
                {
                    _viewBeforeSection = ViewSnapshot.Capture(view);
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
                    document.Editor.SetCurrentView(view);
                }
            }

            public void RestoreSectionView(Document document)
            {
                if (_viewBeforeSection == null) return;
                var snapshot = _viewBeforeSection;
                _viewBeforeSection = null;

                using (var view = document.Editor.GetCurrentView())
                {
                    snapshot.Apply(view);
                    document.Editor.SetCurrentView(view);
                }
            }

            private static Extents3d ReadBounds(Document document, IReadOnlyList<ObjectId> ids)
            {
                var hasBounds = false;
                var min = new Point3d();
                var max = new Point3d();
                using (document.LockDocument())
                using (var transaction = document.Database.TransactionManager.StartTransaction())
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

            public void ResetTransientStateBestEffort(Document document)
            {
                try { ClearHighlight(document); } catch { _highlightedHandles.Clear(); }
                try { RestoreIsolation(document); }
                catch
                {
                    _isolationActive = false;
                    RestoreObjectIsolationModeBestEffort(document);
                }
                try { RestoreSectionView(document); } catch { _viewBeforeSection = null; }
            }

            public void AbandonUnavailableDocumentState()
            {
                _highlightedHandles.Clear();
                _isolationActive = false;
                _objectIsolationModeBefore = null;
                _viewBeforeSection = null;
            }

            public void AbandonDestroyedDocumentState()
            {
                AbandonUnavailableDocumentState();
            }

            private void RestoreObjectIsolationModeBestEffort(Document document)
            {
                if (_objectIsolationModeBefore == null) return;
                var value = _objectIsolationModeBefore;
                _objectIsolationModeBefore = null;
                if (!ReferenceEquals(BcadApplication.DocumentManager.MdiActiveDocument, document)) return;
                try { BcadApplication.SetSystemVariable("OBJECTISOLATIONMODE", value); } catch { }
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                // Controller owns the final attempt to resolve the bound live Document. Session
                // disposal itself is intentionally managed-only so shutdown cannot dereference a
                // stale native wrapper or mutate another active drawing.
                AbandonUnavailableDocumentState();
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
