using System;
using System.IO;

namespace QS3D.Core.SmokeTests
{
    internal static class CommercialQsWorkspaceSurfaceSmoke
    {
        internal static void Run()
        {
            var root = FindRepositoryRoot();
            var commandPath = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "CommercialQsCommands.cs");
            var xamlPath = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "UI", "CommercialQsWindow.xaml");
            var codeBehindPath = Path.Combine(root, "src", "QS3D.BricsCAD.V25", "UI", "CommercialQsWindow.xaml.cs");

            Require(File.Exists(commandPath), "Commercial QS workspace command source is missing.");
            Require(File.Exists(xamlPath), "Commercial QS workspace XAML source is missing.");
            Require(File.Exists(codeBehindPath), "Commercial QS workspace code-behind source is missing.");

            var commandSource = File.ReadAllText(commandPath);
            var xamlSource = File.ReadAllText(xamlPath);
            var codeBehindSource = File.ReadAllText(codeBehindPath);

            Require(commandSource.Contains("[CadCommandMethod(\"QS3DCOMMERCIAL\")]", StringComparison.Ordinal),
                "Commercial QS workspace must expose the QS3DCOMMERCIAL command.");
            Require(commandSource.Contains("ValidateForCommercialAction(\"Open Commercial QS Workspace\")", StringComparison.Ordinal),
                "QS3DCOMMERCIAL must retain the production commercial-action hardening gate.");
            Require(commandSource.Contains("LicenseGate.EnsureCanUseInteractive", StringComparison.Ordinal),
                "QS3DCOMMERCIAL must retain the interactive license gate.");
            Require(commandSource.Contains("WarnIfSupportWindowNearEnd(editor, \"QS3DCOMMERCIAL\")", StringComparison.Ordinal),
                "QS3DCOMMERCIAL must retain product support-window warning behavior.");
            Require(commandSource.Contains("CommercialQsWindow.ShowModeless(doc)", StringComparison.Ordinal),
                "QS3DCOMMERCIAL must open the modeless Commercial QS workspace.");

            Require(xamlSource.Contains("Header=\"Variation\"", StringComparison.Ordinal),
                "Commercial QS workspace must expose a Variation surface.");
            Require(xamlSource.Contains("Header=\"IPC\"", StringComparison.Ordinal),
                "Commercial QS workspace must expose an IPC surface.");
            Require(xamlSource.Contains("Header=\"Final Account\"", StringComparison.Ordinal),
                "Commercial QS workspace must expose a Final Account surface.");
            Require(xamlSource.Contains("SelectedVariationContext", StringComparison.Ordinal),
                "Commercial QS workspace must keep selected Variation context visible across the workflow.");

            Require(codeBehindSource.Contains("CommercialVariationRegister", StringComparison.Ordinal),
                "Commercial QS workspace must bind Variation state to CommercialVariationRegister.");
            Require(codeBehindSource.Contains("ProgressClaimService", StringComparison.Ordinal),
                "Commercial QS workspace must obtain ProgressClaimResult through ProgressClaimService.");
            Require(codeBehindSource.Contains("InterimPaymentCertificateService", StringComparison.Ordinal),
                "Commercial QS workspace must bind IPC calculations to InterimPaymentCertificateService.");
            Require(codeBehindSource.Contains("FinalAccountService", StringComparison.Ordinal),
                "Commercial QS workspace must bind Final Account reconciliation to FinalAccountService.");
            Require(codeBehindSource.Contains("DocumentBoundWindowLifetime.Attach", StringComparison.Ordinal),
                "Commercial QS workspace must remain bound to the BricsCAD document lifetime.");
            Require(codeBehindSource.Contains("Application.ShowModelessWindow", StringComparison.Ordinal),
                "Commercial QS workspace must use the existing BricsCAD modeless host path.");

            Require(!codeBehindSource.Contains("GrossCertifiedThisPeriod =", StringComparison.Ordinal),
                "Commercial QS adapter must not duplicate gross-certified arithmetic.");
            Require(!codeBehindSource.Contains("FinalContractValue =", StringComparison.Ordinal),
                "Commercial QS adapter must not duplicate final-account arithmetic.");
        }

        private static string FindRepositoryRoot()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current != null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "QS3D.sln")) &&
                    Directory.Exists(Path.Combine(current.FullName, "src")))
                    return current.FullName;
            }

            throw new InvalidOperationException("Could not locate the QS3D repository root for the Commercial QS workspace source guard.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
