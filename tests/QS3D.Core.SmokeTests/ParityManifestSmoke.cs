using System;
using System.IO;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class ParityManifestSmoke
    {
        internal static void Run()
        {
            var record = new ParityFeatureRecord(
                new FeatureId("BIM.Draw.Rectangle"), " BIM ",
                "BLT3D / BIM / Rectangle", " BIM.Draw.Rectangle ",
                ParityApplicability.Applicable, ParityEvidenceStage.ReferenceCaptured);
            Equal("bim.draw.rectangle", record.FeatureId.ToString());
            Equal("BIM", record.Domain);
            Equal("bim.draw.rectangle", record.WorkflowKey);

            Throws<ArgumentException>(() => new ParityFeatureRecord(
                new FeatureId("host.unsupported"), "Host", "reference", "host.unsupported",
                ParityApplicability.NotApplicableByHostBoundary, ParityEvidenceStage.ReferenceCaptured));

            ClosureRules();
            ParserRules();
            RepositoryManifestBlocksPrematureClosure();
            P2ShellProjectEvidenceRules();
            P3BimEditingModelingEvidenceRules();
            P4RecognitionEvidenceRules();
            P5RebarEvidenceRules();
            P6SpecialCategoryEvidenceRules();
            P7ReviewDocumentationEvidenceRules();
            P8DrawingInteroperabilityEvidenceRules();
            P9AiMcpEvidenceRules();
        }

        private static ParityFeatureRecord Record(string id, ParityEvidenceStage stage) =>
            new ParityFeatureRecord(new FeatureId(id), "BIM", "BLT3D reference", id,
                ParityApplicability.Applicable, stage);

        private static void ClosureRules()
        {
            Throws<InvalidOperationException>(() => new ParityManifest(new[] {
                Record("bim.draw.rectangle", ParityEvidenceStage.ReferenceCaptured),
                Record("BIM.DRAW.RECTANGLE", ParityEvidenceStage.UiPresent)
            }, false));

            var incompleteCatalog = new ParityManifest(new[] {
                Record("bim.draw.rectangle", ParityEvidenceStage.V25V26ParityPass)
            }, false);
            if (incompleteCatalog.GetClosureReport().CanClaimFullParity)
                throw new InvalidOperationException("Incomplete catalog claimed full parity.");

            var uiOnly = new ParityManifest(new[] {
                Record("bim.draw.rectangle", ParityEvidenceStage.UiPresent)
            }, true);
            if (uiOnly.GetClosureReport().CanClaimFullParity)
                throw new InvalidOperationException("UI-only feature claimed full parity.");

            var complete = new ParityManifest(new[] {
                Record("bim.draw.rectangle", ParityEvidenceStage.V25V26ParityPass)
            }, true);
            if (!complete.GetClosureReport().CanClaimFullParity)
                throw new InvalidOperationException("Qualified catalog did not close.");
        }

        private static void ParserRules()
        {
            Throws<FormatException>(() => ParityManifestParser.Parse(new[] {
                "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
                "bim.draw.rectangle\tBIM\treference\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t\t"
            }));

            var parsed = ParityManifestParser.Parse(new[] {
                "# catalog-complete=false",
                "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
                "bim.draw.rectangle\tBIM\tBLT3D / BIM / Rectangle\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t\t"
            });
            if (parsed.CatalogComplete || parsed.Records.Count != 1)
                throw new InvalidOperationException("Deterministic parser lost manifest metadata or row count.");

            var dashed = ParityManifestParser.Parse(new[] {
                "# catalog-complete=false",
                "FeatureId\tDomain\tReferencePath\tWorkflowKey\tApplicability\tEvidenceStage\tDecisionReference\tDecisionReason",
                "bim.draw.rectangle\tBIM\treference\tbim.draw.rectangle\tApplicable\tReferenceCaptured\t-\t-"
            });
            if (dashed.Records[0].DecisionReference != null || dashed.Records[0].DecisionReason != null)
                throw new InvalidOperationException("TSV dash sentinel must normalize optional decision evidence to null.");
        }
        private static void P2ShellProjectEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            var ids = new[] {
                "shell.start", "shell.workspace", "shell.ribbon", "project.setup",
                "project.zone", "project.floor", "project.family"
            };
            foreach (var id in ids)
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(id, record.WorkflowKey);
                Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            }
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P2 must not mark the parity catalog complete.");
        }
        private static void P3BimEditingModelingEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            var ids = new[] { "bim.authoring", "draw", "tool.editing", "modeling" };
            foreach (var id in ids)
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(id, record.WorkflowKey);
                Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            }
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P3 must not mark the parity catalog complete.");
        }
        private static void P4RecognitionEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            var record = manifest.GetRequired(new FeatureId("recognition"));
            Equal("recognition", record.WorkflowKey);
            Equal(ParityApplicability.Applicable, record.Applicability);
            Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P4 must not mark the parity catalog complete.");
        }
        private static void P5RebarEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            var record = manifest.GetRequired(new FeatureId("rebar"));
            Equal("rebar", record.WorkflowKey);
            Equal(ParityApplicability.Applicable, record.Applicability);
            Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P5 must not mark the parity catalog complete.");
        }
        private static void P6SpecialCategoryEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            var wired = new[] { "room", "room.finish", "earthwork", "stair", "railing", "curtain", "door-opening", "grid" };
            foreach (var id in wired)
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(id, record.WorkflowKey);
                Equal(ParityApplicability.Applicable, record.Applicability);
                Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            }
            var referenceOnly = new[] { "pile-cap", "pile-cap.rebar", "pile.drop-to-cap", "steel-detail", "copy-to-level", "blinding-concrete", "opening-to-slab" };
            foreach (var id in referenceOnly)
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(ParityEvidenceStage.ReferenceCaptured, record.EvidenceStage);
            }
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P6 must not mark the parity catalog complete.");
        }
        private static void P7ReviewDocumentationEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            foreach (var id in new[] { "view", "quantity", "revision" })
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(id, record.WorkflowKey);
                Equal(ParityApplicability.Applicable, record.Applicability);
                Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            }
            var drawingManager = manifest.GetRequired(new FeatureId("drawing-manager"));
            Equal(ParityApplicability.Applicable, drawingManager.Applicability);
            // P7 established the minimum reference evidence. Later parity phases may
            // monotonically advance this same canonical row and own the exact stage.
            if (drawingManager.EvidenceStage < ParityEvidenceStage.ReferenceCaptured)
                throw new InvalidOperationException("P7 Drawing Manager evidence regressed below ReferenceCaptured.");
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P7 must not mark the parity catalog complete.");
        }
        private static void P8DrawingInteroperabilityEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            foreach (var id in new[] { "drawing-manager", "ifc" })
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(id, record.WorkflowKey);
                Equal(ParityApplicability.Applicable, record.Applicability);
                Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            }
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P8 must not mark the parity catalog complete.");
        }
        private static void P9AiMcpEvidenceRules()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            foreach (var id in new[] { "ai.luna", "mcp.direct-cad" })
            {
                var record = manifest.GetRequired(new FeatureId(id));
                Equal(id, record.WorkflowKey);
                Equal(ParityApplicability.Applicable, record.Applicability);
                Equal(ParityEvidenceStage.CommandWired, record.EvidenceStage);
            }
            if (manifest.CatalogComplete)
                throw new InvalidOperationException("P9 must not mark the parity catalog complete.");
        }        private static void RepositoryManifestBlocksPrematureClosure()
        {
            var path = Path.Combine("docs", "BLT3D-PARITY-MANIFEST.tsv");
            if (!File.Exists(path)) throw new InvalidOperationException("Missing parity manifest: " + path);
            var manifest = ParityManifestParser.Parse(File.ReadAllLines(path));
            if (manifest.Records.Count < 20) throw new InvalidOperationException("Parity manifest lost approved domain anchors.");
            if (manifest.GetClosureReport().CanClaimFullParity) throw new InvalidOperationException("Seed manifest must not claim full parity.");
        }
        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new InvalidOperationException("Expected " + expected + " but got " + actual + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); } catch (T) { return; }
            throw new InvalidOperationException("Expected " + typeof(T).Name + ".");
        }
    }
}