using System;
using System.Collections.Generic;
using QS3D.Core.Domain;

namespace QS3D.Core.Services
{
    public static class RoomFinishGenerator
    {
        public static IReadOnlyList<ElementInstance> Generate(ElementInstance room, RoomPropertySet settings)
        {
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            if (room.Family.Category != ElementCategory.Room)
                throw new ArgumentException("Source element must be a Room.", nameof(room));

            var output = new List<ElementInstance>();
            AddIf(settings.GenerateFloorFinish, ElementCategory.FloorFinish, "Sàn Hoàn Thiện", room, output, areaM2: room.AreaM2);
            AddIf(settings.GenerateWaterproofing, ElementCategory.Waterproofing, "Chống Thấm", room, output, areaM2: room.AreaM2);
            AddIf(settings.GenerateSkirting, ElementCategory.Skirting, "Chân Tường", room, output, lengthM: room.InnerPerimeterM);
            AddIf(settings.GenerateWallFinish, ElementCategory.WallFinish, "Hoàn Thiện Tường", room, output, areaM2: room.SideAreaM2);
            AddIf(settings.GenerateCeilingFinish, ElementCategory.CeilingFinish, "Trần Hoàn Thiện", room, output, areaM2: room.AreaM2);
            return output;
        }

        private static void AddIf(bool enabled, ElementCategory category, string familyName, ElementInstance room, IList<ElementInstance> output, double lengthM = 0d, double areaM2 = 0d)
        {
            if (!enabled) return;
            RequireFiniteNonNegative(lengthM, nameof(lengthM));
            RequireFiniteNonNegative(areaM2, nameof(areaM2));

            var family = new FamilyDefinition(familyName, category, room.Family.Material);
            var element = new ElementInstance(room.Id + ":" + category, family, room.Floor)
            {
                LengthM = lengthM,
                AreaM2 = areaM2
            };
            foreach (var handle in room.SourceHandles) element.SourceHandles.Add(handle);
            output.Add(element);
        }

        private static void RequireFiniteNonNegative(double value, string parameterName)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0d)
                throw new InvalidOperationException("Room finish source metric '" + parameterName + "' must be finite and non-negative.");
        }
    }
}
