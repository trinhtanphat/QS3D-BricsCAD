using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Geometry;

namespace QS3D.Core.SmokeTests
{
    internal static class CurtainWallOpeningNegativeCountSmoke
    {
        internal static void Run()
        {
            NegativeFrameCountFailsBeforeAccess();
            NegativeOpeningCountFailsBeforeAccess();
            PanelNegativeCountFailsBeforeAccess();
            PanelNegativeOpeningCountFailsBeforeAccess();
            EmptyInputsRemainAccepted();
        }

        private static void NegativeFrameCountFailsBeforeAccess()
        {
            var frames = new NegativeCountList<CurtainWallRect>();
            ExpectInvalidOperation(
                () => CurtainWallOpeningFramePlanner.Plan(frames, Array.Empty<CurtainWallOpeningRect>()),
                "negative frame Count",
                "Negative frame Count must fail closed.");
            Equal(0, frames.AccessAttempts, "Negative frame Count must fail before frame access.");
        }

        private static void NegativeOpeningCountFailsBeforeAccess()
        {
            var openings = new NegativeCountList<CurtainWallOpeningRect>();
            ExpectInvalidOperation(
                () => CurtainWallOpeningFramePlanner.Plan(Array.Empty<CurtainWallRect>(), openings),
                "negative opening Count",
                "Negative opening Count must fail closed.");
            Equal(0, openings.AccessAttempts, "Negative opening Count must fail before opening access.");
        }

        private static void PanelNegativeCountFailsBeforeAccess()
        {
            var panels = new NegativeCountList<CurtainWallRect>();
            ExpectInvalidOperation(
                () => CurtainWallOpeningPanelPlanner.Plan(panels, Array.Empty<CurtainWallOpeningRect>()),
                "negative panel Count",
                "Negative panel Count must fail closed.");
            Equal(0, panels.AccessAttempts, "Negative panel Count must fail before panel access.");
        }

        private static void PanelNegativeOpeningCountFailsBeforeAccess()
        {
            var openings = new NegativeCountList<CurtainWallOpeningRect>();
            ExpectInvalidOperation(
                () => CurtainWallOpeningPanelPlanner.Plan(Array.Empty<CurtainWallRect>(), openings),
                "negative opening Count",
                "Panel planner negative opening Count must fail closed.");
            Equal(0, openings.AccessAttempts, "Panel planner negative opening Count must fail before opening access.");
        }

        private static void EmptyInputsRemainAccepted()
        {
            var framePlan = CurtainWallOpeningFramePlanner.Plan(
                Array.Empty<CurtainWallRect>(),
                Array.Empty<CurtainWallOpeningRect>());
            Equal(0, framePlan.Pieces.Count, "Empty frame input must remain accepted.");
            Equal(0, framePlan.InterruptedFrameCount, "Empty frame input must not report interruptions.");

            var panelPlan = CurtainWallOpeningPanelPlanner.Plan(
                Array.Empty<CurtainWallRect>(),
                Array.Empty<CurtainWallOpeningRect>());
            Equal(0, panelPlan.Pieces.Count, "Empty panel input must remain accepted.");
            Equal(0, panelPlan.SourcePanelCount, "Empty panel input must report zero source panels.");
        }

        private static void ExpectInvalidOperation(Action action, string diagnosticFragment, string failureMessage)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.IndexOf(diagnosticFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    throw new Exception(failureMessage + " Actual diagnostic: " + ex.Message);
                return;
            }

            throw new Exception(failureMessage);
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new Exception(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class NegativeCountList<T> : IReadOnlyList<T>
        {
            public int AccessAttempts { get; private set; }
            public int Count => -1;

            public T this[int index]
            {
                get
                {
                    AccessAttempts++;
                    throw new Exception("Negative Count input must fail before index access.");
                }
            }

            public IEnumerator<T> GetEnumerator()
            {
                AccessAttempts++;
                throw new Exception("Negative Count input must fail before enumeration.");
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }

    internal static class CurtainWallOpeningNegativeCountSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize() => CurtainWallOpeningNegativeCountSmoke.Run();
    }
}
