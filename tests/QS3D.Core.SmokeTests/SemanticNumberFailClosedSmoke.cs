using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class SemanticNumberFailClosedSmoke
    {
        internal static void Run()
        {
            MalformedPresentOpeningWidthFailsBeforeQuantityWrite();
            NonFinitePresentOpeningHeightFailsClosed();
            MissingOptionalRailingValuesStillUseFallbacks();
            MalformedOptionalRailingHeightDoesNotUseFallback();
            ValidOpeningNumbersRemainUnchanged();
        }

        private static void MalformedPresentOpeningWidthFailsBeforeQuantityWrite()
        {
            var project = NewProject();
            var opening = NewOpening();
            opening.Properties["WidthM"] = "not-a-number";
            opening.Properties["HeightM"] = "2";

            Throws<InvalidOperationException>(() => new OpeningRegenerator().Regenerate(project, opening));
            False(opening.Quantities.ContainsKey("OpeningAreaM2"), "Malformed width wrote a derived opening area.");
            False(opening.Quantities.ContainsKey("Count"), "Malformed width wrote a derived opening count.");
        }

        private static void NonFinitePresentOpeningHeightFailsClosed()
        {
            var project = NewProject();
            var opening = NewOpening();
            opening.Properties["WidthM"] = "0.9";
            opening.Properties["HeightM"] = "NaN";

            Throws<InvalidOperationException>(() => new OpeningRegenerator().Regenerate(project, opening));
            False(opening.Quantities.ContainsKey("OpeningAreaM2"), "Non-finite height wrote a derived opening area.");
        }

        private static void MissingOptionalRailingValuesStillUseFallbacks()
        {
            var project = NewProject();
            var railing = new ProjectElement("RAIL-1", ElementCategory.Railing);
            railing.Properties["LengthM"] = "2";

            new StructuralRegenerator().Regenerate(project, railing);

            Near(2d, railing.Quantities["LengthM"], 1e-12);
            Near(1.1d, railing.Quantities["HeightM"], 1e-12);
            Near(3d, railing.Quantities["PostCount"], 1e-12);
        }

        private static void MalformedOptionalRailingHeightDoesNotUseFallback()
        {
            var project = NewProject();
            var railing = new ProjectElement("RAIL-2", ElementCategory.Railing);
            railing.Properties["LengthM"] = "2";
            railing.Properties["HeightM"] = "bad-height";

            Throws<InvalidOperationException>(() => new StructuralRegenerator().Regenerate(project, railing));
            False(railing.Quantities.ContainsKey("HeightM"), "Malformed optional height silently used the default.");
        }

        private static void ValidOpeningNumbersRemainUnchanged()
        {
            var project = NewProject();
            var opening = NewOpening();
            opening.Properties["WidthM"] = "0.9";
            opening.Properties["HeightM"] = "2.2";

            new OpeningRegenerator().Regenerate(project, opening);

            Near(1.98d, opening.Quantities["OpeningAreaM2"], 1e-12);
            Near(1d, opening.Quantities["Count"], 1e-12);
        }

        private static ProjectState NewProject() => new ProjectState("semantic-number-smoke", "Semantic number smoke");

        private static ProjectElement NewOpening() => new ProjectElement("OPEN-1", ElementCategory.WallOpening);

        private static void Throws<TException>(Action action) where TException : Exception
        {
            try { action(); }
            catch (TException) { return; }
            throw new Exception("Expected " + typeof(TException).Name + ".");
        }

        private static void False(bool condition, string message)
        {
            if (condition) throw new Exception(message);
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }
    }

    internal static class SemanticNumberFailClosedSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => SemanticNumberFailClosedSmoke.Run();
    }
}
