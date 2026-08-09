using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public static class Qs3dCatalog
    {
        private static readonly IReadOnlyList<string> _roomFinishItems = new[]
        {
            "Phòng",
            "Sàn Hoàn Thiện",
            "Chống Thấm",
            "Chân Tường",
            "Hoàn Thiện Tường",
            "Trần Hoàn Thiện",
            "Lan Can"
        };

        private static readonly IReadOnlyList<string> _wallItems = new[]
        {
            "Tường Gạch",
            "Vách Kính",
            "Trụ Tường"
        };

        private static readonly IReadOnlyList<string> _doorItems = new[]
        {
            "Lỗ Mở Vách",
            "Cửa Đi"
        };

        public static IReadOnlyList<string> RoomFinishItems => _roomFinishItems;
        public static IReadOnlyList<string> WallItems => _wallItems;
        public static IReadOnlyList<string> DoorItems => _doorItems;
    }
}
