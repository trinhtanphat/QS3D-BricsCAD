using System;
using System.Collections.Generic;
using System.IO;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Services;
using QS3D.Core.Templates;

namespace QS3D.Core.SmokeTests
{
    internal static class LogicRegressionSmoke
    {
        public static void Run()
        {
            RecognitionUsesTokenBoundaries();
            RecognitionKeepsFallbackTypeGateWithAuthoritativeProjectMapping();
            RecognitionRejectsAmbiguousProjectMappings();
            RecognitionRejectsNonFiniteConfidence();
            RecognitionRejectsDuplicateRuleIds();
            HostUnlinkRejectsNonOpeningElements();
            CurtainFramesStaleOnLinkRehostAndUnlink();
            OpeningDimensionsRejectNegativeValues();
            BulkEditRejectsForeignSameIdElements();
        }

        private static void RecognitionUsesTokenBoundaries()
        {
            var falsePositive = new EntitySnapshot("FP", "Line", "DAMAGE");
            falsePositive.Metadata["Text"] = "damage";
            var rejected = new RecognitionEngine().Suggest(falsePositive);
            True(rejected.TopCandidate == null || rejected.TopCandidate.Category != ElementCategory.Beam);

            var beam = new EntitySnapshot("OK", "Line", "KC-DAM");
            beam.Metadata["Text"] = "Dầm chính";
            var accepted = new RecognitionEngine().Suggest(beam);
            True(accepted.TopCandidate != null);
            Equal(ElementCategory.Beam, accepted.TopCandidate!.Category);
            True(accepted.Confidence >= .92d);
        }

        private static void RecognitionKeepsFallbackTypeGateWithAuthoritativeProjectMapping()
        {
            var textOnWallLayer = new EntitySnapshot("TXT", "DBText", "A-WALL");
            textOnWallLayer.Metadata["Text"] = "wall";
            var fallback = new RecognitionEngine().Suggest(textOnWallLayer);
            True(fallback.TopCandidate == null);

            var project = new ProjectState("recognition-type", "Recognition Type");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-WALL"] = ElementCategory.ArchitecturalWall.ToString();
            var mapped = new ProjectRecognitionService().Suggest(project, textOnWallLayer);
            True(mapped.TopCandidate != null);
            Equal(ElementCategory.ArchitecturalWall, mapped.TopCandidate!.Category);
        }

        private static void RecognitionRejectsAmbiguousProjectMappings()
        {
            var project = new ProjectState("recognition-map", "Recognition Mapping");
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-BEAM"] = ElementCategory.Beam.ToString();
            project.Metadata[TemplateProfileStore.LayerMappingPrefix + "A_BEAM"] = ElementCategory.Slab.ToString();
            var snapshot = new EntitySnapshot("AA", "Line", "A-BEAM");
            Throws<InvalidOperationException>(() => new ProjectRecognitionService().Suggest(project, snapshot));

            var profile = new TemplateProfile("ambiguous", "Ambiguous");
            profile.LayerMappings["A-BEAM"] = ElementCategory.Beam.ToString();
            profile.LayerMappings["A_BEAM"] = ElementCategory.Slab.ToString();
            Throws<InvalidDataException>(() => new TemplateProfileStore().Apply(new ProjectState("template", "Template"), profile));

            var projectedProject = new ProjectState("projected", "Projected");
            projectedProject.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-BEAM"] = ElementCategory.Beam.ToString();
            var projectedProfile = new TemplateProfile("projected-template", "Projected Template");
            projectedProfile.LayerMappings["A_BEAM"] = ElementCategory.Slab.ToString();
            Throws<InvalidOperationException>(() => new TemplateProfileStore().Apply(projectedProject, projectedProfile));
            Equal(ElementCategory.Beam.ToString(), projectedProject.Metadata[TemplateProfileStore.LayerMappingPrefix + "A-BEAM"]);
        }

        private static void RecognitionRejectsNonFiniteConfidence()
        {
            var snapshot = new EntitySnapshot("C", "Line", "A-BEAM");
            Throws<ArgumentOutOfRangeException>(() => new RecognitionBatch(Array.Empty<RecognitionResult>(), double.NaN, .15d));
            Throws<ArgumentOutOfRangeException>(() => new RecognitionBatch(Array.Empty<RecognitionResult>(), .92d, double.PositiveInfinity));

            var invalid = new RecognitionCandidate { RuleId = "bad", Category = ElementCategory.Beam, Confidence = double.NaN };
            Throws<ArgumentOutOfRangeException>(() => new RecognitionResult(snapshot, new[] { invalid }));

            var mutable = new RecognitionCandidate { RuleId = "mutable", Category = ElementCategory.Beam, Confidence = .95d };
            var result = new RecognitionResult(snapshot, new[] { mutable });
            mutable.Confidence = double.NaN;
            Throws<ArgumentOutOfRangeException>(() => new RecognitionBatch(new[] { result }));
        }

        private static void RecognitionRejectsDuplicateRuleIds()
        {
            var rules = new List<RecognitionRule>
            {
                new RecognitionRule("same", ElementCategory.Beam),
                new RecognitionRule("SAME", ElementCategory.Slab)
            };
            Throws<ArgumentException>(() => new RecognitionEngine(rules));
        }

        private static void HostUnlinkRejectsNonOpeningElements()
        {
            var project = new ProjectState("logic", "Logic");
            var room = new ProjectElement("R1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            room.Properties["HostWallId"] = "W1";
            project.Elements.Add(room);
            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, room.Id));
            True(room.Properties.ContainsKey("HostWallId"));
        }

        private static void CurtainFramesStaleOnLinkRehostAndUnlink()
        {
            var project = new ProjectState("curtain-host-link", "Curtain host lifecycle");
            var wallA = new ProjectElement("GW-A", ElementCategory.GlassWall, "glass", "floor", "zone");
            var wallB = new ProjectElement("GW-B", ElementCategory.GlassWall, "glass", "floor", "zone");
            wallA.Properties["GeneratedCurtainFrameHandles"] = "A1;A2";
            wallB.Properties["GeneratedCurtainFrameHandles"] = "B1;B2";
            wallA.ClearGeneratedGeometryStale();
            wallB.ClearGeneratedGeometryStale();
            var opening = new ProjectElement("D1", ElementCategory.Door, "door", "floor", "zone");
            project.Elements.Add(wallA);
            project.Elements.Add(wallB);
            project.Elements.Add(opening);
            var service = new HostLinkService();

            service.LinkOpening(project, opening.Id, wallA.Id);
            True(wallA.IsGeneratedCurtainFrameStale());
            True(!wallA.IsGeneratedSolidStale());

            wallA.ClearGeneratedCurtainFrameStale();
            wallB.ClearGeneratedCurtainFrameStale();
            service.LinkOpening(project, opening.Id, wallB.Id);
            True(wallA.IsGeneratedCurtainFrameStale());
            True(wallB.IsGeneratedCurtainFrameStale());
            True(!wallA.IsGeneratedSolidStale());
            True(!wallB.IsGeneratedSolidStale());

            wallB.ClearGeneratedCurtainFrameStale();
            service.UnlinkOpening(project, opening.Id);
            True(wallB.IsGeneratedCurtainFrameStale());
            True(!wallB.IsGeneratedSolidStale());
            True(!opening.Properties.ContainsKey("HostWallId"));
        }

        private static void OpeningDimensionsRejectNegativeValues()
        {
            Throws<ArgumentOutOfRangeException>(() => WallQuantityCalculator.Calculate(4d, 3d, .2d, new[]
            {
                new OpeningCut { WidthM = -0.9d, HeightM = 2.1d }
            }));
            Throws<ArgumentOutOfRangeException>(() => WallQuantityCalculator.Calculate(4d, 3d, .2d, new[]
            {
                new OpeningCut { WidthM = 0.9d, HeightM = -2.1d }
            }));

            var valid = WallQuantityCalculator.Calculate(4d, 3d, .2d, new[]
            {
                new OpeningCut { WidthM = 0.9d, HeightM = 2.1d }
            });
            True(valid.OpeningAreaM2 > 0d);
            True(valid.NetAreaM2 < valid.GrossAreaM2);
        }

        private static void BulkEditRejectsForeignSameIdElements()
        {
            var project = new ProjectState("bulk-owned", "Bulk ownership");
            var owned = new ProjectElement("same-id", ElementCategory.Slab, "family", "floor", "zone");
            owned.Properties["ThicknessM"] = "0.2";
            project.Elements.Add(owned);
            var foreign = new ProjectElement("same-id", ElementCategory.Slab, "family", "floor", "zone");
            foreign.Properties["ThicknessM"] = "0.2";
            var service = new BulkEditService();

            Throws<InvalidOperationException>(() => service.SetProperty(project, new[] { foreign }, "ThicknessM", "0.3"));
            Equal("0.2", owned.Properties["ThicknessM"]);
            Equal("0.2", foreign.Properties["ThicknessM"]);

            Throws<InvalidOperationException>(() => service.MultiplyNumericProperty(project, new[] { foreign }, "ThicknessM", 2d));
            Equal("0.2", owned.Properties["ThicknessM"]);
            Equal("0.2", foreign.Properties["ThicknessM"]);

            var changed = service.SetProperty(project, new[] { owned, owned }, "ThicknessM", "0.25");
            Equal(1, changed.Count);
            Equal("0.25", owned.Properties["ThicknessM"]);
        }

        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
