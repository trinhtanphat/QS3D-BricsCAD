from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Services" / "HostLinkService.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "HostLinkRelationshipIdentityIntegritySmoke.cs"
RUNBOOK = ROOT / "docs" / "FEATURE-RUNBOOKS" / "host-link-relationship-identity-integrity.md"

errors = []


def read(path: Path) -> str:
    if not path.exists():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(label + " missing token: " + token)


source = read(SOURCE)
smoke = read(SMOKE)
runbook = read(RUNBOOK)

for token in (
    "using System.Xml;",
    "ValidateCanonicalDependencyIds(opening);",
    "ValidateRelationshipIdentityText(rawHostId",
    "ValidateRelationshipIdentityText(dependencyId",
    "XmlConvert.VerifyXmlChars(value);",
    "contains control characters",
    "contains malformed UTF-16 or XML-invalid characters",
):
    require(source, token, "HostLinkService")

if source.count("ValidateCanonicalDependencyIds(opening);") < 2:
    errors.append("HostLinkService must validate opening dependencies before both link and unlink mutation paths")

link_guard = source.find("ValidateCanonicalDependencyIds(opening);")
link_mutation = source.find("ProjectSemanticMutationExecutor.Execute(project, \"host.link\"")
if link_guard < 0 or link_mutation < 0 or link_guard > link_mutation:
    errors.append("HostLinkService dependency identity guard must precede host.link semantic mutation")

for token in (
    "LinkRejectsMalformedNonMatchingDependencyBeforeMutation",
    "LinkRejectsControlBearingDependencyBeforeMutation",
    "LinkRejectsMalformedHostGraphDependencyBeforeMutation",
    "UnlinkRejectsMalformedPersistedHostIdBeforeMutation",
    "CallerIdentityRejectsHostileTextAndPreservesCanonicalLookup",
    "\\uD800",
    "\\uDC00",
    "\\uFFFF",
    "\\u0001",
):
    require(smoke, token, "HostLinkRelationshipIdentityIntegritySmoke")

for token in (
    "Lane-Key: `issue-5226`",
    "malformed UTF-16",
    "control characters",
    "preflight + core",
):
    require(runbook, token, "host-link relationship identity runbook")

if errors:
    for error in errors:
        print("ERROR: " + error)
    raise SystemExit(1)

print("PASS host-link relationship identity integrity preflight")
