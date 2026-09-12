from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "AtomicFileCommit.cs"

text = SOURCE.read_text(encoding="utf-8")
start = text.find("public static void PublishNew")
end = text.find("public static void TryDelete", start)
if start < 0 or end < 0:
    print("ERROR: AtomicFileCommit.PublishNew boundary not found.")
    sys.exit(1)

publish_new = text[start:end]
move_token = "File.Move(temp, destination);"
move = publish_new.find(move_token)
if move < 0:
    print("ERROR: AtomicFileCommit.PublishNew create-new File.Move was not found.")
    sys.exit(1)

post_publish = publish_new[move + len(move_token):]

# Once the primary has been installed, every fail-closed backup-path revalidation
# failure is itself a publication failure and must own rollback of that primary.
# A naked RequireSafe(backup) after File.Move can otherwise throw while leaving the
# new primary committed even though PublishNew reports failure to its caller.
required_tokens = (
    'RequireSafe(backup, "backup");',
    "catch (Exception publicationFailure) when",
    'RequireSafe(destination, "destination");',
    "File.Delete(destination);",
    "RecordRollbackFailure(publicationFailure, rollbackFailure);",
    "throw;",
)
missing = [token for token in required_tokens if token not in post_publish]
if missing:
    print(
        "ERROR: AtomicFileCommit.PublishNew does not rollback a published primary "
        "when post-publication backup safety validation fails: " + ", ".join(missing)
    )
    sys.exit(1)

safety = post_publish.find('RequireSafe(backup, "backup");')
publication_catch = post_publish.find("catch (Exception publicationFailure) when")
rollback_delete = post_publish.find("File.Delete(destination);", publication_catch)
rollback_evidence = post_publish.find(
    "RecordRollbackFailure(publicationFailure, rollbackFailure);", publication_catch
)
bare_rethrow = post_publish.find("throw;", rollback_evidence)
if (
    safety < 0
    or publication_catch < 0
    or rollback_delete < 0
    or rollback_evidence < 0
    or bare_rethrow < 0
    or not (safety < publication_catch < rollback_delete < rollback_evidence < bare_rethrow)
):
    print(
        "ERROR: AtomicFileCommit.PublishNew backup-safety rollback ordering is not "
        "fail-closed after the create-new File.Move."
    )
    sys.exit(1)

if "throw publicationFailure;" in post_publish:
    print(
        "ERROR: AtomicFileCommit.PublishNew must preserve the original backup-safety "
        "failure stack with bare throw;."
    )
    sys.exit(1)

# The original post-publish backup-appearance rollback remains a separate contract;
# this guard must not allow the safety fix to remove that conflict rollback path.
for token in (
    "File.Exists(backup)",
    "Directory.Exists(backup)",
    "A QS3D backup appeared during create-new publication",
):
    if token not in post_publish:
        print(
            "ERROR: AtomicFileCommit.PublishNew lost its post-publication backup "
            "appearance rollback contract: " + token
        )
        sys.exit(1)

recreate_start = text.find("private static void PublishMissingDestinationWithoutStaleBackup")
recreate_end = text.find("private static void MoveWithRecovery", recreate_start)
if recreate_start < 0 or recreate_end < 0:
    print("ERROR: AtomicFileCommit missing-destination recreation boundary not found.")
    sys.exit(1)

recreate = text[recreate_start:recreate_end]
recreate_move = recreate.find("File.Move(tempPath, destinationPath);")
if recreate_move < 0:
    print("ERROR: AtomicFileCommit missing-destination create-new File.Move was not found.")
    sys.exit(1)

recreate_post_publish = recreate[recreate_move + len("File.Move(tempPath, destinationPath);"):]

# Missing-primary recreation has the same post-publication safety boundary, plus a
# staged older backup that must not be discarded. A safety failure must transfer
# rollback ownership before deleting the new primary; outer finally can then restore
# the staged backup and attach any secondary recovery failure to the primary error.
recreate_required = (
    'RequireSafe(backupPath, "backup");',
    "catch (Exception publicationFailure) when",
    "installed = false;",
    'RequireSafe(destinationPath, "destination");',
    "File.Delete(destinationPath);",
    "RecordRollbackFailure(publicationFailure, rollbackFailure);",
    "throw;",
)
missing = [token for token in recreate_required if token not in recreate_post_publish]
if missing:
    print(
        "ERROR: AtomicFileCommit missing-destination recreation does not rollback "
        "a newly installed primary when backup safety validation fails: " + ", ".join(missing)
    )
    sys.exit(1)

recreate_safety = recreate_post_publish.find('RequireSafe(backupPath, "backup");')
recreate_catch = recreate_post_publish.find("catch (Exception publicationFailure) when")
recreate_owner = recreate_post_publish.find("installed = false;", recreate_catch)
recreate_delete = recreate_post_publish.find("File.Delete(destinationPath);", recreate_owner)
recreate_evidence = recreate_post_publish.find(
    "RecordRollbackFailure(publicationFailure, rollbackFailure);", recreate_delete
)
recreate_rethrow = recreate_post_publish.find("throw;", recreate_evidence)
if (
    recreate_safety < 0
    or recreate_catch < 0
    or recreate_owner < 0
    or recreate_delete < 0
    or recreate_evidence < 0
    or recreate_rethrow < 0
    or not (
        recreate_safety
        < recreate_catch
        < recreate_owner
        < recreate_delete
        < recreate_evidence
        < recreate_rethrow
    )
):
    print(
        "ERROR: AtomicFileCommit missing-destination backup-safety rollback ordering "
        "does not transfer ownership before cleanup and staged-backup restoration."
    )
    sys.exit(1)

if "throw publicationFailure;" in recreate_post_publish:
    print(
        "ERROR: AtomicFileCommit missing-destination recreation must preserve the "
        "original backup-safety failure stack with bare throw;."
    )
    sys.exit(1)

print("AtomicFileCommit PublishNew backup-safety rollback preflight passed.")
