using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using QS3D.Core.Export;

namespace QS3D.Core.SmokeTests
{
    internal static class ProjectInterchangeResolutionCapacitySmoke
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            var field = typeof(ProjectInterchangeImportResolutionPlanner).GetField(
                "MaxPlanItems",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (field == null)
                throw new InvalidOperationException("ProjectInterchangeResolutionCapacitySmoke could not locate MaxPlanItems.");

            var raw = field.GetRawConstantValue();
            if (!(raw is int maxPlanItems))
                throw new InvalidOperationException("ProjectInterchangeResolutionCapacitySmoke expected MaxPlanItems to remain an int constant.");
            if (maxPlanItems != ProjectInterchangeJsonValidator.MaxCollectionItems)
                throw new InvalidOperationException(
                    "ProjectInterchangeResolutionCapacitySmoke expected planner capacity " +
                    ProjectInterchangeJsonValidator.MaxCollectionItems +
                    " but got " + maxPlanItems + ".");
            if (maxPlanItems < ProjectInterchangeJsonValidator.MaxElements)
                throw new InvalidOperationException("Resolution planning cannot be narrower than the validated element-count contract.");
        }
    }
}
