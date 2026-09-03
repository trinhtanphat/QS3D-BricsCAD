using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Cad;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using Teigha.Geometry;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Production repeated Direct Draw for linear Wall/Beam authoring. DrawJig owns transient
    /// preview only; every accepted segment is committed through DirectDrawCommands.ExecuteDirect
    /// so source, semantic ownership, regeneration, native Solid3d and rollback stay canonical.
    /// Per-segment checkpoints stay inside one native command group, so one Undo/Redo covers the
    /// accepted set while a document-switch suspension cannot strand semantic/native state.
    /// </summary>
    public sealed class DirectDrawRepeatedCommands
    {
        private const string ResultSchema = "QS3D_DIRECT_DRAW_REPEAT_V1";

        // Runtime qualification observers are internal, optional and exception-isolated. They do
        // not own command input or mutation; production Direct Draw remains the only authoring path.
        internal static event Action<Document, int>? SegmentCommittedForRuntimeQualification;
        internal static event Action<Document, int, string>? SequenceCompletedForRuntimeQualification;

        [CommandMethod("QS3DDRAWWALLREPEAT", CommandFlags.Modal)]
        public void DrawWallRepeated() =>
            Run(ElementCategory.ArchitecturalWall, "Tường liên tục", null, null);

        [CommandMethod("QS3DDRAWBEAMREPEAT", CommandFlags.Modal)]
        public void DrawBeamRepeated() =>
            Run(ElementCategory.Beam, "Dầm liên tục", null, null);

        internal void DrawActiveFamilyRepeated(
            ElementCategory category,
            string expectedProjectId,
            string expectedFamilyId)
        {
            Run(category, "Active Family liên tục", expectedProjectId, expectedFamilyId);
        }

        private static void Run(
            ElementCategory category,
            string label,
            string? expectedProjectId,
            string? expectedFamilyId)
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                RunCore(document, category, label, expectedProjectId, expectedFamilyId);
            }
            catch (Exception)
            {
                Report(document, label + ": không thể hoàn tất thao tác. Vui lòng thử lại.");
            }
        }

        private static void RunCore(
            Document document,
            ElementCategory category,
            string label,
            string? expectedProjectId,
            string? expectedFamilyId)
        {
            if (category != ElementCategory.ArchitecturalWall && category != ElementCategory.Beam)
                throw new InvalidOperationException(
                    "Repeated Direct Draw hiện chỉ hỗ trợ ArchitecturalWall và Beam.");

            DirectDrawCommands.RequireRepeatedModelSpace(document);
            var editor = document.Editor;
            var commandUnit = (object)CadUnitService.GetLengthUnit(document);
            var commandUcs = editor.CurrentUserCoordinateSystem;
            var initialPreview = DirectDrawProjectPreviewContext.Capture(document);
            var currentPreview = initialPreview;
            RequireExpectedFamily(initialPreview, category, expectedProjectId, expectedFamilyId, label);
            using var lifecycleGuard = new RepeatedDocumentLifecycleGuard(document);

            var firstOptions = new PromptPointOptions(
                "\n" + label + " - chọn điểm đầu (Enter/ESC để thoát): ")
            {
                AllowNone = true
            };
            var first = editor.GetPoint(firstOptions);
            if (first.Status != PromptStatus.OK)
            {
                WriteResult(
                    editor,
                    category,
                    0,
                    first.Status == PromptStatus.None ? "ENTER_BEFORE_START" : "ESC_OR_CANCEL_BEFORE_START");
                return;
            }

            DirectDrawCommands.RequireRepeatedPromptContextUnchanged(
                document, commandUnit, commandUcs, label + " / điểm đầu");
            // Editor.GetPoint reports the first point in current UCS; DrawJig uses WCS.
            var startWcs = first.Value.TransformBy(commandUcs);
            var committed = new List<DirectDrawCommitResult>();
            ProjectState? trackedProject = null;
            ProjectStateSnapshot? commandBefore = null;
            var commandBeforeStamp = default(SourceReconcileUndoCoordinator.ProjectRevisionStamp);
            var projectExistedBeforeCommand = initialPreview.HasProject;
            var accepted = 0;
            var checkpointed = 0;
            var termination = "UNKNOWN";
            Exception? deferredSegmentError = null;

            using (SourceReconcileUndoCoordinator.BeginExternalTransitionScope(document))
            {
                while (true)
                {
                    try
                    {
                        if (lifecycleGuard.WasDeactivated)
                        {
                            termination = "DOCUMENT_SWITCH";
                            break;
                        }
                        DirectDrawCommands.RequireRepeatedPromptContextUnchanged(
                            document, commandUnit, commandUcs, label + " / trước preview");
                        // The first segment must resolve against the preview captured before the
                        // first point prompt. This rejects a project that appears/replaces state
                        // while that prompt is open. Only a successfully checkpointed segment may
                        // advance the preview to the new canonical ChangeVersion.
                        var preview = currentPreview;
                        RequireExpectedFamily(
                            preview, category, expectedProjectId, expectedFamilyId, label);
                        var defaults = RepeatedDefaults.Resolve(preview, category);
                        var stripWidth = CadGeometryGuard.Positive(
                            CadGeometryGuard.ToDrawingUnits(
                                document, defaults.ProfileWidthM, label + " preview width"),
                            label + " preview width drawing units");

                        var jig = new DirectDrawProfileStripJig(
                            startWcs,
                            stripWidth,
                            commandUcs,
                            "\n" + label + " - chọn điểm tiếp theo (Enter/ESC để kết thúc): ");
                        var drag = editor.Drag(jig);
                        if (lifecycleGuard.WasDeactivated)
                        {
                            termination = "DOCUMENT_SWITCH";
                            break;
                        }
                        DirectDrawCommands.RequireRepeatedPromptContextUnchanged(
                            document, commandUnit, commandUcs, label + " / sau preview");

                        if (drag.Status != PromptStatus.OK)
                        {
                            termination = jig.LastPromptStatus == PromptStatus.None
                                ? "ENTER"
                                : "ESC_OR_CANCEL";
                            break;
                        }
                        if (!jig.HasUsableEndPoint)
                        {
                            editor.WriteMessage(
                                "\nQS3D " + label + ": điểm trùng điểm đầu segment; chọn lại hoặc Enter/ESC để kết thúc.");
                            continue;
                        }

                        var endWcs = jig.EndPoint;
                        var result = DirectDrawCommands.ExecuteDirect(
                            document,
                            category,
                            () => DirectDrawCommands.CreateLineWcs(document, startWcs, endWcs),
                            element => defaults.Apply(element, category),
                            preview,
                            project =>
                            {
                                if (trackedProject == null)
                                {
                                    trackedProject = project;
                                    commandBefore = ProjectStateSnapshot.Capture(project);
                                    commandBeforeStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);
                                    return;
                                }
                                if (!ReferenceEquals(trackedProject, project))
                                    throw new InvalidOperationException(
                                        "Repeated Direct Draw canonical project changed between accepted segments.");
                            });

                        committed.Add(result);
                        accepted++;
                        startWcs = endWcs;
                        if (trackedProject == null || commandBefore == null)
                            throw new InvalidOperationException(
                                "Repeated Direct Draw accepted CAD without a command-level semantic snapshot.");
                        try
                        {
                            // Publish a whole-command checkpoint after every accepted segment.
                            // All checkpoints remain in this one native command group, so Undo/Redo
                            // still traverses the original before-state and latest accepted state as
                            // one operation. The latest checkpoint also keeps semantic/native state
                            // coherent if BricsCAD suspends this document-context command on a DWG switch.
                            if (checkpointed == 0)
                            {
                                SourceReconcileUndoCoordinator.CommitExternalTransition(
                                    document,
                                    trackedProject,
                                    commandBefore,
                                    commandBeforeStamp);
                            }
                            else
                            {
                                SourceReconcileUndoCoordinator.UpdateExternalTransitionCheckpoint(
                                    document,
                                    trackedProject);
                            }
                            checkpointed = accepted;
                        }
                        catch (Exception transitionError)
                        {
                            throw RollbackWholeCommand(
                                document,
                                trackedProject,
                                commandBefore,
                                projectExistedBeforeCommand,
                                committed,
                                transitionError);
                        }
                        currentPreview = DirectDrawProjectPreviewContext.Capture(document);
                        if (!currentPreview.HasProject ||
                            !ReferenceEquals(currentPreview.DefaultsProject, trackedProject))
                            throw new InvalidOperationException(
                                "Repeated Direct Draw canonical project changed after an accepted segment.");
                        NotifySegmentCommitted(document, accepted);
                        editor.WriteMessage(
                            "\nQS3D " + label + " đã commit segment #" + accepted +
                            ". Chọn endpoint tiếp theo; Enter/ESC kết thúc và giữ các segment đã commit.");
                        if (lifecycleGuard.WasDeactivated)
                        {
                            termination = "DOCUMENT_SWITCH";
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (ex is RepeatedWholeCommandRollbackException) throw;
                        deferredSegmentError = ex;
                        termination = "SEGMENT_ERROR";
                        break;
                    }
                }

                if (accepted > 0)
                {
                    if (trackedProject == null || commandBefore == null || checkpointed != accepted)
                        throw new InvalidOperationException(
                            "Repeated Direct Draw accepted CAD without a matching command-level Undo checkpoint.");
                }
            }

            WriteResult(editor, category, accepted, termination);
            NotifySequenceCompleted(document, accepted, termination);
            if (deferredSegmentError != null)
                Report(
                    document,
                    label + " dừng sau " + accepted + " segment đã commit; các segment đã commit vẫn được giữ. Vui lòng thử segment tiếp theo bằng lệnh mới.");
        }

        private static RepeatedWholeCommandRollbackException RollbackWholeCommand(
            Document document,
            ProjectState project,
            ProjectStateSnapshot commandBefore,
            bool projectExistedBeforeCommand,
            IReadOnlyList<DirectDrawCommitResult> committed,
            Exception transitionError)
        {
            var errors = new List<Exception> { transitionError };
            for (var index = committed.Count - 1; index >= 0; index--)
            {
                var item = committed[index];
                try
                {
                    DirectDrawCommands.EraseRepeatedDirectDrawCad(
                        document,
                        project,
                        item.Element,
                        item.SourceId,
                        item.GeneratedHandles);
                }
                catch (Exception cleanupError)
                {
                    errors.Add(cleanupError);
                }
            }

            try { commandBefore.Restore(project); }
            catch (Exception restoreError) { errors.Add(restoreError); }
            try { SourceReconcileUndoCoordinator.Forget(document); }
            catch (Exception historyError) { errors.Add(historyError); }
            if (!projectExistedBeforeCommand)
            {
                try { ProjectContextCoordinator.Forget(document); }
                catch (Exception forgetError) { errors.Add(forgetError); }
            }
            try { document.Editor.SetImpliedSelection(Array.Empty<Teigha.DatabaseServices.ObjectId>()); }
            catch { }

            return new RepeatedWholeCommandRollbackException(
                "Repeated Direct Draw could not register command-level native Undo; all accepted segments were rolled back.",
                new AggregateException(errors));
        }

        private sealed class RepeatedDocumentLifecycleGuard : IDisposable
        {
            private readonly Document _document;
            private readonly DocumentCollection _documents;
            private bool _disposed;
            private bool _wasDeactivated;

            public RepeatedDocumentLifecycleGuard(Document document)
            {
                _document = document ?? throw new ArgumentNullException(nameof(document));
                _documents = Application.DocumentManager;
                _documents.DocumentToBeDeactivated += OnDocumentToBeDeactivated;
            }

            public bool WasDeactivated => _wasDeactivated;

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                try { _documents.DocumentToBeDeactivated -= OnDocumentToBeDeactivated; }
                catch { }
            }

            private void OnDocumentToBeDeactivated(object sender, DocumentCollectionEventArgs args)
            {
                if (args != null && EqualityComparer<Document>.Default.Equals(args.Document, _document))
                    _wasDeactivated = true;
            }
        }

        private sealed class RepeatedWholeCommandRollbackException : InvalidOperationException
        {
            public RepeatedWholeCommandRollbackException(string message, Exception innerException)
                : base(message, innerException)
            {
            }
        }

        private static void RequireExpectedFamily(
            DirectDrawProjectPreviewContext preview,
            ElementCategory category,
            string? expectedProjectId,
            string? expectedFamilyId,
            string operation)
        {
            if (string.IsNullOrWhiteSpace(expectedProjectId) && string.IsNullOrWhiteSpace(expectedFamilyId))
                return;
            if (!preview.HasProject || preview.DefaultsProject == null)
                throw new InvalidOperationException(operation + ": Active Family project is no longer available.");
            var project = preview.DefaultsProject;
            if (!string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(operation + ": active project changed during repeated drawing.");
            var active = ProjectFamilyActivationService.GetActive(project);
            if (active == null ||
                active.Category != category ||
                !string.Equals(active.Id, expectedFamilyId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(operation + ": Active Family changed during repeated drawing.");
        }

        private static void WriteResult(
            Editor editor,
            ElementCategory category,
            int accepted,
            string termination)
        {
            editor.WriteMessage(
                "\n" + ResultSchema +
                "|category=" + category +
                "|accepted_segments=" + accepted.ToString(CultureInfo.InvariantCulture) +
                "|termination=" + termination +
                "|preview=DrawJigProfileStrip" +
                "|source_model=CanonicalLine" +
                "|semantic_model=CanonicalProject" +
                "|native_model=CanonicalBuilder" +
                "|undo_scope=WholeCommand");
        }

        private static void NotifySegmentCommitted(Document document, int accepted)
        {
            var observer = SegmentCommittedForRuntimeQualification;
            if (observer == null) return;
            try { observer(document, accepted); }
            catch { }
        }

        private static void NotifySequenceCompleted(Document document, int accepted, string termination)
        {
            var observer = SequenceCompletedForRuntimeQualification;
            if (observer == null) return;
            try { observer(document, accepted, termination); }
            catch { }
        }

        private static void Report(Document document, string message)
        {
            try { document.Editor.WriteMessage("\nQS3D " + message); } catch { }
            try { PaletteCoordinator.SetStatus(message); } catch { }
        }

        private sealed class RepeatedDefaults
        {
            private RepeatedDefaults(double profileWidthM, double heightM, double bottomOffsetM)
            {
                ProfileWidthM = profileWidthM;
                HeightM = heightM;
                BottomOffsetM = bottomOffsetM;
            }

            internal double ProfileWidthM { get; }
            internal double HeightM { get; }
            internal double BottomOffsetM { get; }

            internal static RepeatedDefaults Resolve(
                DirectDrawProjectPreviewContext preview,
                ElementCategory category)
            {
                var project = preview.DefaultsProject;
                if (category == ElementCategory.ArchitecturalWall)
                {
                    return new RepeatedDefaults(
                        project == null ? 0.2d : DirectDrawCommands.FamilyNumber(project, category, "ThicknessM", 0.2d),
                        project == null ? 3.6d : DirectDrawCommands.FamilyNumber(project, category, "HeightM", 3.6d),
                        project == null ? 0d : DirectDrawCommands.FamilyFiniteNumber(project, category, "BottomOffsetM", 0d));
                }
                return new RepeatedDefaults(
                    project == null ? 0.3d : DirectDrawCommands.FamilyNumber(project, category, "WidthM", 0.3d),
                    project == null ? 0.5d : DirectDrawCommands.FamilyNumber(project, category, "HeightM", 0.5d),
                    project == null ? 0d : DirectDrawCommands.FamilyFiniteNumber(project, category, "BottomOffsetM", 0d));
            }

            internal void Apply(ProjectElement element, ElementCategory category)
            {
                var widthKey = category == ElementCategory.ArchitecturalWall ? "ThicknessM" : "WidthM";
                element.SetProperty(widthKey, ProfileWidthM.ToString("R", CultureInfo.InvariantCulture));
                element.SetProperty("HeightM", HeightM.ToString("R", CultureInfo.InvariantCulture));
                element.SetProperty("BottomOffsetM", BottomOffsetM.ToString("R", CultureInfo.InvariantCulture));
            }
        }
    }
}
