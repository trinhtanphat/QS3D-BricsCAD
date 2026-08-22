using System;
using Bricscad.ApplicationServices;

namespace QS3D.BricsCAD.V25.Cad
{
    internal static class CadPostCommitUi
    {
        public static void TryRegen(Document document, string operation)
        {
            if (document == null) return;
            try
            {
                document.Editor.Regen();
            }
            catch (Exception ex)
            {
                try
                {
                    document.Editor.WriteMessage(
                        "\nQS3D " + operation + " đã commit; viewport regen warning: " + ex.Message);
                }
                catch
                {
                }
            }
        }
    }
}
