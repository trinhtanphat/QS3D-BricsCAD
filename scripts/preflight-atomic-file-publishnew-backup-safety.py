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

print("AtomicFileCommit PublishNew backup-safety rollback preflight passed.")
