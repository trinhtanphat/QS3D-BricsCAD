using System;
using System.Linq;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureInteractionContractsSmoke
    {
        public static void Run()
        {
            RoomDirectAddIsRepresentable();
            FormDrivenFeatureIsRepresentable();
            RegistryIsDeterministicAndUnique();
            InvalidProfilesFailClosed();
            WorkspaceInteractionSafetyContractsSmoke.Run();
        }

        private static void RoomDirectAddIsRepresentable()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("room.direct", CreateInputMode.Direct) },
                "room.direct",
                new[] { InteractionSurface.PrimaryInspector },
                FeatureCapability.Create | FeatureCapability.EditParameters | FeatureCapability.Quantity,
                dependencyPolicyKey: "room.host-dependencies",
                semanticMappingKey: "room");

            if (profile.Recipes.Count != 1 || profile.Recipes[0].InputMode != CreateInputMode.Direct)
                throw new Exception("Room must be representable as a direct primary Add recipe.");
            if (profile.AllowsModal)
                throw new Exception("Direct Room creation must not require modal interaction.");
        }

        private static void FormDrivenFeatureIsRepresentable()
        {
            var profile = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("floor-finish.from-room", CreateInputMode.FormThenCreate, "finish.material-thickness") },
                "floor-finish.from-room",
                new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector },
                FeatureCapability.Create | FeatureCapability.EditParameters | FeatureCapability.Material | FeatureCapability.Quantity,
                allowsModal: true,
                propertySchemaKey: "finish.material-thickness",
                dependencyPolicyKey: "room-host",
                semanticMappingKey: "floor-finish");

            if (!profile.Recipes[0].RequiresForm || profile.PersistentSurfaces.Count != 2)
                throw new Exception("A form-driven feature must support schema input and two inspector slots.");
        }

        private static void RegistryIsDeterministicAndUnique()
        {
            var direct = new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("direct", CreateInputMode.Direct) },
                "direct",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create);

            var registry = new FeatureRegistry(new[]
            {
                new FeatureDescriptor(new FeatureId("model.wall"), "model", 20, "Feature.Wall", direct),
                new FeatureDescriptor(new FeatureId("model.room"), "model", 10, "Feature.Room", direct)
            });

            var ids = registry.Descriptors.Select(x => x.Id.ToString()).ToArray();
            if (!ids.SequenceEqual(new[] { "model.room", "model.wall" }))
                throw new Exception("Feature registry enumeration must be deterministic by group/order/id.");
            if (!registry.TryGet(new FeatureId("MODEL.ROOM"), out var room) || room == null || room.LabelKey != "Feature.Room")
                throw new Exception("FeatureId lookup must use stable canonical identity rather than visible labels.");

            ExpectInvalid(() => new FeatureRegistry(new[]
            {
                new FeatureDescriptor(new FeatureId("model.room"), "model", 1, "One", direct),
                new FeatureDescriptor(new FeatureId("MODEL.ROOM"), "model", 2, "Two", direct)
            }), "Duplicate canonical FeatureId values must fail closed.");
        }

        private static void InvalidProfilesFailClosed()
        {
            ExpectInvalid(() => new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("direct", CreateInputMode.Direct) },
                "missing",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create),
                "Primary recipe missing from the recipe set must fail closed.");

            ExpectInvalid(() => new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("form", CreateInputMode.FormThenCreate) },
                "form",
                Array.Empty<InteractionSurface>(),
                FeatureCapability.Create,
                allowsModal: true),
                "Form-driven recipes without schema metadata must fail closed.");

            ExpectInvalid(() => new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                new[] { new CreateRecipeDescriptor("form", CreateInputMode.FormThenCreate, "schema") },
                "form",
                new[] { InteractionSurface.PrimaryInspector, InteractionSurface.SecondaryInspector, InteractionSurface.PrimaryInspector },
                FeatureCapability.Create,
                allowsModal: true),
                "More than two normal persistent surfaces must fail closed.");
        }

        private static void ExpectInvalid(Action action, string message)
        {
            try
            {
                action();
            }
            catch (InvalidOperationException)
            {
                return;
            }

            throw new Exception(message);
        }
    }
}
