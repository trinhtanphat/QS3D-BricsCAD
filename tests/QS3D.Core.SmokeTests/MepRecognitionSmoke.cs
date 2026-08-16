using System;
using System.Collections.Generic;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepRecognitionSmoke
    {
        internal static void Run()
        {
            DefaultProfilePriorityAndCase();
            BlockNameRecognition();
            ExplicitPriority();
            AmbiguityFailsClosed();
            UnmatchedFailsClosed();
            MepTbqProjectionSmoke.Run();
        }

        private static void DefaultProfilePriorityAndCase()
        {
            var profile = MepRecognitionProfiles.CreateDefault();
            var cableTray = profile.Recognize("mEp_CaBlEtRaY_main", null);
            Equal(MepRecognitionStatus.Matched, cableTray.Status, "default cable tray status");
            Equal(MepRecognitionDiscipline.Mep, cableTray.Discipline!.Value, "default cable tray discipline");
            Equal(MepElementKind.CableTray, cableTray.MepKind!.Value, "cable tray must outrank embedded cable token");

            var beam = profile.Recognize("s-rc_beam_primary", null);
            Equal(MepRecognitionStatus.Matched, beam.Status, "default beam status");
            Equal(MepRecognitionDiscipline.Structure, beam.Discipline!.Value, "default beam discipline");
            Equal("Beam", beam.Category, "default beam category");
        }

        private static void BlockNameRecognition()
        {
            var result = MepRecognitionProfiles.CreateDefault().Recognize("0", "ahu-01");
            Equal(MepRecognitionStatus.Matched, result.Status, "block-name status");
            Equal(MepElementKind.Equipment, result.MepKind!.Value, "block-name equipment kind");
        }

        private static void ExplicitPriority()
        {
            var profile = new MepRecognitionProfile(new[]
            {
                new MepRecognitionRule("low", 10, MepRecognitionDiscipline.Mep, "Pipe", new[] { "PIPE" }, mepKind: MepElementKind.Pipe),
                new MepRecognitionRule("high", 20, MepRecognitionDiscipline.Mep, "Duct", new[] { "PIPE" }, mepKind: MepElementKind.Duct)
            });
            var result = profile.Recognize("pipe-main", null);
            Equal(MepRecognitionStatus.Matched, result.Status, "priority status");
            Equal(MepElementKind.Duct, result.MepKind!.Value, "higher priority rule");
            Equal(1, result.MatchedRuleIds.Count, "only highest-priority rules participate");
            Equal("high", result.MatchedRuleIds[0], "highest-priority rule id");
        }

        private static void AmbiguityFailsClosed()
        {
            var profile = new MepRecognitionProfile(new[]
            {
                new MepRecognitionRule("pipe", 50, MepRecognitionDiscipline.Mep, "Pipe", new[] { "SERVICE" }, mepKind: MepElementKind.Pipe),
                new MepRecognitionRule("duct", 50, MepRecognitionDiscipline.Mep, "Duct", new[] { "SERVICE" }, mepKind: MepElementKind.Duct)
            });
            var result = profile.Recognize("service-main", null);
            Equal(MepRecognitionStatus.Ambiguous, result.Status, "ambiguity status");
            True(!result.Discipline.HasValue, "ambiguous discipline must not be guessed");
            True(!result.MepKind.HasValue, "ambiguous MEP kind must not be guessed");
            Equal(2, result.MatchedRuleIds.Count, "ambiguous rule evidence count");
        }

        private static void UnmatchedFailsClosed()
        {
            var result = MepRecognitionProfiles.CreateDefault().Recognize("GENERIC-NOTES", "TITLEBLOCK");
            Equal(MepRecognitionStatus.Unmatched, result.Status, "unmatched status");
            True(!result.Discipline.HasValue, "unmatched discipline must remain empty");
            True(result.Category == null, "unmatched category must remain empty");
        }

        private static void Equal<T>(T expected, T actual, string label)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(label + ": expected " + expected + ", actual " + actual + ".");
        }

        private static void True(bool value, string label)
        {
            if (!value) throw new InvalidOperationException(label + ".");
        }
    }
}
