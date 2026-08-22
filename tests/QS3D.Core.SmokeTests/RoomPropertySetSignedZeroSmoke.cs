using System;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomPropertySetSignedZeroSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var room = new RoomPropertySet();

            room.BaseOffsetMm = -0d;
            CanonicalPositiveZero(room.BaseOffsetMm, "BaseOffsetMm");
            room.TopOffsetMm = -0d;
            CanonicalPositiveZero(room.TopOffsetMm, "TopOffsetMm");

            room.BaseOffsetMm = -125d;
            Equal(-125d, room.BaseOffsetMm, "negative base offset");
            room.BaseOffsetMm = 250d;
            Equal(250d, room.BaseOffsetMm, "positive base offset");
            room.TopOffsetMm = -75d;
            Equal(-75d, room.TopOffsetMm, "negative top offset");
            room.TopOffsetMm = 300d;
            Equal(300d, room.TopOffsetMm, "positive top offset");

            Throws<ArgumentOutOfRangeException>(() => room.BaseOffsetMm = double.NaN);
            Throws<ArgumentOutOfRangeException>(() => room.BaseOffsetMm = double.PositiveInfinity);
            Throws<ArgumentOutOfRangeException>(() => room.TopOffsetMm = double.NegativeInfinity);

            Require(room.GenerateFloorFinish, "GenerateFloorFinish default changed.");
            Require(room.GenerateWaterproofing, "GenerateWaterproofing default changed.");
            Require(room.GenerateSkirting, "GenerateSkirting default changed.");
            Require(room.GenerateWallFinish, "GenerateWallFinish default changed.");
            Require(room.GenerateCeilingFinish, "GenerateCeilingFinish default changed.");
        }

        private static void CanonicalPositiveZero(double value, string label)
        {
            if (value != 0d)
                throw new InvalidOperationException(label + ": expected zero but got " + value + ".");
            if (BitConverter.DoubleToInt64Bits(value) != BitConverter.DoubleToInt64Bits(0d))
                throw new InvalidOperationException(label + ": expected canonical positive zero.");
        }

        private static void Equal(double expected, double actual, string label)
        {
            if (expected != actual)
                throw new InvalidOperationException(label + ": expected " + expected + " but got " + actual + ".");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
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

            throw new InvalidOperationException("Expected exception " + typeof(T).Name + ".");
        }
    }
}
