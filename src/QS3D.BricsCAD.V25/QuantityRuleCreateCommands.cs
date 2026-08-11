using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Bricscad.ApplicationServices;
using Bricscad.EditorInput;
using QS3D.BricsCAD.V25.Services;
using QS3D.BricsCAD.V25.UI;
using QS3D.Core.Reporting;
using Teigha.Runtime;

namespace QS3D.BricsCAD.V25
{
    public sealed class QuantityRuleCreateCommands
    {
        [CommandMethod("QS3DRULECREATE", CommandFlags.Modal)]
        public void CreateIntersectionRule()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            try
            {
                var store = new QuantitySettingsStore();
                var settings = store.Load().Clone();
                settings.NormalizeAndValidate();

                var observedCodes = CollectObservedCodes(settings);
                if (observedCodes.Count == 0)
                {
                    Write(document, "QS3DRULECREATE: cấu hình hiện tại chưa có mã cấu kiện để tạo luật. Mở QS3DSETUP hoặc nạp template trước.");
                    return;
                }

                Write(document, "QS3DRULECREATE: mã cấu kiện hiện có: " + string.Join(", ", observedCodes
                    .OrderBy(x => QuantityCategoryDisplayName.Resolve(x), StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x)
                    .Select(x => x + "=" + QuantityCategoryDisplayName.Resolve(x))));

                var source = PromptCategoryCode(document, "\nMã cấu kiện chính A (luật A -> B): ");
                if (!source.HasValue) return;
                if (!observedCodes.Contains(source.Value))
                {
                    Write(document, "QS3DRULECREATE: mã cấu kiện chính " + source.Value + " không có trong cấu hình hiện tại. Không tạo mã mới ngầm.");
                    return;
                }

                var target = PromptCategoryCode(document, "\nMã cấu kiện tham chiếu B (luật A -> B): ");
                if (!target.HasValue) return;
                if (!observedCodes.Contains(target.Value))
                {
                    Write(document, "QS3DRULECREATE: mã cấu kiện tham chiếu " + target.Value + " không có trong cấu hình hiện tại. Không tạo mã mới ngầm.");
                    return;
                }

                if (settings.FindIntersectionRule(source.Value, target.Value) != null)
                {
                    Write(document, "QS3DRULECREATE: luật " + source.Value + " -> " + target.Value + " đã tồn tại. Mở QS3DSETUP để chỉnh luật hiện có.");
                    return;
                }

                var sourceName = QuantityCategoryDisplayName.Resolve(source.Value);
                var targetName = QuantityCategoryDisplayName.Resolve(target.Value);
                var confirm = document.Editor.GetKeywords(
                    "\nTạo luật " + sourceName + " -> " + targetName + " với mọi phép trừ mặc định TẮT? [Yes/No] <No>: ",
                    "Yes No");
                if (confirm.Status != PromptStatus.OK || !string.Equals(confirm.StringResult, "Yes", StringComparison.OrdinalIgnoreCase))
                {
                    Write(document, "QS3DRULECREATE: đã hủy, cấu hình không thay đổi.");
                    return;
                }

                var latestSettings = store.Load().Clone();
                latestSettings.NormalizeAndValidate();
                var latestObservedCodes = CollectObservedCodes(latestSettings);
                if (!latestObservedCodes.Contains(source.Value) || !latestObservedCodes.Contains(target.Value))
                {
                    Write(document, "QS3DRULECREATE: cấu hình đã thay đổi trong lúc nhập; một mã cấu kiện đã không còn tồn tại. Không ghi dữ liệu stale, hãy chạy lại.");
                    return;
                }
                if (latestSettings.FindIntersectionRule(source.Value, target.Value) != null)
                {
                    Write(document, "QS3DRULECREATE: cấu hình đã thay đổi trong lúc nhập và luật " + source.Value + " -> " + target.Value + " hiện đã tồn tại. Không ghi đè thay đổi mới.");
                    return;
                }

                latestSettings.IntersectionRules.Add(new QuantityIntersectionRuleSetting
                {
                    Source = source.Value,
                    Target = target.Value
                });
                latestSettings.NormalizeAndValidate();
                store.Save(latestSettings);

                Write(document,
                    "QS3DRULECREATE: đã tạo luật " + sourceName + " -> " + targetName +
                    " (mọi phép trừ đang TẮT). Mở QS3DSETUP để bật đúng các phép trừ cần dùng. Luật chiều ngược không được tự tạo.");
            }
            catch (Exception ex)
            {
                Write(document, "QS3DRULECREATE lỗi: " + ex.Message);
            }
        }

        [CommandMethod("QS3DINTERSECTIONRULECREATE", CommandFlags.Modal)]
        public void CreateIntersectionRuleLongName()
        {
            CreateIntersectionRule();
        }

        private static HashSet<int> CollectObservedCodes(QuantityCalculationSettings settings) =>
            new HashSet<int>(
                settings.CategoryRules.Select(x => x.Category)
                    .Concat(settings.IntersectionRules.Select(x => x.Source))
                    .Concat(settings.IntersectionRules.Select(x => x.Target)));

        private static int? PromptCategoryCode(Document document, string message)
        {
            var options = new PromptStringOptions(message)
            {
                AllowSpaces = false
            };
            var result = document.Editor.GetString(options);
            if (result.Status != PromptStatus.OK) return null;

            var text = (result.StringResult ?? string.Empty).Trim();
            int value;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 0)
                throw new InvalidOperationException("Mã cấu kiện phải là số nguyên không âm có trong cấu hình hiện tại.");
            return value;
        }

        private static void Write(Document document, string message)
        {
            document.Editor.WriteMessage("\nQS3D " + message);
        }
    }
}
