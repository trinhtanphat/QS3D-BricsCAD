using System;
using System.Collections.Generic;
using System.Globalization;
using Bricscad.ApplicationServices;
using QS3D.BricsCAD.V25.UI;
using Teigha.Runtime;
using Application = Bricscad.ApplicationServices.Application;

namespace QS3D.BricsCAD.V25
{
    public sealed class GraphicsOptimizationCommands
    {
        [CommandMethod("QS3DOPTIMIZEGRAPHICS", CommandFlags.Modal)]
        public void OptimizeGraphics()
        {
            var document = Application.DocumentManager.MdiActiveDocument;
            if (document == null) return;

            var details = new List<string>();
            var changed = 0;
            var failures = 0;

            ApplySetting("RETAINEDGRAPHICS", 1, details, ref changed, ref failures);
            ApplySetting("RENDERUSINGHARDWARE", 1, details, ref changed, ref failures);

            try
            {
                document.Editor.Regen();
                details.Add("viewport regen");
            }
            catch (Exception ex)
            {
                failures++;
                details.Add("regen không áp dụng: " + ex.Message);
            }

            var message = failures == 0
                ? "Tối ưu đồ họa: đã xác nhận retained graphics và ưu tiên GPU; " + changed + " thiết lập được thay đổi."
                : "Tối ưu đồ họa: áp dụng một phần; " + failures + " bước không thể hoàn tất trên host hiện tại.";

            PaletteCoordinator.SetStatus(message);
            document.Editor.WriteMessage(
                "\nQS3D " + message +
                " " + string.Join("; ", details) +
                ". RENDERUSINGHARDWARE có thể cần khởi động lại BricsCAD để áp dụng hoàn toàn.");
        }

        private static void ApplySetting(
            string name,
            short target,
            ICollection<string> details,
            ref int changed,
            ref int failures)
        {
            try
            {
                var current = Application.GetSystemVariable(name);
                var currentNumber = Convert.ToInt32(current, CultureInfo.InvariantCulture);
                if (currentNumber == target)
                {
                    details.Add(name + " đã=" + target.ToString(CultureInfo.InvariantCulture));
                    return;
                }

                Application.SetSystemVariable(name, CoerceTarget(current, target));
                changed++;
                details.Add(name + "=" + target.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                failures++;
                details.Add(name + " không áp dụng: " + ex.Message);
            }
        }

        private static object CoerceTarget(object current, short target)
        {
            if (current is bool) return target != 0;
            if (current is short) return target;
            if (current is int) return (int)target;
            if (current is long) return (long)target;
            if (current is byte) return (byte)target;
            return target;
        }
    }
}
