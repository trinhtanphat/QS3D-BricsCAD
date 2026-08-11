using System;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishGeneratorNumericSafetySmoke
    {
        internal static void Run()
        {
            RejectsNegativeAreaWhenConsumed();
            RejectsNonFiniteWallAreaWhenConsumed();
            RejectsNonFiniteSkirtingLengthWhenConsumed();
            IgnoresInvalidMetricWhenCorrespondingOutputsAreDisabled();
            PreservesValidGenerationAndProvenance();
        }

        private static void RejectsNegativeAreaWhenConsumed()
        {
            var room = NewRoom();
            room.AreaM2 = -1d;
            var settings = Only(ElementCategory.FloorFinish);
            Throws<InvalidOperationException>(() => RoomFinishGenerator.Generate(room, settings));
        }

        private static void RejectsNonFiniteWallAreaWhenConsumed()
        {
            var room = NewRoom();
            room.SideAreaM2 = double.PositiveInfinity;
            var settings = Only(ElementCategory.WallFinish);
            Throws<InvalidOperationException>(() => RoomFinishGenerator.Generate(room, settings));
        }

        private static void RejectsNonFiniteSkirtingLengthWhenConsumed()
        {
            var room = NewRoom();
            room.InnerPerimeterM = double.NaN;
            var settings = Only(ElementCategory.Skirting);
            Throws<InvalidOperationException>(() => RoomFinishGenerator.Generate(room, settings));
        }

        private static void IgnoresInvalidMetricWhenCorrespondingOutputsAreDisabled()
        {
            var room = NewRoom();
            room.AreaM2 = double.NaN;
            room.SideAreaM2 = double.NegativeInfinity;
            room.InnerPerimeterM = 7.5d;

            var output = RoomFinishGenerator.Generate(room, Only(ElementCategory.Skirting));
            Equal(1, output.Count);
            Equal(ElementCategory.Skirting, output[0].Family.Category);
            Near(7.5d, output[0].LengthM, 1e-12);
        }

        private static void PreservesValidGenerationAndProvenance()
        {
            var room = NewRoom();
            room.AreaM2 = 20d;
            room.InnerPerimeterM = 18d;
            room.SideAreaM2 = 48d;
            room.SourceHandles.Add("AB12");

            var output = RoomFinishGenerator.Generate(room, new RoomPropertySet());
            Equal(5, output.Count);
            Equal(ElementCategory.FloorFinish, output[0].Family.Category);
            Equal(ElementCategory.Waterproofing, output[1].Family.Category);
            Equal(ElementCategory.Skirting, output[2].Family.Category);
            Equal(ElementCategory.WallFinish, output[3].Family.Category);
            Equal(ElementCategory.CeilingFinish, output[4].Family.Category);
            Near(20d, output[0].AreaM2, 1e-12);
            Near(18d, output[2].LengthM, 1e-12);
            Near(48d, output[3].AreaM2, 1e-12);
            Equal("AB12", output[0].SourceHandles[0]);
            Equal("AB12", output[4].SourceHandles[0]);
        }

        private static ElementInstance NewRoom()
        {
            return new ElementInstance(
                "R1",
                new FamilyDefinition("Room", ElementCategory.Room, "Paint"),
                "Level 1");
        }

        private static RoomPropertySet Only(ElementCategory category)
        {
            return new RoomPropertySet
            {
                GenerateFloorFinish = category == ElementCategory.FloorFinish,
                GenerateWaterproofing = category == ElementCategory.Waterproofing,
                GenerateSkirting = category == ElementCategory.Skirting,
                GenerateWallFinish = category == ElementCategory.WallFinish,
                GenerateCeilingFinish = category == ElementCategory.CeilingFinish
            };
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try { action(); }
            catch (T) { return; }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private static void Equal<T>(T expected, T actual)
        {
            if (!Equals(expected, actual))
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }

        private static void Near(double expected, double actual, double tolerance)
        {
            if (Math.Abs(expected - actual) > tolerance)
                throw new Exception("Expected " + expected + " but got " + actual + ".");
        }
    }
}
