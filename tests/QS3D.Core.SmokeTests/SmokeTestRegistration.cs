namespace QS3D.Core.SmokeTests
{
    internal static class SmokeTestRegistration
    {
        internal static void RunAll()
        {
            ProjectQuantitySmoke.Run();
            PersistenceHardeningSmoke.Run();
            AdvancedDomainSmoke.Run();
            HardeningRegressionSmoke.Run();
            ReviewHardeningSmoke.Run();
            ContinuationRegressionSmoke.Run();
            LogicRegressionSmoke.Run();
            RevisionRegressionSmoke.Run();
            WorkflowPersistenceSmoke.Run();
            BbsRegressionSmoke.Run();
            WorkflowSafetySmoke.Run();
            CompletionRegressionSmoke.Run();
            SemanticOverflowSmoke.Run();
            RoomBoundaryRegressionSmoke.Run();
            GeometryCompletionSmoke.Run();
            AutoRoomLifecycleSmoke.Run();
            LinearRebarLayoutSmoke.Run();
            WallJunctionRegressionSmoke.Run();
            WallJunctionAdjustmentSmoke.Run();
            PolylineOpeningCutSmoke.Run();
            ProjectRebarShapeSmoke.Run();
            RebarOwnershipHealthSmoke.Run();
            GeneratedGeometryStaleSmoke.Run();
            CurtainWallLayoutSmoke.Run();
            WallPierProfileSmoke.Run();
            CurtainWallDetailSmoke.Run();
        }
    }
}
