#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
VIEW_MODEL = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/QuantityInsightViewModel.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
GEOMETRY = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.Geometry.cs"
EXACT_FACE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.DetailExplainer.ExactFace.cs"
PRESENTATION = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.FoundationExactFacePresentation.cs"
RAFT = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.RaftSemanticFaces.cs"


def read(path):
    if not path.is_file():
        raise SystemExit("ERROR: missing required source file: " + str(path.relative_to(ROOT)))
    return path.read_text(encoding="utf-8")


def require(text, needle, label, failures):
    if needle not in text:
        failures.append(label + ": missing " + repr(needle))


def forbid(text, needle, label, failures):
    if needle in text:
        failures.append(label + ": forbidden " + repr(needle))


def main():
    view_model = read(VIEW_MODEL)
    xaml = read(XAML)
    geometry = read(GEOMETRY)
    exact_face = read(EXACT_FACE)
    presentation = read(PRESENTATION)
    raft = read(RAFT)
    failures = []

    require(xaml, "Floor / Type / Name / Element", "canonical tree vocabulary", failures)
    require(view_model, 'public string Header => "Floor: "', "Floor node vocabulary", failures)
    require(view_model, 'public string Header => "Type: "', "Type node vocabulary", failures)
    require(view_model, 'public string Header => "Name: "', "Name node vocabulary", failures)

    require(geometry, "face.FaceId", "renderer carries exact FaceId envelope", failures)
    require(geometry, "face.SemanticKey", "renderer carries semantic display authority", failures)
    require(raft, 'sideFaces[index].SemanticKey = "Side:OuterLoop:Edge"', "raft semantic face authority", failures)

    require(presentation, "FrameworkElement.LoadedEvent", "loaded-row presentation mapping", failures)
    require(presentation, "textBlock.Tag = new QuantityExactFacePresentationTarget(faceId);", "FaceId stored outside display text", failures)
    require(presentation, "TryGetFoundationQuantityExactFaceIdentity", "metadata identity resolver", failures)
    require(presentation, "TryResolveFoundationQuantityExactFaceButton", "exact-face value button metadata resolver", failures)
    require(presentation, "Side:OuterLoop:Edge", "raft semantic label mapping", failures)
    require(presentation, "Mặt bên ngoài • Cạnh ", "friendly raft exact-face label", failures)
    require(presentation, 'return typeLabel + " • " + faceId;', "deterministic technical fallback", failures)
    require(presentation, "panel.LocateQuantityExactFace(faceId);", "metadata FaceId locate wiring", failures)
    forbid(presentation, "LocateQuantityExactFace(displayText", "display label used as runtime identity", failures)
    forbid(presentation, "LocateQuantityExactFace(semantic", "semantic label used as runtime identity", failures)

    require(exact_face, "TryParseQuantityExactFaceId", "exact native FaceId parser remains authoritative", failures)
    require(exact_face, "TryRevalidateQuantityGeometry", "click-time exact BREP revalidation", failures)
    require(exact_face, "SameQuantityExactFace", "displayed/fresh exact-face parity", failures)

    if failures:
        print("QS3D Foundation Quantity Insight parity preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: Foundation Quantity Insight uses Floor/Type/Name/Element vocabulary and friendly exact-face presentation while FaceId remains the runtime locate/highlight identity.")
    print("NOTE: source-level guard only; real native BricsCAD V25 face highlighting remains LOCAL_ONLY.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
