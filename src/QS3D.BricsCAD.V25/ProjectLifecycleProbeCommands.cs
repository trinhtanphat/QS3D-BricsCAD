using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Bricscad.ApplicationServices;
using QS3D.Core.Audit;
using QS3D.Core.Domain;
using QS3D.Core.Services;
using QS3D.Core.Units;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    /// <summary>
    /// Automation-only, synthetic-fixture qualification for save/reopen, cold-cache
    /// canonical binding and multi-DWG project isolation. Persisted evidence contains
    /// booleans/counts only; drawing paths and project ids never enter the final marker.
    /// </summary>
    public sealed class ProjectLifecycleProbeCommands
    {
        private const string ResultVariable = "QS3D_LIFECYCLE_RESULT";
        private const string StateVariable = "QS3D_LIFECYCLE_STATE";
        private const string NonceVariable = "QS3D_LIFECYCLE_NONCE";
        private const string RoleVariable = "QS3D_LIFECYCLE_ROLE";
        private const string DrawingAVariable = "QS3D_LIFECYCLE_DWG_A";
        private const string DrawingBVariable = "QS3D_LIFECYCLE_DWG_B";
        private const string DrawingCVariable = "QS3D_LIFECYCLE_DWG_C";
        private const string DrawingDVariable = "QS3D_LIFECYCLE_DWG_D";
        private const string PhaseVariable = "QS3D_LIFECYCLE_PHASE";
        private const string StateFileName = "project-lifecycle-state.txt";
        private const string FinalResultFileName = "project-lifecycle-result.txt";
        private const string RoleMetadataKey = "QS3D.LifecycleProbe.Role";
        private const string MutationMetadataKey = "QS3D.LifecycleProbe.MultiDwgMutation";
        private const string CommandPhaseMetadataKey = "QS3D.LifecycleProbe.CommandPhase";
        private const string ProbeRoomFamilyId = "LIFECYCLE-ROOM-FAMILY";
        private const string ProbeRoomElementId = "LIFECYCLE-ROOM";

        [CommandMethod("QS3DLIFECYCLESEED", CommandFlags.Modal)]
        public void Seed()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var role = RequiredRole();
                var statePath = RequiredStatePath(nonce);
                var expectedDrawing = RequiredDrawingPath(role == "A" ? DrawingAVariable : DrawingBVariable);
                EnsureProbeScope(statePath, resultPath!, expectedDrawing);
                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available for the lifecycle seed.");
                if (!SamePath(document.Name, expectedDrawing))
                    throw new InvalidOperationException("The lifecycle seed active drawing does not match its assigned role.");
                var projectPath = ProjectContextCoordinator.GetProjectPath(document);
                if (File.Exists(projectPath) || File.Exists(projectPath + ".bak"))
                    throw new InvalidOperationException("The lifecycle seed requires a drawing copy without a QS3D sidecar.");

                var project = ProjectContextCoordinator.GetOrCreate(document);
                project.Metadata[RoleMetadataKey] = role;
                project.Metadata.Remove(MutationMetadataKey);
                project.Touch();
                if (!ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("The lifecycle seed mutation was not marked pending.");

                document.Editor.WriteMessage("\nQS3D lifecycle seed prepared for automatic DWG-save persistence.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "SEED_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DLIFECYCLEAFTERSAVE", CommandFlags.Modal)]
        public void VerifyAfterSave()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var role = RequiredRole();
                var statePath = RequiredStatePath(nonce);
                var expectedResult = "project-lifecycle-seed-" + role.ToLowerInvariant() + ".txt";
                var validatedResult = RequiredOutputPath(resultPath, expectedResult, "seed result");
                var expectedDrawing = RequiredDrawingPath(role == "A" ? DrawingAVariable : DrawingBVariable);
                EnsureProbeScope(statePath, validatedResult, expectedDrawing);
                if (File.Exists(validatedResult)) throw new IOException("The lifecycle seed result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available after the lifecycle save.");
                if (!SamePath(document.Name, expectedDrawing))
                    throw new InvalidOperationException("The lifecycle after-save drawing does not match its assigned role.");
                var projectPath = ProjectContextCoordinator.GetProjectPath(document);
                if (!File.Exists(projectPath))
                    throw new InvalidOperationException("DWG SaveComplete did not persist the matching QS3D sidecar.");
                if (ProjectContextCoordinator.HasPendingChanges(document))
                    throw new InvalidOperationException("The QS3D project remains pending after DWG SaveComplete.");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new InvalidOperationException("The saved QS3D project is not readable.");
                if (!project.Metadata.TryGetValue(RoleMetadataKey, out var storedRole) ||
                    !string.Equals(storedRole, role, StringComparison.Ordinal))
                    throw new InvalidOperationException("The saved lifecycle role did not round-trip.");

                WriteStateRole(statePath, nonce, role, ProjectDigest(project.ProjectId, nonce));
                WriteMarkerAtomic(validatedResult, new[]
                {
                    "status=PASS",
                    "command=QS3DLIFECYCLEAFTERSAVE",
                    "schema=QS3D_PROJECT_LIFECYCLE_SEED_V1",
                    "nonce=" + nonce,
                    "role=" + role,
                    "dwg_savecomplete_sidecar=true",
                    "pending_changes_cleared=true",
                    "saved_project_readable=true"
                });
                document.Editor.WriteMessage("\nQS3D lifecycle seed/save check PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "AFTER_SAVE_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DLIFECYCLEPROBE", CommandFlags.Modal)]
        public void Run()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var state = ReadState(RequiredStatePath(nonce), nonce);
                var expectedA = RequiredDigest(state, "a");
                var expectedB = RequiredDigest(state, "b");
                var result = RequiredOutputPath(resultPath, FinalResultFileName, "result");
                if (File.Exists(result)) throw new IOException("The lifecycle result already exists.");

                var drawingA = RequiredDrawingPath(DrawingAVariable);
                var drawingB = RequiredDrawingPath(DrawingBVariable);
                var drawingC = RequiredDrawingPath(DrawingCVariable);
                var drawingD = RequiredDrawingPath(DrawingDVariable);
                EnsureProbeScope(RequiredStatePath(nonce), result, drawingA, drawingB, drawingC, drawingD);
                var documentA = FindDocument(drawingA);
                var documentB = FindDocument(drawingB);
                var documentC = FindDocument(drawingC);
                var documentD = FindDocument(drawingD);

                // Simulate a cold cache after reopen. A and B must reload their existing
                // sidecars; C deliberately has none and must remain unavailable.
                ProjectContextCoordinator.Forget(documentA);
                ProjectContextCoordinator.Forget(documentB);
                if (ProjectContextCoordinator.TryGetReadOnly(documentC, out _))
                    throw new InvalidOperationException("Activating a drawing without a sidecar created or cached a replacement project.");
                if (!ProjectContextCoordinator.TryGetReadOnly(documentA, out var observedA) ||
                    !ProjectContextCoordinator.TryGetReadOnly(documentB, out var observedB))
                    throw new InvalidOperationException("Cold-cache reopen could not read both saved sidecars.");
                EnsureProject(observedA, "A", expectedA, nonce);
                EnsureProject(observedB, "B", expectedB, nonce);
                if (string.Equals(observedA.ProjectId, observedB.ProjectId, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The two reopened drawings share one project identity.");
                var detachedA = DetachedProjectStamp.Capture(observedA);
                var detachedB = DetachedProjectStamp.Capture(observedB);

                if (!ExistingProjectMutationContext.TryGet(documentA, out var canonicalA) ||
                    !ExistingProjectMutationContext.TryGet(documentB, out var canonicalB))
                    throw new InvalidOperationException("Existing-project mutation binding failed after cold-cache reopen.");
                if (ReferenceEquals(observedA, canonicalA) || ReferenceEquals(observedB, canonicalB))
                    throw new InvalidOperationException("A detached read-only snapshot leaked into the canonical mutation context.");
                EnsureProject(canonicalA, "A", expectedA, nonce);
                EnsureProject(canonicalB, "B", expectedB, nonce);

                canonicalA.Metadata[MutationMetadataKey] = "A";
                canonicalA.Touch();
                ProjectContextCoordinator.Save(documentA);
                canonicalB.Metadata[MutationMetadataKey] = "B";
                canonicalB.Touch();
                ProjectContextCoordinator.Save(documentB);
                detachedA.EnsureUnchanged(observedA);
                detachedB.EnsureUnchanged(observedB);

                ProjectContextCoordinator.Forget(documentA);
                ProjectContextCoordinator.Forget(documentB);
                if (!ProjectContextCoordinator.TryGetReadOnly(documentA, out var reopenedA) ||
                    !ProjectContextCoordinator.TryGetReadOnly(documentB, out var reopenedB))
                    throw new InvalidOperationException("The multi-DWG mutations did not survive a second cold reload.");
                EnsureProject(reopenedA, "A", expectedA, nonce);
                EnsureProject(reopenedB, "B", expectedB, nonce);
                EnsureMutation(reopenedA, "A");
                EnsureMutation(reopenedB, "B");
                if (ProjectContextCoordinator.TryGetReadOnly(documentC, out _))
                    throw new InvalidOperationException("Another drawing mutation populated the absent-sidecar document context.");
                EnsureCorruptSidecarFailsClosed(documentD);

                WriteMarkerAtomic(result, new[]
                {
                    "status=PASS",
                    "command=QS3DLIFECYCLEPROBE",
                    "schema=QS3D_PROJECT_LIFECYCLE_V1",
                    "nonce=" + nonce,
                    "document_count=" + Application.DocumentManager.Count.ToString(CultureInfo.InvariantCulture),
                    "dwg_savecomplete_sidecar=true",
                    "cold_reopen_project_identity_matched=true",
                    "canonical_bind_matched=true",
                    "detached_snapshot_not_mutated=true",
                    "distinct_project_identity=true",
                    "multi_dwg_mutation_isolated=true",
                    "second_cold_reload_persisted=true",
                    "absent_sidecar_noncreating=true",
                    "corrupt_sidecar_fail_closed=true"
                });
                documentD.Editor.WriteMessage("\nQS3D save/reopen/multi-DWG lifecycle probe PASS.");
            }
            catch (System.Exception)
            {
                TryWriteFailure(resultPath, "LIFECYCLE_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DLIFECYCLECOMMANDPREP", CommandFlags.Modal)]
        public void PrepareCommandLifecycle()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var phase = RequiredPhase();
                var statePath = RequiredStatePath(nonce);
                var existing = IsExistingPhase(phase);
                var expectedDrawing = RequiredDrawingPath(existing ? DrawingAVariable : DrawingCVariable);
                var result = RequiredOutputPath(resultPath, CommandResultFileName(phase), "command lifecycle result");
                EnsureProbeScope(statePath, result, expectedDrawing);

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available for command lifecycle preparation.");
                if (!SamePath(document.Name, expectedDrawing))
                    throw new InvalidOperationException("The command lifecycle active drawing does not match its phase.");

                if (IsUnitPhase(phase)) PrepareUnitCommandPhase(document, phase, nonce, statePath);
                else if (existing) PrepareExistingCommandPhase(document, phase, nonce, statePath);
                else PrepareAbsentCommandPhase(document, phase);
                document.Editor.WriteMessage("\nQS3D command lifecycle phase prepared.");
            }
            catch (System.Exception)
            {
                TryWriteCommandFailure(resultPath, "COMMAND_PREP_FAILED");
                throw;
            }
        }

        [CommandMethod("QS3DLIFECYCLECOMMANDVERIFY", CommandFlags.Modal)]
        public void VerifyCommandLifecycle()
        {
            var resultPath = Environment.GetEnvironmentVariable(ResultVariable);
            if (SkipOutsideAutomation(resultPath)) return;
            try
            {
                var nonce = RequiredNonce();
                var phase = RequiredPhase();
                var statePath = RequiredStatePath(nonce);
                var existing = IsExistingPhase(phase);
                var expectedDrawing = RequiredDrawingPath(existing ? DrawingAVariable : DrawingCVariable);
                var result = RequiredOutputPath(resultPath, CommandResultFileName(phase), "command lifecycle result");
                EnsureProbeScope(statePath, result, expectedDrawing);
                if (File.Exists(result)) throw new IOException("The command lifecycle result already exists.");

                var document = Application.DocumentManager.MdiActiveDocument
                    ?? throw new InvalidOperationException("No active document is available for command lifecycle verification.");
                if (!SamePath(document.Name, expectedDrawing))
                    throw new InvalidOperationException("The command lifecycle verification drawing does not match its phase.");

                var marker = IsUnitPhase(phase)
                    ? VerifyUnitCommandPhase(document, phase, nonce, statePath)
                    : existing
                        ? VerifyExistingCommandPhase(document, phase, nonce, statePath)
                        : VerifyAbsentCommandPhase(document, phase);
                WriteMarkerAtomic(result, marker);
                document.Editor.WriteMessage("\nQS3D command lifecycle phase PASS.");
            }
            catch (LifecycleProbeFailure probeFailure)
            {
                TryWriteCommandFailure(resultPath, probeFailure.ErrorCode);
                throw;
            }
            catch (System.Exception)
            {
                TryWriteCommandFailure(resultPath, "COMMAND_VERIFY_FAILED");
                throw;
            }
        }

        private static void PrepareExistingCommandPhase(Document document, string phase, string nonce, string statePath)
        {
            var state = ReadState(statePath, nonce);
            var expectedA = RequiredDigest(state, "a");
            ProjectContextCoordinator.Forget(document);
            if (!ExistingProjectMutationContext.TryGet(document, out var project))
                throw new InvalidOperationException("The existing-project command phase could not bind its saved sidecar.");
            EnsureProject(project, "A", expectedA, nonce);

            var room = EnsureProbeRoom(document, project);
            if (phase == "FINISH_EXISTING") room.MarkClean(ElementDirtyFlags.All);
            else room.MarkDirty(ElementDirtyFlags.All);
            project.Metadata[CommandPhaseMetadataKey] = phase;
            project.Touch();
            ProjectContextCoordinator.Save(document);
            if (ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("Command lifecycle preparation did not persist a clean baseline.");

            var roomHandle = room.SourceHandles.Single();
            ProjectContextCoordinator.Forget(document);
            if (phase == "FINISH_EXISTING" && Cad.CadHandleService.SelectIfAny(document, new[] { roomHandle }) != 1)
                throw new InvalidOperationException("The room source could not be selected for QS3DFINISH.");
        }

        private static void PrepareAbsentCommandPhase(Document document, string phase)
        {
            var path = ProjectContextCoordinator.GetProjectPath(document);
            if (File.Exists(path) || File.Exists(path + ".bak"))
                throw new InvalidOperationException("The absent-project command phase unexpectedly has a sidecar.");
            ProjectContextCoordinator.Forget(document);
            if (ProjectContextCoordinator.TryGetReadOnly(document, out _) || ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("The absent-project command phase began with cached project state.");
            if (phase == "FINISH_ABSENT")
            {
                var sourceHandle = FirstLiveSourceHandle(document);
                if (Cad.CadHandleService.SelectIfAny(document, new[] { sourceHandle }) != 1)
                    throw new InvalidOperationException("The absent-project fixture source could not be selected for QS3DFINISH.");
            }
        }

        private static void PrepareUnitCommandPhase(Document document, string phase, string nonce, string statePath)
        {
            if (phase == "BQ_LEGACY_EXISTING")
            {
                var state = ReadState(statePath, nonce);
                var expectedA = RequiredDigest(state, "a");
                ProjectContextCoordinator.Forget(document);
                if (!ExistingProjectMutationContext.TryGet(document, out var project))
                    throw new InvalidOperationException("The legacy-unit phase could not bind its saved sidecar.");
                EnsureProject(project, "A", expectedA, nonce);
                EnsureProbeRoom(document, project);
                if (!Cad.CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                    throw new InvalidOperationException("The synthetic lifecycle fixture must expose a supported native INSUNITS value.");

                project.Metadata.Remove(DrawingUnitResolutionPolicy.BoundMetadataKey);
                project.Metadata.Remove(DrawingUnitResolutionPolicy.OverrideMetadataKey);
                project.Metadata.Remove(DrawingUnitResolutionPolicy.BindingSourceMetadataKey);
                project.Metadata[DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = nativeUnit + " (assumed)";
                project.Metadata[DrawingUnitResolutionPolicy.LegacyAssumptionMetadataKey] = "Lifecycle probe legacy unit evidence";
                project.Metadata[CommandPhaseMetadataKey] = phase;
                project.Touch();
                ProjectContextCoordinator.Save(document);
                ProjectContextCoordinator.Forget(document);
                return;
            }

            PrepareAbsentCommandPhase(document, phase);
            if (phase == "BQ_NATIVE_ABSENT")
            {
                if (!Cad.CadUnitService.TryGetNativeLengthUnit(document, out _))
                    throw new InvalidOperationException("The no-project BQ phase requires supported native INSUNITS.");
                return;
            }

            if (phase == "UNITS_OVERRIDE_ABSENT")
            {
                document.Database.Insunits = Teigha.DatabaseServices.UnitsValue.Undefined;
                if (Cad.CadUnitService.TryGetNativeLengthUnit(document, out _))
                    throw new InvalidOperationException("The explicit unit phase could not make INSUNITS unresolved.");
                Services.DrawingUnitAutomationConfirmation.Arm(document, LengthUnit.Meter);
                return;
            }

            throw new InvalidOperationException("Unsupported drawing-unit lifecycle phase.");
        }

        private static IReadOnlyList<string> VerifyExistingCommandPhase(Document document, string phase, string nonce, string statePath)
        {
            var state = ReadState(statePath, nonce);
            var expectedA = RequiredDigest(state, "a");
            if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                throw new InvalidOperationException("The tested command lost its existing project.");
            EnsureProject(project, "A", expectedA, nonce);
            if (!project.Metadata.TryGetValue(CommandPhaseMetadataKey, out var storedPhase) ||
                !string.Equals(storedPhase, phase, StringComparison.Ordinal))
                throw new InvalidOperationException("The tested command rebound a stale or replacement project.");
            if (!ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("The tested command did not retain its semantic mutation on the canonical project.");

            var room = project.FindElement(ProbeRoomElementId)
                ?? throw new InvalidOperationException("The command lifecycle Room is missing after command execution.");
            var semanticDirty = room.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity);
            var finishesGenerated = false;
            if (phase == "FINISH_EXISTING")
            {
                foreach (var category in RoomFinishSynchronizationService.Categories)
                {
                    var finish = RoomFinishIdentityService.FindExisting(project, room, category)
                        ?? throw new InvalidOperationException("QS3DFINISH did not create every canonical Room Finish category.");
                    if (!finish.DependsOn.Any(x => string.Equals(x, room.Id, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException("A generated Room Finish lost its Room dependency.");
                    if ((finish.Dirty & (ElementDirtyFlags.Properties | ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity)) != ElementDirtyFlags.None)
                        throw new InvalidOperationException("A generated Room Finish remains semantically dirty.");
                }
                finishesGenerated = true;
            }
            else if (semanticDirty != ElementDirtyFlags.None)
            {
                throw new InvalidOperationException("QS3DREGEN/QS3DREFRESH left the probe Room semantically dirty.");
            }

            return new[]
            {
                "status=PASS",
                "command=QS3DLIFECYCLECOMMANDVERIFY",
                "schema=QS3D_PROJECT_COMMAND_LIFECYCLE_V1",
                "nonce=" + nonce,
                "phase=" + phase,
                "existing_project_bound=true",
                "canonical_project_identity_matched=true",
                "pending_semantic_mutation=true",
                "semantic_regenerated=" + (phase == "FINISH_EXISTING" ? "false" : "true"),
                "room_finishes_generated=" + (finishesGenerated ? "true" : "false")
            };
        }

        private static IReadOnlyList<string> VerifyAbsentCommandPhase(Document document, string phase)
        {
            if (ProjectContextCoordinator.TryGetReadOnly(document, out _) || ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("A no-project command created or cached replacement project state.");
            var path = ProjectContextCoordinator.GetProjectPath(document);
            if (File.Exists(path) || File.Exists(path + ".bak"))
                throw new InvalidOperationException("A no-project command created a replacement sidecar.");
            return new[]
            {
                "status=PASS",
                "command=QS3DLIFECYCLECOMMANDVERIFY",
                "schema=QS3D_PROJECT_COMMAND_LIFECYCLE_V1",
                "nonce=" + RequiredNonce(),
                "phase=" + phase,
                "absent_sidecar_noncreating=true",
                "no_cached_project=true",
                "no_pending_project_state=true",
                "semantic_mutation_not_applied=true"
            };
        }

        private static IReadOnlyList<string> VerifyUnitCommandPhase(Document document, string phase, string nonce, string statePath)
        {
            if (phase == "BQ_LEGACY_EXISTING")
            {
                var state = ReadState(statePath, nonce);
                var expectedA = RequiredDigest(state, "a");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new LifecycleProbeFailure("LEGACY_PROJECT_MISSING");
                try { EnsureProject(project, "A", expectedA, nonce); }
                catch (InvalidOperationException) { throw new LifecycleProbeFailure("LEGACY_PROJECT_IDENTITY_MISMATCH"); }
                if (project.Elements.Count == 0)
                    throw new LifecycleProbeFailure("LEGACY_ELEMENTS_MISSING");
                if (!Cad.CadUnitService.TryGetNativeLengthUnit(document, out var nativeUnit))
                    throw new LifecycleProbeFailure("LEGACY_NATIVE_UNIT_MISSING");
                RequireMetadata(project, CommandPhaseMetadataKey, phase, "LEGACY_PHASE_METADATA_MISMATCH");
                RequireMetadata(project, DrawingUnitResolutionPolicy.BoundMetadataKey, nativeUnit.ToString(), "LEGACY_BOUND_UNIT_MISMATCH");
                RequireMetadata(project, DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey, nativeUnit.ToString(), "LEGACY_EFFECTIVE_UNIT_MISMATCH");
                RequireMetadata(project, DrawingUnitResolutionPolicy.BindingSourceMetadataKey, DrawingUnitResolutionSource.NativeInsunits.ToString(), "LEGACY_SOURCE_MISMATCH");
                if (project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.OverrideMetadataKey))
                    throw new LifecycleProbeFailure("LEGACY_OVERRIDE_CREATED");
                if (ProjectContextCoordinator.HasPendingChanges(document))
                    throw new LifecycleProbeFailure("LEGACY_BINDING_PENDING");
                return new[]
                {
                    "status=PASS",
                    "command=QS3DLIFECYCLECOMMANDVERIFY",
                    "schema=QS3D_PROJECT_COMMAND_LIFECYCLE_V2",
                    "nonce=" + nonce,
                    "phase=" + phase,
                    "existing_project_bound=true",
                    "canonical_project_identity_matched=true",
                    "legacy_unit_binding_persisted=true",
                    "no_pending_project_state=true"
                };
            }

            if (phase == "BQ_NATIVE_ABSENT")
            {
                if (!Cad.CadUnitService.TryGetNativeLengthUnit(document, out _))
                    throw new LifecycleProbeFailure("NATIVE_BQ_UNIT_MISSING");
                return VerifyAbsentCommandPhase(document, phase)
                    .Concat(new[] { "native_unit_resolution_noncreating=true" })
                    .ToList();
            }

            if (phase == "UNITS_OVERRIDE_ABSENT")
            {
                if (Cad.CadUnitService.TryGetNativeLengthUnit(document, out _))
                    throw new LifecycleProbeFailure("OVERRIDE_NATIVE_UNIT_PRESENT");
                if (Services.DrawingUnitAutomationConfirmation.IsArmed(document))
                    throw new LifecycleProbeFailure("OVERRIDE_CONFIRMATION_NOT_CONSUMED");
                if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))
                    throw new LifecycleProbeFailure("OVERRIDE_PROJECT_MISSING");
                if (project.Elements.Count != 0)
                    throw new LifecycleProbeFailure("OVERRIDE_ELEMENTS_CREATED");
                RequireMetadata(project, DrawingUnitResolutionPolicy.OverrideMetadataKey, LengthUnit.Meter.ToString(), "OVERRIDE_UNIT_MISMATCH");
                if (project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.BoundMetadataKey) ||
                    project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey) ||
                    project.Metadata.ContainsKey(DrawingUnitResolutionPolicy.BindingSourceMetadataKey))
                    throw new LifecycleProbeFailure("OVERRIDE_BOUND_EMPTY_PROJECT");
                if (!Cad.CadUnitService.TryGetPolicy(document, out _, out var effectiveResolution) ||
                    effectiveResolution.Unit != LengthUnit.Meter ||
                    effectiveResolution.Source != DrawingUnitResolutionSource.ProjectOverride)
                    throw new LifecycleProbeFailure("OVERRIDE_RESOLUTION_MISMATCH");
                if (ProjectContextCoordinator.HasPendingChanges(document))
                    throw new LifecycleProbeFailure("OVERRIDE_PENDING");
                return new[]
                {
                    "status=PASS",
                    "command=QS3DLIFECYCLECOMMANDVERIFY",
                    "schema=QS3D_PROJECT_COMMAND_LIFECYCLE_V2",
                    "nonce=" + nonce,
                    "phase=" + phase,
                    "explicit_unit_override_persisted=true",
                    "automation_confirmation_consumed=true",
                    "intentional_project_bootstrap=true",
                    "no_pending_project_state=true",
                    "semantic_elements_not_created=true",
                    "unbound_binding_evidence_absent=true",
                    "effective_override_resolved=true"
                };
            }

            throw new InvalidOperationException("Unsupported drawing-unit lifecycle verification phase.");
        }

        private static void RequireMetadata(ProjectState project, string key, string expected, string errorCode)
        {
            if (!project.Metadata.TryGetValue(key, out var actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new LifecycleProbeFailure(errorCode);
        }

        private static ProjectElement EnsureProbeRoom(Document document, ProjectState project)
        {
            var family = project.FindFamily(ProbeRoomFamilyId);
            if (family == null)
            {
                family = new ProjectFamily(ProbeRoomFamilyId, "Lifecycle Room", ElementCategory.Room);
                project.Families.Add(family);
            }
            else if (family.Category != ElementCategory.Room)
            {
                throw new InvalidOperationException("The lifecycle Room Family id collides with another category.");
            }

            var room = project.FindElement(ProbeRoomElementId);
            if (room == null)
            {
                room = new ProjectElement(ProbeRoomElementId, ElementCategory.Room, family.Id, string.Empty, string.Empty);
                project.Elements.Add(room);
            }
            else if (room.Category != ElementCategory.Room || !string.Equals(room.FamilyId, family.Id, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("The lifecycle Room id collides with incompatible semantic state.");
            }

            var sourceHandle = FirstLiveSourceHandle(document);
            room.SourceHandles.Clear();
            room.SourceHandles.Add(sourceHandle);
            room.SetProperty("AreaM2", "25");
            room.SetProperty("PerimeterM", "20");
            room.SetProperty("HeightM", "3");
            return room;
        }

        private static string FirstLiveSourceHandle(Document document)
        {
            var handle = Cad.EntitySnapshotReader.ReadCurrentSpace(document)
                .Select(x => Cad.CadHandleService.NormalizeHexHandle(x.Handle))
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            return handle ?? throw new InvalidOperationException("The synthetic lifecycle fixture has no live selectable entity.");
        }

        private static string RequiredPhase()
        {
            var phase = (Environment.GetEnvironmentVariable(PhaseVariable) ?? string.Empty).Trim().ToUpperInvariant();
            switch (phase)
            {
                case "REGEN_EXISTING":
                case "REFRESH_EXISTING":
                case "FINISH_EXISTING":
                case "REGEN_ABSENT":
                case "REFRESH_ABSENT":
                case "FINISH_ABSENT":
                case "BQ_LEGACY_EXISTING":
                case "BQ_NATIVE_ABSENT":
                case "UNITS_OVERRIDE_ABSENT":
                    return phase;
                default:
                    throw new InvalidOperationException("The lifecycle command phase is invalid.");
            }
        }

        private static bool IsExistingPhase(string phase) => phase.EndsWith("_EXISTING", StringComparison.Ordinal);

        private static bool IsUnitPhase(string phase) =>
            phase == "BQ_LEGACY_EXISTING" ||
            phase == "BQ_NATIVE_ABSENT" ||
            phase == "UNITS_OVERRIDE_ABSENT";

        private static string CommandResultFileName(string phase) =>
            "project-lifecycle-" + phase.ToLowerInvariant().Replace('_', '-') + ".txt";

        private static void TryWriteCommandFailure(string? resultPath, string errorCode)
        {
            try
            {
                var nonce = RequiredNonce();
                var phase = RequiredPhase();
                var normalized = RequiredOutputPath(resultPath, CommandResultFileName(phase), "command lifecycle result");
                var statePath = RequiredStatePath(nonce);
                var drawing = RequiredDrawingPath(IsExistingPhase(phase) ? DrawingAVariable : DrawingCVariable);
                EnsureProbeScope(statePath, normalized, drawing);
                if (File.Exists(normalized)) return;
                WriteMarkerAtomic(normalized, new[]
                {
                    "status=FAIL",
                    "command=QS3DLIFECYCLECOMMANDVERIFY",
                    "nonce=" + nonce,
                    "phase=" + phase,
                    "error_code=" + errorCode
                });
            }
            catch { }
        }

        private static void EnsureProject(ProjectState project, string role, string digest, string nonce)
        {
            if (!string.Equals(ProjectDigest(project.ProjectId, nonce), digest, StringComparison.Ordinal))
                throw new InvalidOperationException("A reopened project identity did not match its saved seed.");
            if (!project.Metadata.TryGetValue(RoleMetadataKey, out var storedRole) ||
                !string.Equals(storedRole, role, StringComparison.Ordinal))
                throw new InvalidOperationException("A reopened project role did not match its drawing.");
        }

        private static void EnsureMutation(QS3D.Core.Domain.ProjectState project, string expected)
        {
            if (!project.Metadata.TryGetValue(MutationMetadataKey, out var actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidOperationException("A multi-DWG project mutation was lost or crossed into another drawing.");
        }

        private static void EnsureCorruptSidecarFailsClosed(Document document)
        {
            ProjectContextCoordinator.Forget(document);
            var readFailed = false;
            try { ProjectContextCoordinator.TryGetReadOnly(document, out _); }
            catch (InvalidDataException) { readFailed = true; }
            if (!readFailed)
                throw new InvalidOperationException("A corrupt sidecar did not fail the read-only load boundary.");

            var bindFailed = false;
            try { ProjectContextCoordinator.GetOrCreate(document); }
            catch (InvalidDataException) { bindFailed = true; }
            if (!bindFailed || ProjectContextCoordinator.HasPendingChanges(document))
                throw new InvalidOperationException("A corrupt sidecar created or cached mutable replacement state.");
        }

        private static Document FindDocument(string path)
        {
            foreach (Document document in Application.DocumentManager)
                if (SamePath(document.Name, path)) return document;
            throw new InvalidOperationException("A required lifecycle drawing is not open.");
        }

        private static string RequiredDrawingPath(string variable)
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException(variable + " is required.");
            var path = Path.GetFullPath(value);
            if (!path.EndsWith(".reference-copy.dwg", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                throw new InvalidOperationException("Lifecycle drawings must be existing disposable '*.reference-copy.dwg' files.");
            return path;
        }

        private static void EnsureProbeScope(string statePath, string resultPath, params string[] drawings)
        {
            var artifactRoot = Path.GetDirectoryName(Path.GetFullPath(statePath));
            if (string.IsNullOrWhiteSpace(artifactRoot) ||
                !string.Equals(Path.GetDirectoryName(Path.GetFullPath(resultPath)), artifactRoot, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Lifecycle state and results must share one qualification directory.");
            var copyRoot = Path.Combine(artifactRoot, "fixture-copies");
            foreach (var drawing in drawings)
                if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(drawing)), copyRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Lifecycle drawing copies must stay in the qualification fixture-copies directory.");
        }

        private static bool SamePath(string? left, string right)
        {
            if (string.IsNullOrWhiteSpace(left)) return false;
            try { return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase); }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) { return false; }
        }

        private static string RequiredRole()
        {
            var role = (Environment.GetEnvironmentVariable(RoleVariable) ?? string.Empty).Trim().ToUpperInvariant();
            if (role != "A" && role != "B") throw new InvalidOperationException("The lifecycle role must be A or B.");
            return role;
        }

        private static string RequiredNonce()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            if (!Guid.TryParseExact(nonce, "N", out _)) throw new InvalidOperationException("The lifecycle nonce is invalid.");
            return nonce;
        }

        private static string RequiredStatePath(string nonce)
        {
            var path = RequiredOutputPath(Environment.GetEnvironmentVariable(StateVariable), StateFileName, "state");
            if (!File.Exists(path)) throw new FileNotFoundException("The lifecycle state file is missing.");
            var state = ReadState(path, nonce);
            if (!state.TryGetValue("nonce", out var storedNonce) || !string.Equals(storedNonce, nonce, StringComparison.Ordinal))
                throw new InvalidDataException("The lifecycle state nonce does not match.");
            return path;
        }

        private static Dictionary<string, string> ReadState(string path, string nonce)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                var separator = line.IndexOf('=');
                if (separator <= 0) throw new InvalidDataException("The lifecycle state is malformed.");
                var key = line.Substring(0, separator).Trim();
                var value = line.Substring(separator + 1).Trim();
                if (key.Length == 0 || value.Length == 0 || result.ContainsKey(key))
                    throw new InvalidDataException("The lifecycle state contains an invalid or duplicate field.");
                result.Add(key, value);
            }
            if (!result.TryGetValue("nonce", out var storedNonce) || !string.Equals(storedNonce, nonce, StringComparison.Ordinal))
                throw new InvalidDataException("The lifecycle state nonce does not match.");
            return result;
        }

        private static void WriteStateRole(string path, string nonce, string role, string digest)
        {
            var state = ReadState(path, nonce);
            var key = role.ToLowerInvariant();
            if (state.ContainsKey(key)) throw new InvalidDataException("The lifecycle state already contains this role.");
            state[key] = digest;
            var tempPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            var backupPath = path + "." + Guid.NewGuid().ToString("N") + ".bak";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine("nonce=" + nonce);
                    foreach (var item in state.Where(x => !string.Equals(x.Key, "nonce", StringComparison.OrdinalIgnoreCase))
                                              .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                        writer.WriteLine(item.Key.ToLowerInvariant() + "=" + item.Value);
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Replace(tempPath, path, backupPath, true);
            }
            finally
            {
                TryDelete(tempPath);
                TryDelete(backupPath);
            }
        }

        private static string RequiredDigest(IDictionary<string, string> state, string key)
        {
            if (!state.TryGetValue(key, out var value) || value.Length != 64 || value.Any(x => !Uri.IsHexDigit(x)))
                throw new InvalidDataException("The lifecycle state is missing a canonical project digest.");
            return value.ToUpperInvariant();
        }

        private static string ProjectDigest(string projectId, string nonce)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(nonce + "\0" + (projectId ?? string.Empty)));
                return BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
        }

        private static string RequiredOutputPath(string? value, string expectedFileName, string label)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Lifecycle " + label + " path is required.", label);
            var fullPath = Path.GetFullPath(value);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.Equals(Path.GetFileName(fullPath), expectedFileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The lifecycle " + label + " filename is invalid.");
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
                throw new DirectoryNotFoundException("The lifecycle output directory must already exist.");
            return fullPath;
        }

        private static void TryWriteFailure(string? resultPath, string errorCode)
        {
            try
            {
                var normalized = (resultPath ?? string.Empty).Trim();
                if (normalized.Length == 0 || File.Exists(normalized)) return;
                var fileName = Path.GetFileName(normalized);
                if (!string.Equals(fileName, FinalResultFileName, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "project-lifecycle-seed-a.txt", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(fileName, "project-lifecycle-seed-b.txt", StringComparison.OrdinalIgnoreCase)) return;
                WriteMarkerAtomic(normalized, new[]
                {
                    "status=FAIL",
                    "command=QS3DLIFECYCLEPROBE",
                    "nonce=" + SafeNonce(),
                    "error_code=" + errorCode
                });
            }
            catch { }
        }

        private static void WriteMarkerAtomic(string resultPath, IEnumerable<string> lines)
        {
            var fullPath = Path.GetFullPath(resultPath);
            if (File.Exists(fullPath)) throw new IOException("The lifecycle marker already exists.");
            var tempPath = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(OneLine(line));
                    writer.Flush();
                    stream.Flush(true);
                }
                File.Move(tempPath, fullPath);
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        private static string OneLine(string value) =>
            (value ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');

        private static bool SkipOutsideAutomation(string? resultPath)
        {
            if (!string.IsNullOrWhiteSpace(resultPath)) return false;
            Application.DocumentManager.MdiActiveDocument?.Editor.WriteMessage(
                "\nQS3D project lifecycle probe skipped: " + ResultVariable + " is not set.");
            return true;
        }

        private static string SafeNonce()
        {
            var nonce = (Environment.GetEnvironmentVariable(NonceVariable) ?? string.Empty).Trim();
            return Guid.TryParseExact(nonce, "N", out _) ? nonce : "invalid";
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private sealed class DetachedProjectStamp
        {
            private readonly long _changeVersion;
            private readonly DateTime _updatedUtc;
            private readonly int _auditCount;
            private readonly int _elementCount;
            private readonly int _familyCount;
            private readonly Dictionary<string, string> _metadata;

            private DetachedProjectStamp(QS3D.Core.Domain.ProjectState project)
            {
                _changeVersion = project.ChangeVersion;
                _updatedUtc = project.UpdatedUtc;
                _auditCount = AuditTrail.ForProject(project).Events.Count;
                _elementCount = project.Elements.Count;
                _familyCount = project.Families.Count;
                _metadata = new Dictionary<string, string>(project.Metadata, StringComparer.OrdinalIgnoreCase);
            }

            public static DetachedProjectStamp Capture(QS3D.Core.Domain.ProjectState project) =>
                new DetachedProjectStamp(project ?? throw new ArgumentNullException(nameof(project)));

            public void EnsureUnchanged(QS3D.Core.Domain.ProjectState project)
            {
                if (project.ChangeVersion != _changeVersion || project.UpdatedUtc != _updatedUtc ||
                    AuditTrail.ForProject(project).Events.Count != _auditCount ||
                    project.Elements.Count != _elementCount || project.Families.Count != _familyCount ||
                    project.Metadata.Count != _metadata.Count ||
                    _metadata.Any(x => !project.Metadata.TryGetValue(x.Key, out var value) || !string.Equals(value, x.Value, StringComparison.Ordinal)))
                    throw new InvalidOperationException("A detached read-only project snapshot was mutated by canonical multi-DWG work.");
            }
        }

        private sealed class LifecycleProbeFailure : InvalidOperationException
        {
            public LifecycleProbeFailure(string errorCode)
                : base("A sanitized lifecycle probe invariant failed.")
            {
                ErrorCode = errorCode;
            }

            public string ErrorCode { get; }
        }
    }
}
