using System;
using System.Linq;
using QS3D.Core.Domain;
using QS3D.Core.Features;

namespace QS3D.Core.SmokeTests
{
    internal static class FeatureNavigationRegistrySmoke
    {
        public static void Run()
        {
            WorkspaceParityIsDeterministic();
            StableFeatureIdDoesNotDependOnLabel();
            SelectionResolvesInteractionProfileAndLegacyCategory();
            DuplicateAndMissingRegistrationsFailFast();
        }

        private static void WorkspaceParityIsDeterministic()
        {
            var navigation = WorkspaceFeatureNavigationCatalog.Navigation;
            var groupSnapshot = string.Join("|", navigation.Groups
                .Where(x => x.Key != "slab-canopy")
                .Select(x => x.LabelKey));
            const string expectedGroups =
                "Lưới Trục|HT_Phong|Dầm|Sàn|Cột|Vách|Tường KT|Cửa|Cầu Thang|Móng|Đào đắp|Kết cấu thép|Cấu kiện khác|KL Tùy chỉnh";
            Equal(expectedGroups, groupSnapshot, "Workspace top-level navigation parity changed.");
            Equal(41, navigation.Registrations.Count, "Workspace registered leaf count changed.");
            Equal("model.grid.straight", navigation.Registrations[0].FeatureId.Value, "First feature ordering changed.");
            Equal("quantity.custom.plane", navigation.Registrations[navigation.Registrations.Count - 1].FeatureId.Value, "Last feature ordering changed.");
        }

        private static void StableFeatureIdDoesNotDependOnLabel()
        {
            var profile = SelectionProfile();
            var id = new FeatureId("model.room");
            var features = new FeatureRegistry(new[]
            {
                new FeatureDescriptor(id, "group", 0, "Room.NewLocalizedLabel", profile)
            });
            var navigation = new FeatureNavigationRegistry(
                features,
                new[] { new FeatureNavigationGroup("group", 0, "Localized.Group") },
                new[] { new FeatureNavigationRegistration(id, "group", 0, "Localized.Room", ElementCategory.Room) });

            Equal(id, navigation.SelectRequired(id).FeatureId, "Visible/localized labels changed semantic FeatureId.");
        }

        private static void SelectionResolvesInteractionProfileAndLegacyCategory()
        {
            var context = WorkspaceFeatureNavigationCatalog.Navigation.SelectRequired(new FeatureId("model.room"));
            Equal(new FeatureId("model.room"), context.FeatureId, "Selected feature context lost FeatureId.");
            Equal(FeatureOnSelectBehavior.SelectContext, context.InteractionProfile.OnSelect, "Selected feature context did not resolve InteractionProfile.");
            Equal(ElementCategory.Room, context.LegacyCategory, "Legacy ElementCategory adapter changed.");
        }

        private static void DuplicateAndMissingRegistrationsFailFast()
        {
            var profile = SelectionProfile();
            var first = new FeatureDescriptor(new FeatureId("test.first"), "group", 0, "First", profile);
            var second = new FeatureDescriptor(new FeatureId("test.second"), "group", 1, "Second", profile);
            var features = new FeatureRegistry(new[] { first, second });
            var groups = new[] { new FeatureNavigationGroup("group", 0, "Group") };
            var one = new FeatureNavigationRegistration(first.Id, "group", 0, "First", ElementCategory.Room);

            Throws<InvalidOperationException>(
                () => new FeatureNavigationRegistry(features, groups, new[] { one, one }),
                "Duplicate navigation registration was accepted.");
            Throws<InvalidOperationException>(
                () => new FeatureNavigationRegistry(features, groups, new[] { one }),
                "Feature missing a navigation registration was accepted.");
        }

        private static InteractionProfile SelectionProfile() =>
            new InteractionProfile(
                FeatureOnSelectBehavior.SelectContext,
                Array.Empty<CreateRecipeDescriptor>(),
                null,
                Array.Empty<InteractionSurface>(),
                FeatureCapability.None);

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + " Actual=" + actual);
        }

        private static void Throws<T>(Action action, string message) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new InvalidOperationException(message);
        }
    }
}
