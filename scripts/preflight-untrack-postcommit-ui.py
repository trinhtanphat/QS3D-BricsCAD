#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ViewportCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ViewportCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find("private static void UntrackSelectedElements")
    finalize = text.find("private static void FinalizeUntrackUi", start)
    report = text.find("private static void ReportUntrackError", finalize)
    next_method = text.find("private static void EnsureTiledModelSpace", report)
    if min(start, finalize, report, next_method) < 0 or not start < finalize < report < next_method:
        errors.append("cannot isolate semantic untrack command/UI helpers")
    else:
        command = text[start:finalize]
        required = (
            "EntitySnapshotReader.ReadImpliedSelection(doc)",
            "var handles = snapshots.Select(x => x.Handle).ToArray();",
            "SemanticUntrackResult result;",
            'ExistingProjectMutationContext.Require(doc, "Untrack semantic elements")',
            "result = SemanticUntrackService.Untrack(project, handles, predicate);",
            "catch (Exception)",
            "ReportUntrackError(doc, label);",
            "return;",
            "FinalizeUntrackUi(doc, result.Count, label);",
        )
        for token in required:
            if token not in command:
                errors.append("Untrack command missing post-commit boundary token: " + token)

        bind = command.find('ExistingProjectMutationContext.Require(doc, "Untrack semantic elements")')
        mutate = command.find("result = SemanticUntrackService.Untrack(project, handles, predicate);")
        catch = command.find("catch (Exception)", mutate)
        failure = command.find("ReportUntrackError(doc, label);", catch)
        ret = command.find("return;", failure)
        success = command.find("FinalizeUntrackUi(doc, result.Count, label);", ret)
        if min(bind, mutate, catch, failure, ret, success) < 0 or not bind < mutate < catch < failure < ret < success:
            errors.append("Untrack must keep bind/mutation in business try, return on business failure, then finalize UI after the try/catch")

        try_start = command.find("try", command.find("SemanticUntrackResult result;"))
        try_catch = command.find("catch (Exception)", try_start)
        if try_start >= 0 and try_catch > try_start:
            mutation_try = command[try_start:try_catch]
            for forbidden in (
                "PaletteCoordinator.RefreshProject",
                "PaletteCoordinator.SetStatus",
                "Editor.WriteMessage",
                "FinalizeUntrackUi",
            ):
                if forbidden in mutation_try:
                    errors.append("post-commit UI must not remain inside Untrack business try: " + forbidden)

        success_helper = text[finalize:report]
        for token in (
            "try { PaletteCoordinator.RefreshProject(); }",
            "try { PaletteCoordinator.SetStatus(status); }",
            "try { document.Editor.WriteMessage(",
            "Cảnh báo UI sau untrack commit",
        ):
            if token not in success_helper:
                errors.append("FinalizeUntrackUi missing best-effort token: " + token)
        if "throw" in success_helper:
            errors.append("FinalizeUntrackUi must not throw after committed semantic mutation")

        error_helper = text[report:next_method]
        for token in (
            'var message = "Không thể bỏ theo dõi " + label + ". Vui lòng thử lại.";',
            "try { PaletteCoordinator.SetStatus(message); }",
            "try { document.Editor.WriteMessage(",
        ):
            if token not in error_helper:
                errors.append("ReportUntrackError missing best-effort redacted failure-report token: " + token)
        if ".Message" in error_helper:
            errors.append("ReportUntrackError must not expose raw exception detail")
        if "throw" in error_helper:
            errors.append("ReportUntrackError must not throw while reporting business failure")

    for command_name in ("QS3DUNTRACK", "QS3DUNTRACKFINISH"):
        if ('CommandMethod("' + command_name + '"') not in text:
            errors.append("missing semantic untrack command owner: " + command_name)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: semantic untrack keeps business mutation failures separate from best-effort redacted post-commit palette/editor finalization for both general and finish untrack commands.")
