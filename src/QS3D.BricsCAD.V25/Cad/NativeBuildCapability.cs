using QS3D.Core.Domain;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class NativeBuildCapability
    {
        public static bool Supports(ElementCategory category) =>
            IsWallCategory(category) || StructuralSolidBuilder.Supports(category);

        public static bool IsWallCategory(ElementCategory category) =>
            category == ElementCategory.ArchitecturalWall ||
            category == ElementCategory.GlassWall ||
            category == ElementCategory.WallPier;

        public static string UnsupportedMessage(ElementCategory category) =>
            "Vẽ 3D native chưa hỗ trợ category " + category + ". Có thể tiếp tục Bóc chọn/semantic/BQ và dùng workflow chuyên dụng nếu category đó có builder riêng.";
    }
}
