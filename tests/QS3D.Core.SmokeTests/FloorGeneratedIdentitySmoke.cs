using System;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorGeneratedIdentitySmoke
    {
        public static void Run()
        {
            OwnerIsStableAcrossNameAndElevationChanges();
            CaseAndWhitespaceCanonicalizeOwner();
            StateIsDeterministic();
            InvalidLegacyLengthsFailClosed();
            TokensStayCompact();
        }

        private static void OwnerIsStableAcrossNameAndElevationChanges()
        {
            var floor = new FloorDefinition("L1", "Level 1", 0d);
            var before = FloorGeneratedIdentityPlanner.Create(floor);
            floor.Name = "Ground Floor";
            floor.ElevationM = 0.15d;
            var after = FloorGeneratedIdentityPlanner.Create(floor);

            Equal(before.OwnerToken, after.OwnerToken);
            NotEqual(before.StateToken, after.StateToken);
            Equal("L1", after.FloorId);
            Equal("Ground Floor", after.DisplayName);
            Equal(0.15d, after.ElevationM);
        }

        private static void CaseAndWhitespaceCanonicalizeOwner()
        {
            var first = new FloorDefinition(" level-01 ", "Level 01", 1d);
            var second = new FloorDefinition("LEVEL-01", "Level 01", 1d);
            Equal(
                FloorGeneratedIdentityPlanner.Create(first).OwnerToken,
                FloorGeneratedIdentityPlanner.Create(second).OwnerToken);
            Equal(
                FloorGeneratedIdentityPlanner.BuildOwnerToken("level-01"),
                FloorGeneratedIdentityPlanner.BuildOwnerToken(" LEVEL-01 "));
        }

        private static void StateIsDeterministic()
        {
            var first = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L2", "Level 2", 3.6d));
            var second = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("l2", "Level 2", 3.6d));
            Equal(first.OwnerToken, second.OwnerToken);
            Equal(first.StateToken, second.StateToken);
            Equal(first.StateKey, second.StateKey);
        }

        private static void InvalidLegacyLengthsFailClosed()
        {
            Throws(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition(new string('A', 65), "Legacy", 0d)));
            Throws(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L", new string('N', 121), 0d)));
        }

        private static void TokensStayCompact()
        {
            var identity = FloorGeneratedIdentityPlanner.Create(
                new FloorDefinition(new string('F', 64), new string('N', 120), -123.456789d));
            True(identity.OwnerToken.StartsWith("LVO1:", StringComparison.Ordinal));
            True(identity.StateToken.StartsWith("LVS1:", StringComparison.Ordinal));
            True(identity.OwnerToken.Length < 100);
            True(identity.StateToken.Length < 100);
        }

        private static void Throws(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                return;
            }
            throw new Exception("Expected operation to throw.");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual)) throw new Exception("Expected " + expected + ", got " + actual + ".");
        }

        private static void NotEqual<T>(T left, T right)
        {
            if (Equals(left, right)) throw new Exception("Expected values to differ.");
        }

        private static void True(bool value)
        {
            if (!value) throw new Exception("Expected condition to be true.");
        }
    }
}
