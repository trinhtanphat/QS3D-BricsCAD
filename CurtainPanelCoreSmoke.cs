using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using QS3D.Core.Diagnostics;
using QS3D.Core.Domain;
using QS3D.Core.Geometry;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainPanelCoreSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OpeningClipsPanelsDeterministically();
            BoundsFailBeforePlanning();
            FingerprintIsOrderIndependentAndConfigurationSensitive();
            OwnershipHealthReleaseAndStaleAreIntegrated();
            CompletedEmptyBuildRemainsDiagnosableAndStaleable();
            LinkedOpeningChangesMarkPanelsStale();
        }

        private static void OpeningClipsPanelsDeterministically()
        {
            var panels = new[] { new CurtainWallRect(0d, 0d, 2d, 3d), new CurtainWallRect(2d, 0d, 2d, 3d) };
            var openings = new[] { new CurtainWallOpeningRect { X_M = 1d, Z_M = 0.5d, WidthM = 2d, HeightM = 2d } };
            var first = CurtainWallOpeningPanelPlanner.Plan(panels, openings);
            var second = CurtainWallOpeningPanelPlanner.Plan(panels.AsEnumerable().Reverse().Reverse().ToArray(), openings);
            Equal(6, first.Pieces.Count);
            Equal(2, first.InterruptedPanelCount);
            Near(4d, first.RemovedPanelAreaM2);
            Equal(Signature(first.Pieces), Signature(second.Pieces));
            foreach (var piece in first.Pieces)
                Require(!Overlaps(piece, openings[0]), "planned glass overlaps an opening");
        }

        private static void BoundsFailBeforePlanning()
        {
            var panels = Enumerable.Repeat(new CurtainWallRect(0d, 0d, 1d, 1d), CurtainWallOpeningPanelPlanner.MaxInputPanels + 1).ToArray();
            Throws<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(panels, Array.Empty<CurtainWallOpeningRect>()));
            var openings = Enumerable.Repeat(new CurtainWallOpeningRect { X_M = 0d, Z_M = 0d, WidthM = 1d, HeightM = 1d }, CurtainWallOpeningPanelPlanner.MaxOpenings + 1).ToArray();
            Throws<InvalidOperationException>(() => CurtainWallOpeningPanelPlanner.Plan(new[] { new CurtainWallRect(0d, 0d, 1d, 1d) }, openings));
        }

        private static void FingerprintIsOrderIndependentAndConfigurationSensitive()
        {
            var pieces = new[]
            {
                Piece(1, 2d, 0d, 1d, 1d),
                Piece(0, 0d, 0d, 1d, 1d)
            };
            var a = Fingerprint(pieces, 0.02d);
            var b = Fingerprint(pieces.AsEnumerable().Reverse().ToArray(), 0.02d);
            Equal(a, b);
            Require(a.Length == 64 && a.All(Uri.IsHexDigit), "fingerprint is not SHA-256 hex");
            Require(!string.Equals(a, Fingerprint(pieces, 0.03d), StringComparison.Ordinal), "panel depth did not affect fingerprint");
        }

        private static void OwnershipHealthReleaseAndStaleAreIntegrated()
        {
            var project = new ProjectState("CURTAIN-PANEL", "Curtain panel");
            var curtain = PanelElement("CW-1", "AA;AB");
            project.Elements.Add(curtain);
            Require(new GeneratedCurtainPanelHealthService().Inspect(project, Live("AA", "AB")).Count == 0, "healthy panel metadata produced issues");

            curtain.MarkGeneratedCurtainPanelStale("smoke");
            Require(curtain.IsGeneratedCurtainPanelStale(), "panel stale state was not retained");
            Require(new GeneratedGeometryStaleHealthService().Inspect(project).Any(x => x.Code == "CURTAIN_PANEL_GENERATED_STALE"), "aggregate stale health missed panels");
            Require(BomReleaseGuardService.Inspect(project, Live("AA", "AB")).Any(x => x.Code == "CURTAIN_PANEL_GENERATED_STALE"), "release guard missed stale panels");
            curtain.ClearGeneratedCurtainPanelStale();
            Require(!curtain.IsGeneratedCurtainPanelStale(), "panel stale state did not clear independently");

            var conflict = new ProjectElement("OTHER", ElementCategory.Beam);
            conflict.Properties["GeneratedSolidHandle"] = "AA";
            project.Elements.Add(conflict);
            Require(new GeneratedCurtainPanelHealthService().Inspect(project).Any(x => x.Code == "CURTAIN_PANEL_GENERATED_OWNERSHIP_CONFLICT"), "panel ownership conflict was not detected");
            Require(new GeneratedCurtainPanelHealthService().Inspect(project, Live("AA")).Any(x => x.Code == "CURTAIN_PANEL_GENERATED_SOLID_MISSING"), "missing live panel was not detected");
        }

        private static void LinkedOpeningChangesMarkPanelsStale()
        {
            var project = new ProjectState("CURTAIN-PANEL-LINK", "Curtain panel link");
            var curtain = PanelElement("CW-2", "BA;BB");
            var opening = new ProjectElement("O-1", ElementCategory.WallOpening);
            opening.Properties["WidthM"] = "1";
            opening.Properties["HeightM"] = "2";
            project.Elements.Add(curtain);
            project.Elements.Add(opening);
            new HostLinkService().LinkOpening(project, opening.Id, curtain.Id);
            Require(curtain.IsGeneratedCurtainPanelStale(), "linking an opening did not stale panels");
            curtain.ClearGeneratedCurtainPanelStale();
            new OpeningRegenerator().Regenerate(project, opening);
            Require(curtain.IsGeneratedCurtainPanelStale(), "regenerating a linked opening did not stale panels");
        }

        private static void CompletedEmptyBuildRemainsDiagnosableAndStaleable()
        {
            var plan = CurtainWallOpeningPanelPlanner.Plan(
                new[] { new CurtainWallRect(0d, 0d, 1d, 1d) },
                new[] { new CurtainWallOpeningRect { X_M = 0d, Z_M = 0d, WidthM = 1d, HeightM = 1d } });
            Equal(0, plan.Pieces.Count);

            var project = new ProjectState("CURTAIN-PANEL-EMPTY", "Curtain panel empty");
            var element = PanelElement("CW-EMPTY", string.Empty);
            element.Properties["GeneratedCurtainPanelCount"] = "0";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "1";
            element.Properties["GeneratedCurtainPanelColumns"] = "1";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "1";
            element.Properties["GeneratedCurtainPanelAreaM2"] = "0";
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids.OpeningAware";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = Fingerprint(Array.Empty<CurtainWallPanelPiece>(), 0.02d);
            project.Elements.Add(element);
            Require(new GeneratedCurtainPanelHealthService().Inspect(project).Count == 0, "completed-empty panel build produced health issues");
            element.MarkGeneratedCurtainPanelStale("opening changed");
            Require(element.IsGeneratedCurtainPanelStale(), "completed-empty panel build could not become stale");
            Require(BomReleaseGuardService.Inspect(project).Any(x => x.Code == "CURTAIN_PANEL_GENERATED_STALE"), "release guard missed completed-empty stale build");

            element.ClearGeneratedCurtainPanelStale();
            element.Properties["GeneratedCurtainPanelMode"] = "PathPanelSolids.OpeningAware";
            element.Properties["GeneratedCurtainPanelSourceKind"] = "OpenPolyline";
            element.Properties["GeneratedCurtainPanelPathSegmentCount"] = "2";
            element.Properties["GeneratedCurtainPanelMappedCount"] = "0";
            element.Properties["GeneratedCurtainPanelPathSagittaM"] = "0.002";
            Require(new GeneratedCurtainPanelHealthService().Inspect(project).Count == 0, "completed-empty path panel build produced health issues");
        }

        private static ProjectElement PanelElement(string id, string handles)
        {
            var element = new ProjectElement(id, ElementCategory.GlassWall);
            element.Properties[GeneratedCurtainPanelHealthService.HandlesKey] = handles;
            element.Properties[GeneratedCurtainPanelHealthService.BuildStateKey] = GeneratedCurtainPanelHealthService.BuildCompleteValue;
            element.Properties["GeneratedCurtainPanelCount"] = "2";
            element.Properties["GeneratedCurtainPanelBaseCount"] = "2";
            element.Properties["GeneratedCurtainPanelOpeningCount"] = "0";
            element.Properties["GeneratedCurtainPanelColumns"] = "2";
            element.Properties["GeneratedCurtainPanelRows"] = "1";
            element.Properties["GeneratedCurtainPanelDepthM"] = "0.02";
            element.Properties["GeneratedCurtainPanelSourceLengthM"] = "4";
            element.Properties["GeneratedCurtainPanelHeightM"] = "3";
            element.Properties["GeneratedCurtainPanelAreaM2"] = "2";
            element.Properties["GeneratedCurtainPanelConfigFingerprint"] = Fingerprint(new[] { Piece(0, 0d, 0d, 1d, 1d), Piece(1, 2d, 0d, 1d, 1d) }, 0.02d);
            element.Properties["GeneratedCurtainPanelMode"] = "LinePanelSolids";
            return element;
        }

        private static string Fingerprint(IReadOnlyList<CurtainWallPanelPiece> pieces, double depth) =>
            CurtainWallPanelFingerprint.Compute(new CurtainWallPanelFingerprintInput
            {
                SourceLengthM = 4d,
                HeightM = 3d,
                BottomOffsetM = 0d,
                PanelDepthM = depth,
                SourceKind = "Line",
                Pieces = pieces
            });

        private static CurtainWallPanelPiece Piece(int source, double x, double z, double width, double height) =>
            new CurtainWallPanelPiece { SourcePanelIndex = source, X_M = x, Z_M = z, WidthM = width, HeightM = height };

        private static HashSet<string> Live(params string[] handles) => new HashSet<string>(handles, StringComparer.OrdinalIgnoreCase);
        private static string Signature(IEnumerable<CurtainWallPanelPiece> pieces) => string.Join("|", pieces.Select(x => x.SourcePanelIndex + ":" + x.X_M + "," + x.Z_M + "," + x.WidthM + "," + x.HeightM));
        private static bool Overlaps(CurtainWallPanelPiece p, CurtainWallOpeningRect o) => Math.Min(p.X_M + p.WidthM, o.X_M + o.WidthM) > Math.Max(p.X_M, o.X_M) + 1e-9d && Math.Min(p.Z_M + p.HeightM, o.Z_M + o.HeightM) > Math.Max(p.Z_M, o.Z_M) + 1e-9d;
        private static void Near(double expected, double actual) { if (Math.Abs(expected - actual) > 1e-9d) throw new InvalidOperationException("Expected " + expected + ", got " + actual + "."); }
        private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + ", got " + actual + "."); }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException("CurtainPanelCoreSmoke: " + message); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name + "."); }
    }
}
