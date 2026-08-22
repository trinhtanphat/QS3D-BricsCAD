using System;
using System.Collections.Generic;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Diagnostics
{
    public sealed class RoomFinishHealthService
    {
        public IReadOnlyList<ModelHealthIssue> Inspect(ProjectState project)
        {
            if (project == null) throw new ArgumentNullException(nameof(project));
            var issues = new List<ModelHealthIssue>();

            foreach (var finish in project.Elements
                .Where(x => AutoRoomLifecycle.IsRoomFinishCategory(x.Category))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                string roomId;
                try
                {
                    roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, finish);
                }
                catch (InvalidOperationException ex)
                {
                    issues.Add(new ModelHealthIssue(
                        "ROOM_PROVENANCE_CONFLICT",
                        HealthSeverity.Error,
                        "HT_Phòng có nhiều Room provenance mâu thuẫn: " + ex.Message,
                        finish.Id));
                    continue;
                }

                if (roomId.Length == 0)
                {
                    issues.Add(new ModelHealthIssue(
                        "UNLINKED_ROOM_FINISH",
                        HealthSeverity.Warning,
                        "HT_Phòng chưa liên kết Room semantic. Schedule vẫn giữ dòng dưới nhãn chưa liên kết, nhưng cần gán Room để có provenance đầy đủ.",
                        finish.Id));
                    continue;
                }

                var room = project.FindElement(roomId);
                if (room == null)
                {
                    issues.Add(new ModelHealthIssue(
                        "ORPHAN_ROOM_FINISH",
                        HealthSeverity.Error,
                        "HT_Phòng tham chiếu Room không còn tồn tại: " + roomId + ". Dòng này bị loại khỏi BQ/Material/HT_Phòng schedule.",
                        finish.Id));
                    continue;
                }

                if (room.Category != ElementCategory.Room)
                {
                    issues.Add(new ModelHealthIssue(
                        "INVALID_ROOM_FINISH_PARENT",
                        HealthSeverity.Error,
                        "HT_Phòng tham chiếu element không phải Room: " + roomId + " (" + room.Category + "). Dòng này bị loại khỏi quantity.",
                        finish.Id));
                    continue;
                }

                if (!string.Equals(finish.FloorId, room.FloorId, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(finish.ZoneId, room.ZoneId, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add(new ModelHealthIssue(
                        "ROOM_FINISH_SCOPE_MISMATCH",
                        HealthSeverity.Error,
                        "HT_Phòng và Room không cùng Floor/Zone. Finish=" + finish.FloorId + "/" + finish.ZoneId +
                        ", Room=" + room.FloorId + "/" + room.ZoneId + ". Dòng này bị loại khỏi quantity.",
                        finish.Id));
                    continue;
                }

                if (AutoRoomLifecycle.IsStaleAutoRoom(room))
                {
                    issues.Add(new ModelHealthIssue(
                        "STALE_ROOM_FINISH",
                        HealthSeverity.Warning,
                        "HT_Phòng thuộc AutoRoom stale " + room.Id + "; quantity đang được loại an toàn cho tới khi Room được tái kích hoạt hoặc dữ liệu được repair.",
                        finish.Id));
                }
            }

            return issues.AsReadOnly();
        }
    }
}
