using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Domain;
using QS3D.Core.Services;

namespace QS3D.Core.SmokeTests
{
    internal static class RoomFinishGeneratorReadOnlyResultSmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OrdinaryGeneratedFinishesRemainReadOnly();
        }

        private static void OrdinaryGeneratedFinishesRemainReadOnly()
        {
            var roomFamily = new FamilyDefinition("Room Type", ElementCategory.Room, "Concrete");
            var room = new ElementInstance("ROOM-1", roomFamily, "L1")
            {
                AreaM2 = 12d,
                InnerPerimeterM = 14d
            };
            room.SourceHandles.Add("ABCD");

            var settings = new RoomPropertySet
            {
                GenerateFloorFinish = true,
                GenerateWaterproofing = false,
                GenerateSkirting = true,
                GenerateWallFinish = false,
                GenerateCeilingFinish = false
            };

            var output = RoomFinishGenerator.Generate(room, settings);
            if (output.Count != 2 ||
                output[0].Family.Category != ElementCategory.FloorFinish || Math.Abs(output[0].AreaM2 - 12d) > 1e-12d ||
                output[1].Family.Category != ElementCategory.Skirting || Math.Abs(output[1].LengthM - 14d) > 1e-12d ||
                !string.Equals(output[0].Floor, "L1", StringComparison.Ordinal) ||
                !string.Equals(output[0].Family.Material, "Concrete", StringComparison.Ordinal) ||
                output[0].SourceHandles.Count != 1 || !string.Equals(output[0].SourceHandles[0], "ABCD", StringComparison.Ordinal))
                throw new InvalidOperationException("Room finish generation semantics changed while hardening the result boundary.");

            if (!(output is ICollection<ElementInstance> collection) || !collection.IsReadOnly)
                throw new InvalidOperationException("Room finish generator result must expose a structural read-only collection boundary.");

            try
            {
                collection.Add(room);
            }
            catch (NotSupportedException)
            {
                return;
            }

            throw new InvalidOperationException("Room finish generator result accepted structural mutation through ICollection<T>.");
        }
    }
}
