using System.Collections.Generic;

namespace QS3D.Core.Domain
{
    public static class Qs3dCatalog
    {
        private static readonly IReadOnlyList<string> _roomFinishItems = new List<string>
        {
            "Phòng",
            "Sàn Hoàn Thiện",
            "Chống Thấm",
            "Chân Tường",
            "Hoàn Thiện Tường",
            "Trần Hoàn Thiện",
            "Lan Can"
        }.AsReadOnly();

        private static readonly IReadOnlyList<string> _wallItems = new List<string>
        {
            "Tường Gạch",
            "Vách Kính",
            "Trụ Tường"
        }.AsReadOnly();

        private static readonly IReadOnlyList<string> _doorItems = new List<string>
        {
            "Lỗ Mở Vách",
            "Cửa Đi"
        }.AsReadOnly();

        public static IReadOnlyList<string> RoomFinishItems => _roomFinishItems;
        public static IReadOnlyList<string> WallItems => _wallItems;
        public static IReadOnlyList<string> DoorItems => _doorItems;
    }
}
