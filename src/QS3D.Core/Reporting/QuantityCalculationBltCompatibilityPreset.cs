using System;
using System.Collections.Generic;

namespace QS3D.Core.Reporting
{
    /// <summary>
    /// Exact compatibility preset reconstructed from the user-supplied
    /// BLT3D_CaiDatTinhToan.json (schema 2). This preset is opt-in and does not
    /// replace QS3D native defaults or infer aliases for legacy integer codes.
    /// </summary>
    public static class QuantityCalculationBltCompatibilityPreset
    {
        private static readonly int[] CategoryCodes =
        {
            201, 202, 204, 205, 207, 301, 302, 703, 401, 501, 601, 701, 704, 705, 801, 802, 803, 804, 805, 806, 901, 403, 902, 903, 904, 905, 1301, 1302
        };

        private const string CategoryExtractionMasksBase64 =
            "AwAAAAADAwMDAQEAAAEAAQEBAQEDAwAAAAAAAA==";

        private const string IntersectionMasksBase64 =
            "Hx8fHwYGBgYGBgYGBgYGBgYGBgYfBh8fHx8fHwYfHx8GBgYGBgYGBgYGBgYGBgYGHwYfHx8fHx8GBh8GBgYGBgYGBgYGBgYG" +
            "BgYGBh8GHx8fHx8fBgYfHwYGBgYGBgYGBgYGBgYGBgYfBh8fHx8fHx8fHx8fBgYGBgYGBgYGBgYGBgYGHwYfHx8fHx8fHx8f" +
            "Hx8GHwYGBh8fBgYGBgYGHx8fAB8fHx8fHx8fHx8fHx8fBgYfHwYGBgYGBh8fHx8fHx8fHx8fHx8fBgYfHwYGHx8GBgYGBgYf" +
            "Hx8fHx8fHx8fHx8fHx8GBh8fBh8fBgYGBwYGHx8GAR8fHx8fHx8fHx8fHx8GHwYfHx8GBh8GBh8fHx8fHx8fHx8fHx8fHx8f" +
            "Hx8fHx8fBgYfBgYfHx8fHx8fHx8fHx8fHwYGBgYGBh8fBgYGBgYGBh8GHx8fHx8fHx8fHx8GBgYGBgYGHwYGBgYGBgYfBh8f" +
            "Hx8fHx8fHx8fHx8fHwYGHx8fBgYfBgYfHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8fHx8f" +
            "Hx8GHx8fHx8fHx8fHx8fHx8fHx8fHx8fHgYGHx8GBgYfBgYfHx8fHx8fHx8fHx8fHx8fHx8fHx8fHwYGHx8fHx8fHx8fHx8f" +
            "Hx8fHx8fHx8fHx8fHx8GBh8GHx8fHx8fHx8fHx8fHx8fBgYGBgYGHx8GBgYGBgYfHwYfHx8fHx8GBgYGBgYGBgYGBgYGBgYG" +
            "BgYGBh8GBgYGBgYGHx8fHx8GBgYGBgYfHwYGBgYGBh8fHx8fHx8fHwYGBgYGAAYGAAYGBgYGBgYGBgYGHwYfBgYGHwYGBgYG" +
            "BgYGBgYGBgYGBgYGBgYGBh8GBh8GBh8GBgYGBgYGBgYGBgYGBgYGBgYGBgYfBgYGHwYfBgYGBgYGBgYGBgYGBgYGBgYGBgYG" +
            "HwYGBgYfHwYGBgYGBgYGBgYGBgYGBgYGBgYGBgYGBgYGBh8GBgYGBgYGBgYGBgYGBgYGBgYGBgYfBh8fHx8fHw==";

        public static QuantityCalculationSettings Create()
        {
            var categoryMasks = Convert.FromBase64String(CategoryExtractionMasksBase64);
            var intersectionMasks = Convert.FromBase64String(IntersectionMasksBase64);
            if (categoryMasks.Length != CategoryCodes.Length)
                throw new InvalidOperationException("Bundled BLT category preset is inconsistent.");
            if (intersectionMasks.Length != CategoryCodes.Length * CategoryCodes.Length)
                throw new InvalidOperationException("Bundled BLT intersection preset is inconsistent.");

            var settings = new QuantityCalculationSettings
            {
                SchemaVersion = QuantityCalculationSettings.CurrentSchemaVersion,
                FormworkTolerance = 10d,
                BlindingConcreteOffset = 100d,
                MinSubtractAreaMm2 = 10d,
                MinFormworkAreaMm2 = 1000d,
                MinConcreteVolumeM3 = 0.0001d,
                EngulfRelPercent = 1d,
                EngulfMinAreaMm2 = 1000d,
                RoomGapFillMm = 50d,
                RoomSearchRadiusMm = 40000d,
                DimColor = "#FFFFFF",
                DimTextHeight = 30d,
                CategoryRules = new List<QuantityCategoryRuleSetting>(CategoryCodes.Length),
                IntersectionRules = new List<QuantityIntersectionRuleSetting>(intersectionMasks.Length)
            };

            for (var i = 0; i < CategoryCodes.Length; i++)
            {
                var mask = categoryMasks[i];
                settings.CategoryRules.Add(new QuantityCategoryRuleSetting
                {
                    Category = CategoryCodes[i],
                    ExtractSide = (mask & 1) != 0,
                    ExtractBottom = (mask & 2) != 0,
                    FaceAngleThresholdDeg = 30d
                });
            }

            var index = 0;
            for (var sourceIndex = 0; sourceIndex < CategoryCodes.Length; sourceIndex++)
            {
                for (var targetIndex = 0; targetIndex < CategoryCodes.Length; targetIndex++)
                {
                    var mask = intersectionMasks[index++];
                    settings.IntersectionRules.Add(new QuantityIntersectionRuleSetting
                    {
                        Source = CategoryCodes[sourceIndex],
                        Target = CategoryCodes[targetIndex],
                        SubtractConcrete = (mask & 1) != 0,
                        SubtractSideFormworkByConcrete = (mask & 2) != 0,
                        SubtractBottomFormworkByConcrete = (mask & 4) != 0,
                        SubtractSideFormworkBySideFormwork = (mask & 8) != 0,
                        SubtractBottomFormworkByBottomFormwork = (mask & 16) != 0
                    });
                }
            }

            settings.NormalizeAndValidate();
            return settings;
        }
    }
}
