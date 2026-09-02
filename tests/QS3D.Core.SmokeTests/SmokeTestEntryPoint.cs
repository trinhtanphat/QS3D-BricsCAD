using System;
using System.Reflection;

namespace QS3D.Core.SmokeTests
{
    internal static class SmokeTestEntryPoint
    {
        private static int Main()
        {
            try
            {
                Bim4dScheduleFoundationSmoke.Run();
                AddCreateStateMachineSmoke.Run();
                FeatureNavigationRegistrySmoke.Run();
                WorkspaceModalPrimitivesSmoke.Run();
                PolygonScanlineOrientationSmoke.Run();
                PolygonSourceLoopRegionAssemblerSmoke.Run();
                BltLegacyAdapterSmoke.Run();
                QuantityEvidenceGraphSmoke.Run();
                QsCustomerWorkbookDgklLayoutSmoke.Run();
                CoordinationIssuePersistenceSmoke.Run();
                CoordinationIssueExcelLifecycleSmoke.Run();
                CoordinationIssueExcelWorkbookSmoke.Run();
                CoordinationIssueExcelSheetRelationshipSmoke.Run();
                CoordinationDirtyBatchAtomicitySmoke.Run();
                WallJunctionSnapPreviewRevisionSmoke.Run();
                ProjectElementKeyControlSmoke.Run();
                ProjectMeasurementWorkItemMappingIdentitySmoke.Run();
                MeasurementTraceKnownCountStabilitySmoke.Run();
                WallFormworkContactSmoke.Run();
                CommercialSubtractionPrecisionSmoke.Run();
                BeamCoreFormworkRegeneratorSmoke.Run();

                var legacyMain = typeof(Program).GetMethod(
                    "Main",
                    BindingFlags.NonPublic | BindingFlags.Static);
                if (legacyMain == null ||
                    legacyMain.ReturnType != typeof(int) ||
                    legacyMain.GetParameters().Length != 0)
                {
                    Console.Error.WriteLine(
                        "FAIL smoke runner: legacy Program.Main() entry point is unavailable or has an unexpected signature.");
                    return 1;
                }

                var result = legacyMain.Invoke(null, null);
                if (result is int exitCode)
                    return exitCode;

                Console.Error.WriteLine(
                    "FAIL smoke runner: legacy Program.Main() returned an unexpected result.");
                return 1;
            }
            catch (TargetInvocationException ex)
            {
                var actual = ex.InnerException ?? ex;
                Console.Error.WriteLine(
                    "FAIL smoke runner: " + actual.GetType().FullName + ": " + actual.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    "FAIL smoke runner: " + ex.GetType().FullName + ": " + ex.Message);
                return 1;
            }
        }
    }
}
