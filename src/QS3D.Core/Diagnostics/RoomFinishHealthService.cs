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
            var elements = new List<ProjectElement>(project.Elements.Count);
            foreach (var element in project.Elements)
            {
                if (element == null)
                    throw new InvalidOperationException("Room-finish diagnostics cannot inspect a project containing a null semantic element.");
                elements.Add(element);
            }
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var byId = new Dictionary<string, ProjectElement>(StringComparer.OrdinalIgnoreCase);
            foreach (var element in elements)
            {
                if (!counts.TryGetValue(element.Id, out var count)) count = 0;
                counts[element.Id] = count + 1;
                if (!byId.ContainsKey(element.Id)) byId[element.Id] = element;
            }
            var duplicateIds = new HashSet<string>(counts.Where(x => x.Value > 1).Select(x => x.Key), StringComparer.OrdinalIgnoreCase);
            var identityGroups = new Dictionary<string, List<ProjectElement>>(StringComparer.OrdinalIgnoreCase);
            var identityRoomIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var finish in elements
                .Where(x => AutoRoomLifecycle.IsRoomFinishCategory(x.Category))
                .OrderBy(x => x.Id, StringComparer.OrdinalIgnoreCase))
            {
                string roomId;
                try
                {
                    roomId = AutoRoomLifecycle.ResolveRoomReferenceId(project, finish);
                }
                catch (InvalidOperationException)
                {
                    issues.Add(new ModelHealthIssue(
                        "ROOM_PROVENANCE_CONFLICT",
                        HealthSeverity.Error,
                        "HT_Phòng có Room provenance mâu thuẫn và không thể phân giải an toàn. Cần sửa Room provenance trước khi quantity/release.",
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

                if (duplicateIds.Contains(roomId))
                {
                    issues.Add(new ModelHealthIssue(
                        "AMBIGUOUS_ROOM_FINISH_PARENT",
                        HealthSeverity.Error,
                        "HT_Phòng tham chiếu mã Room/element bị trùng: " + roomId + ". Dòng này bị loại khỏi quantity cho tới khi identity được repair.",
                        finish.Id));
                    continue;
                }

                if (!byId.TryGetValue(roomId, out var room))
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

                var identityKey = room.Id + "\u001f" + finish.Category;
                if (!identityGroups.TryGetValue(identityKey, out var group))
                {
                    group = new List<ProjectElement>();
                    identityGroups[identityKey] = group;
                    identityRoomIds[identityKey] = room.Id;
                }
                group.Add(finish);

                if (AutoRoomLifecycle.IsStaleAutoRoom(room))
                {
                    issues.Add(new ModelHealthIssue(
                        "STALE_ROOM_FINISH",
                        HealthSeverity.Warning,
                        "HT_Phòng thuộc AutoRoom stale " + room.Id + "; quantity đang được loại an toàn cho tới khi Room được tái kích hoạt hoặc dữ liệu được repair.",
                        finish.Id));
                }
            }

            foreach (var pair in identityGroups.Where(x => x.Value.Count > 1))
            {
                var group = pair.Value;
                var roomId = identityRoomIds[pair.Key];
                var ids = string.Join(", ", group.Select(x => x.Id).OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                foreach (var finish in group)
                    issues.Add(new ModelHealthIssue(
                        "DUPLICATE_ROOM_FINISH",
                        HealthSeverity.Error,
                        "Nhiều " + finish.Category + " cùng tham chiếu Room " + roomId + ": " + ids + ". BQ/Material/HT_Phòng schedule fail closed để tránh cộng đôi khối lượng.",
                        finish.Id));
            }

            return issues.AsReadOnly();
        }
    }
}
