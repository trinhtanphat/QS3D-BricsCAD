using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class FloorGeneratedIdentityUnicodeSmoke
    {
        public static void Run()
        {
            MalformedIdsAreRejected();
            MalformedNamesAreRejected();
            ValidSupplementaryUnicodeRemainsDeterministic();
        }

        private static void MalformedIdsAreRejected()
        {
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken("FLOOR-\uD800"));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken("FLOOR-\uD801"));
            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.BuildOwnerToken("FLOOR-\uDC00"));
        }

        private static void MalformedNamesAreRejected()
        {
            const string malformedName = "Floor-\uD800";
            Throws<ArgumentException>(() => new FloorDefinition("F-1", malformedName, 3.5d));

            var floor = new FloorDefinition("F-1", "Floor 1", 3.5d);
            var nameField = typeof(FloorDefinition).GetField("_name", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("FloorDefinition raw-name fixture field was not found.");
            nameField.SetValue(floor, malformedName);
            if (!string.Equals(floor.Name, malformedName, StringComparison.Ordinal))
                throw new InvalidOperationException("FloorDefinition malformed legacy name fixture was not injected.");

            Throws<ArgumentException>(() => FloorGeneratedIdentityPlanner.Create(floor));
        }

        private static void ValidSupplementaryUnicodeRemainsDeterministic()
        {
            const string scalar = "\uD83E\uDDF1";
            var direct = FloorGeneratedIdentityPlanner.BuildOwnerToken("fl-" + scalar);
            var recased = FloorGeneratedIdentityPlanner.BuildOwnerToken("FL-" + scalar);
            if (!string.Equals(direct, recased, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Floor id text changed case-insensitive owner-token identity.");
            if (!direct.StartsWith("LVO1:", StringComparison.Ordinal) || direct.Length != "LVO1:".Length + 64)
                throw new InvalidOperationException("Floor owner-token format changed unexpectedly.");

            var floor = new FloorDefinition("fl-" + scalar, "Floor " + scalar, 3.5d);
            var first = FloorGeneratedIdentityPlanner.Create(floor);
            var second = FloorGeneratedIdentityPlanner.Create(floor);
            if (!string.Equals(first.OwnerToken, direct, StringComparison.Ordinal) ||
                !string.Equals(first.OwnerToken, second.OwnerToken, StringComparison.Ordinal) ||
                !string.Equals(first.StateToken, second.StateToken, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Floor identity text produced non-deterministic generated tokens.");
            if (!string.Equals(first.FloorId, "FL-" + scalar, StringComparison.Ordinal) ||
                !string.Equals(first.DisplayName, "Floor " + scalar, StringComparison.Ordinal))
                throw new InvalidOperationException("Valid supplementary Floor identity text changed canonical id/name semantics.");
        }

        private static TException Throws<TException>(Action action) where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Expected " + typeof(TException).Name + " but got " + ex.GetType().Name + ".", ex);
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }
    }

    internal static class FloorGeneratedIdentityUnicodeSmokeRegistration
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            FloorGeneratedIdentityUnicodeSmoke.Run();
        }
    }
}
