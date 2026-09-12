#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DAUTOLINKHOSTS", CommandFlags.UsePickSet)]')
    finalize = text.find("private static void FinalizeAutoHostUi", start)
    report = text.find("private static void ReportAutoHostError", finalize)
    single = text.find("internal static string LinkSingleOpening", report)
    if min(start, finalize, report, single) < 0 or not start < finalize < report < single:
        errors.append("cannot isolate Auto Host command/UI helpers")
    else:
        affinity = text.find("private static bool IsActiveDocumentGeneration", start)
        command_end = affinity if start < affinity < finalize else finalize
        command = text[start:command_end]
        for token in (
            "nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
            "ReadSelectedHandles(document)",
            "ExistingProjectMutationContext.TryGet(document, out var project)",
            "new OpeningHostMatcher()",
            "ProjectStateSnapshot.Capture(project)",
            "service.LinkOpening(project, item.Opening.Id, item.HostId);",
            "regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;",
            "rollback.Restore(project);",
            "FinalizeAutoHostUi(document, nativeDatabaseIdentity, summary);",
        ):
            if token not in command:
                errors.append("Auto Host post-commit boundary missing token: " + token)

        redacted_failure = "ReportAutoHostError(document, nativeDatabaseIdentity);"
        if redacted_failure not in command:
            errors.append("Auto Host post-commit boundary missing generation-bound best-effort business failure reporter")

        regen = command.find("regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;")
        summary = command.find('var summary = "Auto Host: linked="', regen)
        success = command.find("FinalizeAutoHostUi(document, nativeDatabaseIdentity, summary);", summary)
        outer_catch = command.rfind("catch (System.Exception)")
        failure = command.find(redacted_failure, outer_catch)
        if min(regen, summary, success, outer_catch, failure) < 0 or not regen < summary < success < outer_catch < failure:
            errors.append("Auto Host must finish semantic mutation/regeneration before generation-bound best-effort summary UI, with business failures routed separately")

        after_regen = command[regen:]
        for forbidden in (
            "PaletteCoordinator.RefreshProject();",
            "PaletteCoordinator.SetStatus(summary);",
            'document.Editor.WriteMessage("\\nQS3D " + summary',
        ):
            if forbidden in after_regen:
                errors.append("Auto Host must not perform direct fallible post-commit UI in the outer business path: " + forbidden)

        success_helper = text[finalize:report]
        for token in (
            "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
            "try { TryRefreshProject(document, nativeDatabaseIdentity); }",
            "try { TrySetPaletteStatus(document, nativeDatabaseIdentity, summary); }",
            "if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
            "document.Editor.WriteMessage(",
        ):
            if token not in success_helper:
                errors.append("FinalizeAutoHostUi missing generation-safe best-effort token: " + token)
        if "PostCommitUiWarning" not in success_helper:
            errors.append("FinalizeAutoHostUi missing committed-state UI warning")
        if "throw" in success_helper:
            errors.append("FinalizeAutoHostUi must not throw after committed Auto Host mutation")

        error_helper = text[report:single]
        for token in (
            "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
            "try { TrySetPaletteStatus(document, nativeDatabaseIdentity, message); }",
            "if (IsActiveDocumentGeneration(document, nativeDatabaseIdentity))",
            "document.Editor.WriteMessage(",
        ):
            if token not in error_helper:
                errors.append("ReportAutoHostError missing generation-safe best-effort failure-report token: " + token)
        if "var message = OperationFailure;" not in error_helper:
            errors.append("ReportAutoHostError missing redacted message construction")
        if "throw" in error_helper:
            errors.append("ReportAutoHostError must not throw while reporting business failure")

        single_body = text[single:]
        for token in (
            "new HostLinkService().LinkOpening(project, opening.Id, match.HostElementId);",
            "if (UpdateAutoHostMetadata(opening, match.GapM)) project.Touch();",
            "return match.HostElementId;",
        ):
            if token not in single_body:
                errors.append("LinkSingleOpening lifecycle must remain unchanged: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Auto Host keeps matching/rollback/regeneration semantics intact while committed batch results and business failures use exact document/database-generation non-throwing UI/reporting boundaries.")
