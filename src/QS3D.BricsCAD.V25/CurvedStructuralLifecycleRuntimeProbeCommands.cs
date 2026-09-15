using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.Cad;
using QS3D.Core.Domain;
using QS3D.Core.Persistence;
using QS3D.Core.Units;
using Teigha.DatabaseServices;
using Teigha.Runtime;
using BcadApplication = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    public sealed partial class CurvedStructuralRuntimeProbeCommands
    {
        private const string LifeResultVariable = "QS3D_CURVED_LIFECYCLE_RESULT";
        private const string LifePhaseVariable = "QS3D_CURVED_LIFECYCLE_PHASE_RESULT";
        private const string LifeNonceVariable = "QS3D_CURVED_LIFECYCLE_NONCE";
        private const string LifeSourceShaVariable = "QS3D_CURVED_LIFECYCLE_SOURCE_SHA";
        private const string LifeDrawingAVariable = "QS3D_CURVED_LIFECYCLE_DWG_A";
        private const string LifeDrawingBVariable = "QS3D_CURVED_LIFECYCLE_DWG_B";
        private const string LifeExpectedReopenFingerprintVariable = "QS3D_CURVED_LIFECYCLE_EXPECTED_REOPEN_FINGERPRINT";
        private const string LifeResultFile = "curved-structural-lifecycle-result.txt";
        private const string LifePhaseFile = "curved-structural-lifecycle-session1.txt";
        private const string LifeSchema = "QS3D_CURVED_STRUCTURAL_LIFECYCLE_RUNTIME_V1";
        private static readonly string[] LifeElementIds = { "beam_arc", "beam_circle", "beam_polyline_curved", "slab_circle" };
        private static LifeSessionOne? _lifeSessionOne;
        private static LifeSessionTwo? _lifeSessionTwo;
        private static LifeDocumentState? _lifeDocumentB;

        [CommandMethod("QS3DCURVEDLIFEPREPARE", CommandFlags.Modal)]
        public void CurvedLifePrepare() => LifeRun("prepare", () =>
        {
            var context = LifeContext(false);
            LifeRequirePath(context.Document.Name, context.DrawingA, "A");
            var project = ProjectContextCoordinator.GetOrCreate(context.Document);
            LifeRequire(project.Elements.Count == 0, "drawing A project must be pristine");
            var state = LifeSeedDocument(context.Document, project);
            _lifeSessionOne = new LifeSessionOne(context.Document, project, context.Nonce, state.ElementIds,
                ProjectPersistenceCheckpoint.Capture(project, state.ElementIds));
            _lifeSessionTwo = null;
            _lifeDocumentB = null;
        });

        [CommandMethod("QS3DCURVEDLIFESELECTBEAMS", CommandFlags.Modal)]
        public void CurvedLifeSelectBeams() => LifeRun("select_beams", () => LifeSelect(false));

        [CommandMethod("QS3DCURVEDLIFESELECTSLAB", CommandFlags.Modal)]
        public void CurvedLifeSelectSlab() => LifeRun("select_slab", () => LifeSelect(true));

        [CommandMethod("QS3DCURVEDLIFECAPTUREBASELINE", CommandFlags.Modal)]
        public void CurvedLifeCaptureBaseline() => LifeRun("baseline", () =>
        {
            var context = LifeContext(true);
            var state = LifeRequireSessionOne(context);
            state.AfterHandles = LifeGeneratedHandles(context.Document, context.Project, state.ElementIds);
            state.After = ProjectPersistenceCheckpoint.Capture(context.Project, state.ElementIds);
        });

        [CommandMethod("QS3DCURVEDLIFECHECKUNDO", CommandFlags.Modal)]
        public void CurvedLifeCheckUndo() => LifeRun("native_undo", () =>
        {
            var context = LifeContext(true);
            var state = LifeRequireSessionOne(context);
            LifeRequire(state.After != null && state.AfterHandles.Count == LifeElementIds.Length, "baseline missing");
            state.UndoGeneratedAbsent = LifeAllAbsent(context.Document, state.AfterHandles);
            state.UndoCoherent = state.Before.Matches(context.Project) && state.UndoGeneratedAbsent;
        });

        [CommandMethod("QS3DCURVEDLIFECAPTUREREDO", CommandFlags.Modal)]
        public void CurvedLifeCaptureRedo() => LifeRun("native_redo_capture", () =>
        {
            var context = LifeContext(true);
            var state = LifeRequireSessionOne(context);
            state.RedoHandles = LifeGeneratedHandles(context.Document, context.Project, state.ElementIds);
        });

        [CommandMethod("QS3DCURVEDLIFECHECKREDO", CommandFlags.Modal)]
        public void CurvedLifeCheckRedo() => LifeRun("native_redo", () =>
        {
            var context = LifeContext(true);
            var state = LifeRequireSessionOne(context);
            var after = state.After ?? throw new InvalidOperationException("baseline checkpoint missing");
            state.RedoCoherent = after.Matches(context.Project) &&
                                 state.RedoHandles.Count == state.AfterHandles.Count &&
                                 state.RedoHandles.SequenceEqual(state.AfterHandles, StringComparer.OrdinalIgnoreCase) &&
                                 LifeAllPresent(context.Document, state.RedoHandles);
        });

        [CommandMethod("QS3DCURVEDLIFESESSION1", CommandFlags.Modal)]
        public void CurvedLifeSessionOne() => LifeRun("session1", () =>
        {
            var context = LifeContext(true);
            var state = LifeRequireSessionOne(context);
            LifeRequire(state.UndoCoherent && state.RedoCoherent, "Undo/Redo lifecycle is not coherent");
            ProjectContextCoordinator.Save(context.Document);
            var savedCheckpoint = ProjectPersistenceCheckpoint.Capture(context.Project, state.ElementIds);
            LifeRequire(savedCheckpoint.Matches(context.Project), "saved persistence checkpoint is unstable");
            var reopenFingerprint = LifeStateFingerprint(context.Project, state.ElementIds);
            LifeWriteMarker(context.PhasePath, new[]
            {
                "status=PASS", "command=QS3DCURVEDLIFESESSION1", "nonce=" + context.Nonce,
                "schema=" + LifeSchema, "qualification_boundary=LOCAL_003_CURVED_LIFECYCLE_ONLY",
                "production_local003_qualified=false", "undo_coherent=true", "redo_coherent=true",
                "undo_generated_absent=true", "saved_checkpoint_matches=true",
                "reopen_fingerprint=" + reopenFingerprint,
                "generated_count=" + state.AfterHandles.Count.ToString(CultureInfo.InvariantCulture)
            });
        });

        [CommandMethod("QS3DCURVEDLIFEREOPEN", CommandFlags.Modal)]
        public void CurvedLifeReopen() => LifeRun("cold_reopen", () =>
        {
            var context = LifeContext(true);
            LifeRequirePath(context.Document.Name, context.DrawingA, "A");
            var handles = LifeGeneratedHandles(context.Document, context.Project, LifeElementIds);
            var reopenedCheckpoint = ProjectPersistenceCheckpoint.Capture(context.Project, LifeElementIds);
            LifeRequire(reopenedCheckpoint.Matches(context.Project), "cold reopen persistence checkpoint is unstable");
            var expectedFingerprint = LifeExpectedReopenFingerprint();
            var actualFingerprint = LifeStateFingerprint(context.Project, LifeElementIds);
            var reopenCoherent = string.Equals(expectedFingerprint, actualFingerprint, StringComparison.Ordinal);
            LifeRequire(reopenCoherent, "cold reopen canonical state fingerprint mismatch");
            _lifeSessionTwo = new LifeSessionTwo(context.Document, context.Project, context.Nonce, LifeElementIds,
                handles, reopenedCheckpoint, reopenCoherent);
            _lifeSessionOne = null;
        });

        [CommandMethod("QS3DCURVEDLIFEAFTERREBUILD", CommandFlags.Modal)]
        public void CurvedLifeAfterRebuild() => LifeRun("rebuild", () =>
        {
            var context = LifeContext(true);
            var state = LifeRequireSessionTwo(context);
            var rebuilt = LifeGeneratedHandles(context.Document, context.Project, state.ElementIds);
            state.OldGeneratedRemoved = LifeAllAbsent(context.Document, state.ReopenedHandles);
            state.NewGeneratedDisjoint = state.ReopenedHandles.All(x => !rebuilt.Contains(x, StringComparer.OrdinalIgnoreCase));
            state.RebuildCountsStable = rebuilt.Count == state.ReopenedHandles.Count;
            state.RebuiltHandles = rebuilt;
            state.Rebuilt = ProjectPersistenceCheckpoint.Capture(context.Project, state.ElementIds);
            ProjectContextCoordinator.Save(context.Document);
        });

        [CommandMethod("QS3DCURVEDLIFEPREPAREB", CommandFlags.Modal)]
        public void CurvedLifePrepareB() => LifeRun("prepare_b", () =>
        {
            var context = LifeContext(false);
            LifeRequirePath(context.Document.Name, context.DrawingB, "B");
            var project = ProjectContextCoordinator.GetOrCreate(context.Document);
            LifeRequire(project.Elements.Count == 0, "drawing B project must be pristine");
            var seeded = LifeSeedDocument(context.Document, project);
            _lifeDocumentB = new LifeDocumentState(context.Document, project, seeded.ElementIds, new List<string>(),
                ProjectPersistenceCheckpoint.Capture(project, seeded.ElementIds));
        });

        [CommandMethod("QS3DCURVEDLIFECAPTUREB", CommandFlags.Modal)]
        public void CurvedLifeCaptureB() => LifeRun("capture_b", () =>
        {
            var context = LifeContext(true);
            LifeRequirePath(context.Document.Name, context.DrawingB, "B");
            var state = _lifeDocumentB ?? throw new InvalidOperationException("Drawing B lifecycle state is missing.");
            state.GeneratedHandles = LifeGeneratedHandles(context.Document, context.Project, state.ElementIds);
            state.Checkpoint = ProjectPersistenceCheckpoint.Capture(context.Project, state.ElementIds);
        });

        [CommandMethod("QS3DCURVEDLIFEACTIVATEA", CommandFlags.Modal)]
        public void CurvedLifeActivateA() => LifeRun("activate_a", () =>
        {
            var context = LifeContext(false);
            BcadApplication.DocumentManager.MdiActiveDocument = LifeFindDocument(context.DrawingA);
        });

        [CommandMethod("QS3DCURVEDLIFECHECKA", CommandFlags.Modal)]
        public void CurvedLifeCheckA() => LifeRun("check_a", () =>
        {
            var context = LifeContext(true);
            LifeRequirePath(context.Document.Name, context.DrawingA, "A");
            var state = LifeRequireSessionTwo(context);
            var rebuilt = state.Rebuilt ?? throw new InvalidOperationException("drawing A rebuilt checkpoint is missing.");
            LifeRequire(rebuilt.Matches(context.Project), "drawing A changed while B was active");
            LifeRequire(LifeAllPresent(context.Document, state.RebuiltHandles), "drawing A generated solids changed while B was active");
            state.AUnchangedAfterB = true;
        });

        [CommandMethod("QS3DCURVEDLIFEACTIVATEB", CommandFlags.Modal)]
        public void CurvedLifeActivateB() => LifeRun("activate_b", () =>
        {
            var context = LifeContext(false);
            BcadApplication.DocumentManager.MdiActiveDocument = LifeFindDocument(context.DrawingB);
        });

        [CommandMethod("QS3DCURVEDLIFECOMPLETE", CommandFlags.Modal)]
        public void CurvedLifeComplete() => LifeRun("final", () =>
        {
            var context = LifeContext(true);
            LifeRequirePath(context.Document.Name, context.DrawingB, "B");
            var a = _lifeSessionTwo ?? throw new InvalidOperationException("Drawing A lifecycle state is missing.");
            var b = _lifeDocumentB ?? throw new InvalidOperationException("Drawing B lifecycle state is missing.");
            var bStable = b.Checkpoint.Matches(context.Project) && LifeAllPresent(context.Document, b.GeneratedHandles);
            var isolated = a.AUnchangedAfterB && bStable && !ReferenceEquals(a.Project, b.Project);
            var pass = a.ReopenCoherent && a.OldGeneratedRemoved && a.NewGeneratedDisjoint && a.RebuildCountsStable && isolated;
            LifeWriteMarker(context.ResultPath, new[]
            {
                "status=" + (pass ? "PASS" : "FAIL"), "command=QS3DCURVEDLIFECOMPLETE", "nonce=" + context.Nonce,
                "schema=" + LifeSchema, "qualification_boundary=LOCAL_003_CURVED_LIFECYCLE_ONLY",
                "production_local003_qualified=false", "reopen_coherent=" + LifeBool(a.ReopenCoherent), "rebuild_coherent=" + LifeBool(a.RebuildCountsStable),
                "old_generated_removed=" + LifeBool(a.OldGeneratedRemoved), "new_generated_disjoint=" + LifeBool(a.NewGeneratedDisjoint),
                "rebuild_counts_stable=" + LifeBool(a.RebuildCountsStable), "multi_dwg_isolated=" + LifeBool(isolated),
                "drawing_a_unchanged=" + LifeBool(a.AUnchangedAfterB), "drawing_b_unchanged=" + LifeBool(bStable),
                "generated_count=" + a.RebuiltHandles.Count.ToString(CultureInfo.InvariantCulture),
                "error_code=" + (pass ? "NONE" : "CURVED_LIFECYCLE_STATE_REJECTED")
            });
        });

        private static void LifeSelect(bool slab)
        {
            var context = LifeContext(true);
            IReadOnlyList<string> ids;
            if (LifeSamePath(context.Document.Name, context.DrawingB) && _lifeDocumentB != null) ids = _lifeDocumentB.ElementIds;
            else if (_lifeSessionTwo != null && LifeSamePath(context.Document.Name, context.DrawingA)) ids = _lifeSessionTwo.ElementIds;
            else ids = LifeRequireSessionOne(context).ElementIds;
            var selected = ids.Where(id => slab == string.Equals(id, "slab_circle", StringComparison.OrdinalIgnoreCase)).ToList();
            var sourceHandles = selected.Select(id => context.Project.FindElement(id)?.SourceHandles.SingleOrDefault() ?? string.Empty).ToList();
            LifeRequire(sourceHandles.All(x => x.Length > 0), "source selection is incomplete");
            var objectIds = CadHandleService.Resolve(context.Document, sourceHandles);
            LifeRequire(objectIds.Count == sourceHandles.Count, "source selection did not resolve");
            context.Document.Editor.SetImpliedSelection(objectIds.ToArray());
        }

        private static LifeDocumentState LifeSeedDocument(Document document, ProjectState project)
        {
            if (project.Floors.All(x => !string.Equals(x.Id, "L0", StringComparison.OrdinalIgnoreCase)))
                project.Floors.Add(new FloorDefinition("L0", "Level 0", 0d));
            project.ActiveFloorId = "L0";
            if (!CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                throw new InvalidOperationException("Curved lifecycle requires a supported native drawing unit.");
            DrawingUnitResolutionPolicy.BindQuantityUnit(project.Metadata, false, nativeUnit, DrawingUnitResolutionSource.NativeInsunits);
            var sources = CreateSources(document);
            var selected = sources.Positive.Where(x => LifeElementIds.Contains(x.Name, StringComparer.Ordinal)).ToList();
            LifeRequire(selected.Count == LifeElementIds.Length, "curved lifecycle fixture selection is incomplete");
            foreach (var source in selected)
            {
                var element = AddElement(project, source.Name, source.Category, source);
                Configure(element, source.Category, source.HeightM);
                element.MarkClean(ElementDirtyFlags.All);
            }
            project.Touch();
            var ids = selected.Select(x => x.Name).ToArray();
            return new LifeDocumentState(document, project, ids, new List<string>(), ProjectPersistenceCheckpoint.Capture(project, ids));
        }

        private static List<string> LifeGeneratedHandles(Document document, ProjectState project, IReadOnlyList<string> elementIds)
        {
            var handles = elementIds.Select(id => project.FindElement(id) ?? throw new InvalidOperationException("Missing lifecycle element: " + id))
                .Select(x => Property(x, "GeneratedSolidHandle")).ToList();
            LifeRequire(handles.Count == elementIds.Count && handles.All(x => x.Length > 0), "generated ownership is incomplete");
            LifeRequire(CadHandleService.Resolve(document, handles).Count == handles.Count, "generated solids are not all live");
            return handles;
        }

        private static bool LifeAllPresent(Document document, IReadOnlyCollection<string> handles) =>
            handles.Count > 0 && CadHandleService.Resolve(document, handles).Count == handles.Count;
        private static bool LifeAllAbsent(Document document, IReadOnlyCollection<string> handles) =>
            handles.Count > 0 && CadHandleService.Resolve(document, handles).Count == 0;

        private static LifeContextState LifeContext(bool requireProject)
        {
            var nonce = (Environment.GetEnvironmentVariable(LifeNonceVariable) ?? string.Empty).Trim();
            if (!Guid.TryParseExact(nonce, "N", out _)) throw new InvalidOperationException("Curved lifecycle probe is automation-only.");
            var sourceSha = (Environment.GetEnvironmentVariable(LifeSourceShaVariable) ?? string.Empty).Trim().ToLowerInvariant();
            if (sourceSha.Length != 40 || sourceSha.Any(x => !Uri.IsHexDigit(x))) throw new InvalidOperationException("Curved lifecycle source SHA is invalid.");
            RequireAssemblyRevision(typeof(CurvedStructuralRuntimeProbeCommands).Assembly, sourceSha, "QS3D.BricsCAD.V25");
            RequireAssemblyRevision(typeof(ProjectState).Assembly, sourceSha, "QS3D.Core");
            var document = BcadApplication.DocumentManager.MdiActiveDocument ?? throw new InvalidOperationException("Curved lifecycle active document is missing.");
            var project = requireProject ? ExistingProjectMutationContext.Require(document, "Curved lifecycle qualification") : null;
            return new LifeContextState(document, project, nonce, sourceSha,
                LifeRequiredPath(Environment.GetEnvironmentVariable(LifeResultVariable), LifeResultFile),
                LifeRequiredPath(Environment.GetEnvironmentVariable(LifePhaseVariable), LifePhaseFile),
                LifeRequiredDrawing(Environment.GetEnvironmentVariable(LifeDrawingAVariable), "curved-structural-lifecycle-a-probe-copy.dwg"),
                LifeRequiredDrawing(Environment.GetEnvironmentVariable(LifeDrawingBVariable), "curved-structural-lifecycle-b-probe-copy.dwg"));
        }

        private static LifeSessionOne LifeRequireSessionOne(LifeContextState context)
        {
            var state = _lifeSessionOne ?? throw new InvalidOperationException("Curved lifecycle session one is missing.");
            LifeRequire(ReferenceEquals(state.Document, context.Document) && ReferenceEquals(state.Project, context.Project) && state.Nonce == context.Nonce,
                "curved lifecycle session one context changed");
            return state;
        }

        private static LifeSessionTwo LifeRequireSessionTwo(LifeContextState context)
        {
            var state = _lifeSessionTwo ?? throw new InvalidOperationException("Curved lifecycle session two is missing.");
            LifeRequire(ReferenceEquals(state.Document, context.Document) && ReferenceEquals(state.Project, context.Project) && state.Nonce == context.Nonce,
                "curved lifecycle session two context changed");
            return state;
        }

        private static Document LifeFindDocument(string path)
        {
            foreach (Document candidate in BcadApplication.DocumentManager)
                if (LifeSamePath(candidate.Name, path)) return candidate;
            throw new InvalidOperationException("Curved lifecycle expected drawing is not open.");
        }

        private static void LifeRun(string phase, Action action)
        {
            try { action(); }
            catch (Exception error)
            {
                LifeTryWriteFailure(phase, error);
                throw;
            }
        }

        private static void LifeTryWriteFailure(string phase, Exception error)
        {
            try
            {
                var raw = Environment.GetEnvironmentVariable(LifeResultVariable);
                if (string.IsNullOrWhiteSpace(raw)) return;
                var path = LifeRequiredPath(raw, LifeResultFile);
                if (File.Exists(path)) return;
                LifeWriteMarker(path, new[]
                {
                    "status=FAIL", "command=QS3DCURVEDLIFECOMPLETE", "schema=" + LifeSchema,
                    "qualification_boundary=LOCAL_003_CURVED_LIFECYCLE_ONLY", "production_local003_qualified=false",
                    "error_code=CURVED_STRUCTURAL_LIFECYCLE_RUNTIME_FAILED", "failure_phase=" + LifeSafe(phase),
                    "failure_code=STATE_REJECTED", "exception_type=" + LifeSafe(error.GetType().Name)
                });
            }
            catch { }
        }

        private static string LifeRequiredPath(string? value, string expectedName)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Curved lifecycle marker path is missing.");
            var path = Path.GetFullPath(value);
            if (!string.Equals(Path.GetFileName(path), expectedName, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Curved lifecycle marker filename is invalid.");
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) throw new DirectoryNotFoundException("Curved lifecycle marker directory is missing.");
            return path;
        }

        private static string LifeRequiredDrawing(string? value, string expectedSuffix)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Curved lifecycle drawing path is missing.");
            var path = Path.GetFullPath(value);
            if (!Path.GetFileName(path).EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Curved lifecycle drawing suffix is invalid.");
            return path;
        }

        private static void LifeWriteMarker(string path, IEnumerable<string> lines)
        {
            if (File.Exists(path)) throw new IOException("Curved lifecycle marker already exists.");
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllLines(temp, lines.Select(LifeSafe), new System.Text.UTF8Encoding(false));
                File.Move(temp, path);
            }
            finally { try { if (File.Exists(temp)) File.Delete(temp); } catch { } }
        }

        private static void LifeRequirePath(string actual, string expected, string label) =>
            LifeRequire(LifeSamePath(actual, expected), "active drawing is not drawing " + label);
        private static bool LifeSamePath(string? left, string? right)
        {
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }
        private static string LifeSafe(string value) => (value ?? string.Empty).Replace("\r", " ").Replace("\n", " ").Trim();
        private static string LifeExpectedReopenFingerprint()
        {
            var value = (Environment.GetEnvironmentVariable(LifeExpectedReopenFingerprintVariable) ?? string.Empty).Trim().ToUpperInvariant();
            if (value.Length != 64 || value.Any(x => !Uri.IsHexDigit(x)))
                throw new InvalidOperationException("Curved lifecycle expected reopen fingerprint is invalid.");
            return value;
        }

        private static string LifeStateFingerprint(ProjectState project, IReadOnlyList<string> elementIds)
        {
            var checkpoint = ProjectPersistenceCheckpoint.Capture(project, elementIds);
            LifeRequire(checkpoint.Matches(project), "lifecycle fingerprint checkpoint is unstable");
            var builder = new StringBuilder();
            LifeAppend(builder, project.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            LifeAppend(builder, project.ProjectId);
            LifeAppend(builder, project.Name);
            LifeAppend(builder, project.DrawingPath);
            LifeAppend(builder, project.DrawingFingerprint);
            LifeAppend(builder, project.ActiveFloorId);
            LifeAppend(builder, project.ActiveZoneId);
            LifeAppend(builder, project.ChangeVersion.ToString(CultureInfo.InvariantCulture));
            LifeAppend(builder, project.UpdatedUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            foreach (var pair in project.Metadata.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
            {
                LifeAppend(builder, pair.Key);
                LifeAppend(builder, pair.Value ?? string.Empty);
            }
            foreach (var floor in project.Floors.OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                LifeAppend(builder, floor.Id);
                LifeAppend(builder, floor.Name);
                LifeAppend(builder, floor.ElevationM.ToString("R", CultureInfo.InvariantCulture));
            }
            foreach (var id in elementIds.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var element = project.FindElement(id) ?? throw new InvalidOperationException("Missing lifecycle fingerprint element.");
                LifeAppend(builder, element.Id);
                LifeAppend(builder, ((int)element.Category).ToString(CultureInfo.InvariantCulture));
                LifeAppend(builder, element.FamilyId);
                LifeAppend(builder, element.FloorId);
                LifeAppend(builder, element.ZoneId);
                LifeAppend(builder, element.DrawingFingerprint);
                LifeAppend(builder, ((int)element.Dirty).ToString(CultureInfo.InvariantCulture));
                LifeAppend(builder, element.UpdatedUtc.Ticks.ToString(CultureInfo.InvariantCulture));
                foreach (var value in element.SourceHandles) LifeAppend(builder, value);
                foreach (var value in element.DependsOn) LifeAppend(builder, value);
                foreach (var pair in element.Properties.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    LifeAppend(builder, pair.Key);
                    LifeAppend(builder, pair.Value ?? string.Empty);
                }
                foreach (var pair in element.Quantities.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    LifeAppend(builder, pair.Key);
                    LifeAppend(builder, pair.Value.ToString("R", CultureInfo.InvariantCulture));
                }
            }
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(builder.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty);
            }
        }

        private static void LifeAppend(StringBuilder builder, string value)
        {
            var normalized = value ?? string.Empty;
            builder.Append(normalized.Length.ToString(CultureInfo.InvariantCulture));
            builder.Append(':');
            builder.Append(normalized);
            builder.Append('|');
        }
        private static string LifeBool(bool value) => value ? "true" : "false";
        private static void LifeRequire(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

        private sealed class LifeContextState
        {
            public LifeContextState(Document document, ProjectState? project, string nonce, string sourceSha, string resultPath, string phasePath, string drawingA, string drawingB)
            { Document = document; Project = project!; Nonce = nonce; SourceSha = sourceSha; ResultPath = resultPath; PhasePath = phasePath; DrawingA = drawingA; DrawingB = drawingB; }
            public Document Document { get; } public ProjectState Project { get; } public string Nonce { get; } public string SourceSha { get; }
            public string ResultPath { get; } public string PhasePath { get; } public string DrawingA { get; } public string DrawingB { get; }
        }

        private sealed class LifeSessionOne
        {
            public LifeSessionOne(Document document, ProjectState project, string nonce, IReadOnlyList<string> ids, ProjectPersistenceCheckpoint before)
            { Document = document; Project = project; Nonce = nonce; ElementIds = ids; Before = before; }
            public Document Document { get; } public ProjectState Project { get; } public string Nonce { get; } public IReadOnlyList<string> ElementIds { get; }
            public ProjectPersistenceCheckpoint Before { get; } public ProjectPersistenceCheckpoint? After { get; set; }
            public List<string> AfterHandles { get; set; } = new List<string>(); public List<string> RedoHandles { get; set; } = new List<string>();
            public bool UndoGeneratedAbsent { get; set; } public bool UndoCoherent { get; set; } public bool RedoCoherent { get; set; }
        }

        private sealed class LifeSessionTwo
        {
            public LifeSessionTwo(Document document, ProjectState project, string nonce, IReadOnlyList<string> ids, List<string> reopened, ProjectPersistenceCheckpoint checkpoint, bool reopenCoherent)
            { Document = document; Project = project; Nonce = nonce; ElementIds = ids; ReopenedHandles = reopened; Reopened = checkpoint; ReopenCoherent = reopenCoherent; }
            public Document Document { get; } public ProjectState Project { get; } public string Nonce { get; } public IReadOnlyList<string> ElementIds { get; }
            public List<string> ReopenedHandles { get; } public ProjectPersistenceCheckpoint Reopened { get; }
            public bool ReopenCoherent { get; }
            public List<string> RebuiltHandles { get; set; } = new List<string>(); public ProjectPersistenceCheckpoint? Rebuilt { get; set; }
            public bool OldGeneratedRemoved { get; set; } public bool NewGeneratedDisjoint { get; set; } public bool RebuildCountsStable { get; set; } public bool AUnchangedAfterB { get; set; }
        }

        private sealed class LifeDocumentState
        {
            public LifeDocumentState(Document document, ProjectState project, IReadOnlyList<string> ids, List<string> generated, ProjectPersistenceCheckpoint checkpoint)
            { Document = document; Project = project; ElementIds = ids; GeneratedHandles = generated; Checkpoint = checkpoint; }
            public Document Document { get; } public ProjectState Project { get; } public IReadOnlyList<string> ElementIds { get; }
            public List<string> GeneratedHandles { get; set; } public ProjectPersistenceCheckpoint Checkpoint { get; set; }
        }
    }
}
