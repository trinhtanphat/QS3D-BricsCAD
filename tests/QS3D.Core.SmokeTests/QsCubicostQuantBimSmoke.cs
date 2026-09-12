using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using QS3D.Core.BenchmarkParity;

namespace QS3D.Core.SmokeTests
{
    internal static class QsCubicostQuantBimSmoke
    {
        internal static void Run()
        {
            CubicostReviewAndEvidence();
            QuantBimStandaloneWorkflow();
            QuantBimIfcStepIngestion();
        }

        private static void CubicostReviewAndEvidence()
        {
            var components = new[]
            {
                new RecognizedQsComponent("C1", "Beam", "STR.BEAM", "L01", 5d, 0.3d, 0.6d, ComponentSourceKind.DrawingRecognition, new QuantityEvidence("M1", "A101.pdf#M1", "R2", "calibrated-polyline", 0.96d)),
                new RecognizedQsComponent("C2", "Column", "STR.COLUMN", "L01", 3d, 0.4d, 0.4d, ComponentSourceKind.IfcModel, new QuantityEvidence("G2", "model.ifc#G2", "R2", "ifc-geometry", 1d)),
                new RecognizedQsComponent("C3", "Beam", "STR.BEAM", "L02", 4d, 0.25d, 0.5d, ComponentSourceKind.DrawingRecognition, new QuantityEvidence("M3", "A201.pdf#M3", "R2", "component-recognition", 0.71d))
            };
            var reviews = new[]
            {
                new ComponentReviewDecision("C1", ComponentRecognitionStatus.Accepted, "qs@example", "checked against grid", null, null, null),
                new ComponentReviewDecision("C2", ComponentRecognitionStatus.Corrected, "qs@example", "column depth corrected", 3d, 0.45d, 0.4d),
                new ComponentReviewDecision("C3", ComponentRecognitionStatus.Rejected, "qs@example", "false positive", null, null, null)
            };
            var workflow = new CubicostConcreteFormworkWorkflow();
            var lines = workflow.Quantify(components, reviews, false);
            Equal(2, lines.Count, "accepted component count");
            Equal(ComponentRecognitionStatus.Corrected, lines.Single(x => x.ComponentId == "C2").ReviewStatus, "corrected review status");
            Near(0.54d, lines.Single(x => x.ComponentId == "C2").ConcreteVolume, 1e-12, "corrected column volume");
            Equal("A101.pdf#M1", lines.Single(x => x.ComponentId == "C1").Evidence.SourceReference, "trace evidence");
            var inventory = workflow.BuildInventory(lines);
            True(inventory.Any(x => x.Classification == "STR.BEAM.CONCRETE" && x.Unit == "m3"), "concrete inventory");
            True(inventory.Any(x => x.Classification == "STR.COLUMN.FORMWORK" && x.Unit == "m2"), "formwork inventory");
        }

        private static void QuantBimStandaloneWorkflow()
        {
            var source = new FakeIfcSource();
            var workbench = new QuantBimStandaloneWorkbench(source);
            var document = workbench.Open("sample.ifc");
            Equal("R5", document.Revision, "standalone revision");
            var filtered = workbench.Filter(document, new IfcWorkbenchFilter("IfcWall", "L01", "External", "ARC.WALL"));
            Equal(1, filtered.Count, "standalone IFC filter");
            var tree = workbench.PropertyTree(filtered[0]);
            True(tree.Any(x => x.Name == "Identity.Guid" && x.Value == "G1"), "property tree identity");
            True(tree.Any(x => x.Name == "Pset_WallCommon.IsExternal" && x.Value == "TRUE"), "property tree pset");
            var selection = new IfcSelectionSet("External walls", filtered.Select(x => x.Guid));
            var takeoff = workbench.Takeoff(document, selection);
            Equal(2, takeoff.Count, "selection takeoff quantities");
            var boq = workbench.BuildBoq(takeoff);
            Near(12d, boq.Single(x => x.Classification == "ARC.WALL" && x.Unit == "m2").Quantity, 1e-12, "BOQ area");
            Near(2.4d, boq.Single(x => x.Classification == "ARC.WALL" && x.Unit == "m3").Quantity, 1e-12, "BOQ volume");
            var csv = workbench.ExportCsv(boq);
            True(csv.Contains("ARC.WALL,m2,12"), "CSV export");
            var navigation = workbench.DefaultNavigation(selection);
            Equal(IfcViewCommandKind.FocusSelection, navigation.Last().Kind, "viewer focus command");

            var evidenceEngine = new QuantBimTraceableTakeoffEngine();
            var evidence = evidenceEngine.Build(document, selection);
            Equal(2, evidence.Count, "traceable evidence count");
            True(evidence.All(x => x.Revision == "R5"), "traceable revision");
            True(evidence.All(x => x.GeometryReference == "mesh://G1"), "traceable geometry");
            True(evidence.Any(x => x.QuantityName == "NetSideArea" && x.Unit == "m2" && Math.Abs(x.Quantity - 12d) < 1e-12), "traceable quantity item");
            var traceableBoq = evidenceEngine.BuildBoq(evidence);
            Near(12d, traceableBoq.Single(x => x.Classification == "ARC.WALL" && x.Unit == "m2").Quantity, 1e-12, "traceable BOQ area");
            var evidenceCsv = evidenceEngine.ExportEvidenceCsv(evidence);
            True(evidenceCsv.Contains("DocumentPath,Revision,Guid,Entity,Storey,Classification,QuantityName,Quantity,Unit,GeometryReference"), "evidence CSV header");
            True(evidenceCsv.Contains("sample.ifc,R5,G1,IfcWall,L01,ARC.WALL,NetSideArea,12,m2,mesh://G1"), "evidence CSV provenance");

            var unknownSelectionRejected = false;
            try
            {
                evidenceEngine.Build(document, new IfcSelectionSet("Invalid", new[] { "NOT-IN-MODEL" }));
            }
            catch (InvalidOperationException)
            {
                unknownSelectionRejected = true;
            }
            True(unknownSelectionRejected, "unknown selection fails closed");

            var mismatchedDocument = new IfcStandaloneDocument("bad.ifc", "R1", new[]
            {
                new IfcStandaloneElement("G9", "IfcWall", "Wall-09", "L01", "External", "ARC.WALL", Array.Empty<IfcPropertyNode>(), new[]
                {
                    new IfcQtoItem("OTHER", "IfcWall", "L01", "ARC.WALL", "NetSideArea", 1d, "m2")
                }, "mesh://G9")
            });
            var mismatchedQuantityRejected = false;
            try
            {
                evidenceEngine.Build(mismatchedDocument, new IfcSelectionSet("Mismatch", new[] { "G9" }));
            }
            catch (InvalidOperationException)
            {
                mismatchedQuantityRejected = true;
            }
            True(mismatchedQuantityRejected, "quantity owner mismatch fails closed");
        }

        private static void QuantBimIfcStepIngestion()
        {
            const string step = "ISO-10303-21;\nHEADER;\nFILE_SCHEMA(('IFC4'));\nENDSEC;\nDATA;\n" +
                "#10=IFCWALL('G1',$,'Wall-01',$,$,$,$,$);\n" +
                "#20=IFCBUILDINGSTOREY('S1',$,'L01',$,$,$,$,$,$,.ELEMENT.,0.);\n" +
                "#21=IFCRELCONTAINEDINSPATIALSTRUCTURE('R-SPATIAL',$,$,$,(#10),#20);\n" +
                "#30=IFCWALLTYPE('T1',$,'External',$,$,$,$,$,$,.NOTDEFINED.);\n" +
                "#31=IFCRELDEFINESBYTYPE('R-TYPE',$,$,$,(#10),#30);\n" +
                "#40=IFCPROPERTYSINGLEVALUE('IsExternal',$,IFCBOOLEAN(.T.),$);\n" +
                "#41=IFCPROPERTYSET('P1',$,'Pset_WallCommon',$,(#40));\n" +
                "#42=IFCRELDEFINESBYPROPERTIES('R-PSET',$,$,$,(#10),#41);\n" +
                "#50=IFCQUANTITYAREA('NetSideArea',$,$,12.0,$);\n" +
                "#51=IFCQUANTITYVOLUME('NetVolume',$,$,2.4,$);\n" +
                "#52=IFCELEMENTQUANTITY('Q1',$,'BaseQuantities',$,$,(#50,#51));\n" +
                "#53=IFCRELDEFINESBYPROPERTIES('R-QTO',$,$,$,(#10),#52);\n" +
                "#60=IFCCLASSIFICATIONREFERENCE($,'ARC.WALL','Wall',$);\n" +
                "#61=IFCRELASSOCIATESCLASSIFICATION('R-CLASS',$,$,$,(#10),#60);\nENDSEC;\nEND-ISO-10303-21;\n";

            var source = new IfcStepStandaloneSource();
            var document = source.Parse("minimal-qto.ifc", step);
            Equal(1, document.Elements.Count, "STEP product count");
            True(document.Revision.StartsWith("IFCSTEP-", StringComparison.Ordinal), "STEP deterministic revision");
            var wall = document.Elements.Single();
            Equal("G1", wall.Guid, "STEP GlobalId");
            Equal("IfcWall", wall.Entity, "STEP entity");
            Equal("L01", wall.Storey, "STEP storey relationship");
            Equal("External", wall.Type, "STEP type relationship");
            Equal("ARC.WALL", wall.Classification, "STEP classification relationship");
            Equal("ifc-step://#10", wall.GeometryReference, "STEP source evidence reference");
            True(wall.Properties.Any(x => x.Name == "Pset_WallCommon.IsExternal" && x.Value == "TRUE"), "STEP property set");
            Near(12d, wall.Quantities.Single(x => x.QuantityName == "NetSideArea").Quantity, 1e-12, "STEP area quantity");
            Near(2.4d, wall.Quantities.Single(x => x.QuantityName == "NetVolume").Quantity, 1e-12, "STEP volume quantity");

            var workbench = new QuantBimStandaloneWorkbench(source);
            var tempPath = Path.Combine(Path.GetTempPath(), "qs3d-quantbim-" + Guid.NewGuid().ToString("N") + ".ifc");
            try
            {
                File.WriteAllText(tempPath, step);
                var opened = workbench.Open(tempPath);
                Equal("G1", opened.Elements.Single().Guid, "standalone file-open IFC ingestion");
            }
            finally
            {
                if (File.Exists(tempPath)) File.Delete(tempPath);
            }

            var duplicateRejected = false;
            try
            {
                source.Parse("duplicate.ifc", step.Replace("#20=IFCBUILDINGSTOREY", "#10=IFCBUILDINGSTOREY"));
            }
            catch (InvalidDataException)
            {
                duplicateRejected = true;
            }
            True(duplicateRejected, "duplicate STEP identity fails closed");
        }

        private sealed class FakeIfcSource : IIfcStandaloneSource
        {
            public IfcStandaloneDocument Open(string path)
            {
                var wallQto = new[]
                {
                    new IfcQtoItem("G1", "IfcWall", "L01", "ARC.WALL", "NetSideArea", 12d, "m2"),
                    new IfcQtoItem("G1", "IfcWall", "L01", "ARC.WALL", "NetVolume", 2.4d, "m3")
                };
                var slabQto = new[] { new IfcQtoItem("G2", "IfcSlab", "L01", "STR.SLAB", "NetVolume", 5d, "m3") };
                return new IfcStandaloneDocument(path, "R5", new[]
                {
                    new IfcStandaloneElement("G1", "IfcWall", "Wall-01", "L01", "External", "ARC.WALL", new[] { new IfcPropertyNode("Pset_WallCommon.IsExternal", "TRUE") }, wallQto, "mesh://G1"),
                    new IfcStandaloneElement("G2", "IfcSlab", "Slab-01", "L01", "200mm", "STR.SLAB", new[] { new IfcPropertyNode("Pset_SlabCommon.LoadBearing", "TRUE") }, slabQto, "mesh://G2")
                });
            }
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance, string label)
        {
            if (Math.Abs(expected - actual) > tolerance) throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ": expected true.");
        }
    }
}
