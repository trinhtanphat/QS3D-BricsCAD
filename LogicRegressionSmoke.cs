using System;
using QS3D.Core.Domain;
using QS3D.Core.Model;
using QS3D.Core.Recognition;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class LogicRegressionSmoke
    {
        public static void Run()
        {
            RecognitionUsesTokenBoundaries();
            HostUnlinkRejectsNonOpeningElements();
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

        private static void HostUnlinkRejectsNonOpeningElements()
        {
            var project = new ProjectState("logic", "Logic");
            var room = new ProjectElement("R1", ElementCategory.Room, string.Empty, string.Empty, string.Empty);
            room.Properties["HostWallId"] = "W1";
            project.Elements.Add(room);
            Throws<InvalidOperationException>(() => new HostLinkService().UnlinkOpening(project, room.Id));
            True(room.Properties.ContainsKey("HostWallId"));
        }

        private static void Equal<T>(T expected, T actual) { if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual); }
        private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
        private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new Exception("Expected exception " + typeof(T).Name + "."); }
    }
}
