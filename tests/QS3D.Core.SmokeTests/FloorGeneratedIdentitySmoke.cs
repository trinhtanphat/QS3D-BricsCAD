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
            SignedZeroCanonicalizesState();
            ExactLengthBoundariesStayAccepted();
            InvalidLegacyLengthsFailClosed();
            MalformedUnicodeFailsClosed();
            OwnerAndStateKeysStayUnambiguous();
            OwnerTokenBuilderMatchesCreate();
            StateSensitivityStaysSeparatedFromOwnership();
            ExtremeFiniteElevationIsDeterministic();
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
            var firstIdentity = FloorGeneratedIdentityPlanner.Create(first);
            var secondIdentity = FloorGeneratedIdentityPlanner.Create(second);

            Equal(firstIdentity.OwnerToken, secondIdentity.OwnerToken);
            Equal(firstIdentity.OwnerKey, secondIdentity.OwnerKey);
            Equal("LEVEL-01", firstIdentity.FloorId);
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

        private static void SignedZeroCanonicalizesState()
        {
            var negativeZero = BitConverter.Int64BitsToDouble(unchecked((long)0x8000000000000000UL));
            var positive = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L0", "Level Zero", 0d));
            var negative = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L0", "Level Zero", negativeZero));

            Equal(positive.StateKey, negative.StateKey);
            Equal(positive.StateToken, negative.StateToken);
            Equal(0L, BitConverter.DoubleToInt64Bits(negative.ElevationM));
        }

        private static void ExactLengthBoundariesStayAccepted()
        {
            var floorId = new string('f', 64);
            var floorName = new string('N', 120);
            var identity = FloorGeneratedIdentityPlanner.Create(new FloorDefinition(floorId, floorName, 0d));

            Equal(new string('F', 64), identity.FloorId);
            Equal(floorName, identity.DisplayName);
            Equal("64:" + new string('F', 64), identity.OwnerKey);
            Equal(FloorGeneratedIdentityPlanner.BuildOwnerToken(floorId), identity.OwnerToken);
        }

        private static void InvalidLegacyLengthsFailClosed()
        {
            Throws<ArgumentNullException>(() => FloorGeneratedIdentityPlanner.Create(null));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken(null));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken("   "));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken(new string('A', 65)));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L", "   ", 0d)));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L", new string('N', 121), 0d)));
            Throws<ArgumentOutOfRangeException>(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L", "NaN", double.NaN)));
            Throws<ArgumentOutOfRangeException>(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L", "+Inf", double.PositiveInfinity)));
            Throws<ArgumentOutOfRangeException>(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L", "-Inf", double.NegativeInfinity)));
        }

        private static void MalformedUnicodeFailsClosed()
        {
            var loneHighSurrogate = "L" + '\ud800';
            var loneLowSurrogate = "N" + '\udc00';

            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken(loneHighSurrogate));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L-UNICODE", loneLowSurrogate, 0d)));
        }

        private static void OwnerAndStateKeysStayUnambiguous()
        {
            var shortIdLongName = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("A", "BC", 1d));
            var longIdShortName = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("AB", "C", 1d));

            Equal("1:A", shortIdLongName.OwnerKey);
            Equal("2:AB", longIdShortName.OwnerKey);
            Equal("1:A|2:BC|1", shortIdLongName.StateKey);
            Equal("2:AB|1:C|1", longIdShortName.StateKey);
            NotEqual(shortIdLongName.OwnerToken, longIdShortName.OwnerToken);
            NotEqual(shortIdLongName.StateToken, longIdShortName.StateToken);
        }

        private static void OwnerTokenBuilderMatchesCreate()
        {
            var identity = FloorGeneratedIdentityPlanner.Create(new FloorDefinition(" mixed-case ", " Floor Name ", 12.5d));
            Equal("MIXED-CASE", identity.FloorId);
            Equal("Floor Name", identity.DisplayName);
            Equal(FloorGeneratedIdentityPlanner.BuildOwnerToken("MIXED-CASE"), identity.OwnerToken);
            Equal(FloorGeneratedIdentityPlanner.BuildOwnerToken(" mixed-case "), identity.OwnerToken);
        }

        private static void StateSensitivityStaysSeparatedFromOwnership()
        {
            var baseline = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("L3", "Level 3", 7.2d));
            var renamed = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("l3", "Third Floor", 7.2d));
            var moved = FloorGeneratedIdentityPlanner.Create(new FloorDefinition(" L3 ", "Level 3", 7.200000000000001d));

            Equal(baseline.OwnerToken, renamed.OwnerToken);
            Equal(baseline.OwnerToken, moved.OwnerToken);
            NotEqual(baseline.StateToken, renamed.StateToken);
            NotEqual(baseline.StateToken, moved.StateToken);
        }

        private static void ExtremeFiniteElevationIsDeterministic()
        {
            var maxA = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("MAX", "Maximum", double.MaxValue));
            var maxB = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("max", "Maximum", double.MaxValue));
            var minA = FloorGeneratedIdentityPlanner.Create(new FloorDefinition("MIN", "Minimum", double.MinValue));
            var minB = FloorGeneratedIdentityPlanner.Create(new FloorDefinition(" min ", "Minimum", double.MinValue));

            Equal(maxA.StateKey, maxB.StateKey);
            Equal(maxA.StateToken, maxB.StateToken);
            Equal(minA.StateKey, minB.StateKey);
            Equal(minA.StateToken, minB.StateToken);
            NotEqual(maxA.StateToken, minA.StateToken);
        }

        private static void TokensStayCompact()
        {
            var identity = FloorGeneratedIdentityPlanner.Create(
                new FloorDefinition(new string('F', 64), new string('N', 120), -123.456789d));
            AssertToken(identity.OwnerToken, "LVO1:");
            AssertToken(identity.StateToken, "LVS1:");
        }

        private static void AssertToken(string token, string prefix)
        {
            True(token.StartsWith(prefix, StringComparison.Ordinal));
            Equal(prefix.Length + 64, token.Length);
            for (var i = prefix.Length; i < token.Length; i++)
            {
                var c = token[i];
                True((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'));
            }
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new Exception("Expected " + typeof(T).Name + ", got " + ex.GetType().Name + ".", ex);
            }
            throw new Exception("Expected exception " + typeof(T).Name + ".");
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
