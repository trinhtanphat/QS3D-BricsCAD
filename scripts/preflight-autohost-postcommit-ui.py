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
        command = text[start:finalize]
        for token in (
            "ReadSelectedHandles(document)",
            "ExistingProjectMutationContext.TryGet(document, out var project)",
            "new OpeningHostMatcher()",
            "ProjectStateSnapshot.Capture(project)",
            "service.LinkOpening(project, item.Opening.Id, item.HostId);",
            "regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;",
            "rollback.Restore(project);",
            "FinalizeAutoHostUi(document, summary);",
        ):
            if token not in command:
                errors.append("Auto Host post-commit boundary missing token: " + token)

        legacy_failure = "ReportAutoHostError(document, ex);"
        redacted_failure = "ReportAutoHostError(document);"
        if legacy_failure not in command and redacted_failure not in command:
            errors.append("Auto Host post-commit boundary missing best-effort business failure reporter")

        regen = command.find("regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;")
        summary = command.find('var summary = "Auto Host: linked="', regen)
        success = command.find("FinalizeAutoHostUi(document, summary);", summary)
        legacy_catch = command.rfind("catch (System.Exception ex)")
        redacted_catch = command.rfind("catch (System.Exception)")
        outer_catch = max(legacy_catch, redacted_catch)
        failure = command.find(legacy_failure, outer_catch)
        if failure < 0:
            failure = command.find(redacted_failure, outer_catch)
        if min(regen, summary, success, outer_catch, failure) < 0 or not regen < summary < success < outer_catch < failure:
            errors.append("Auto Host must finish semantic mutation/regeneration before best-effort summary UI, with business failures routed separately")

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
            "try { PaletteCoordinator.RefreshProject(); }",
            "try { PaletteCoordinator.SetStatus(summary); }",
            "try { document.Editor.WriteMessage(",
        ):
            if token not in success_helper:
                errors.append("FinalizeAutoHostUi missing best-effort token: " + token)
        if "Cảnh báo UI sau Auto Host commit" not in success_helper and "PostCommitUiWarning" not in success_helper:
            errors.append("FinalizeAutoHostUi missing committed-state UI warning")
        if "throw" in success_helper:
            errors.append("FinalizeAutoHostUi must not throw after committed Auto Host mutation")

        error_helper = text[report:single]
        for token in (
            "try { PaletteCoordinator.SetStatus(message); }",
            "try { document.Editor.WriteMessage(",
        ):
            if token not in error_helper:
                errors.append("ReportAutoHostError missing best-effort failure-report token: " + token)
        legacy_message = 'var message = "QS3DAUTOLINKHOSTS lỗi: " + error.Message;'
        redacted_message = "var message = OperationFailure;"
        if legacy_message not in error_helper and redacted_message not in error_helper:
            errors.append("ReportAutoHostError missing supported legacy/redacted message construction")
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

print("PASS: Auto Host keeps matching/rollback/regeneration semantics intact while committed batch results and business failures use non-throwing UI/reporting boundaries across legacy and redacted reporter shapes.")
