using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using QS3D.Core.Domain;

namespace QS3D.Core.Features
{
    public sealed class FeatureNavigationGroup
    {
        public FeatureNavigationGroup(string key, int order, string labelKey, ElementCategory? legacyCategory = null, IEnumerable<string>? legacyLabels = null)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Navigation group key cannot be blank.", nameof(key));
            if (string.IsNullOrWhiteSpace(labelKey)) throw new ArgumentException("Navigation group label key cannot be blank.", nameof(labelKey));
            Key = key.Trim();
            Order = order;
            LabelKey = labelKey.Trim();
            LegacyCategory = legacyCategory;
            LegacyLabels = SnapshotStrings(legacyLabels);
        }

        public string Key { get; }
        public int Order { get; }
        public string LabelKey { get; }
        public ElementCategory? LegacyCategory { get; }
        public IReadOnlyList<string> LegacyLabels { get; }

        internal static IReadOnlyList<string> SnapshotStrings(IEnumerable<string>? source)
        {
            var values = (source ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            return new ReadOnlyCollection<string>(values);
        }
    }

    public sealed class FeatureNavigationRegistration
    {
        public FeatureNavigationRegistration(
            FeatureId featureId,
            string groupKey,
            int order,
            string labelKey,
            ElementCategory? legacyCategory,
            string? iconKey = null,
            IEnumerable<string>? searchAliases = null)
        {
            if (string.IsNullOrWhiteSpace(groupKey)) throw new ArgumentException("Navigation group key cannot be blank.", nameof(groupKey));
            if (string.IsNullOrWhiteSpace(labelKey)) throw new ArgumentException("Navigation label key cannot be blank.", nameof(labelKey));
            FeatureId = featureId;
            GroupKey = groupKey.Trim();
            Order = order;
            LabelKey = labelKey.Trim();
            LegacyCategory = legacyCategory;
            IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey!.Trim();
            SearchAliases = FeatureNavigationGroup.SnapshotStrings(searchAliases);
        }

        public FeatureId FeatureId { get; }
        public string GroupKey { get; }
        public int Order { get; }
        public string LabelKey { get; }
        public ElementCategory? LegacyCategory { get; }
        public string? IconKey { get; }
        public IReadOnlyList<string> SearchAliases { get; }
    }

    public sealed class SelectedFeatureContext
    {
        public SelectedFeatureContext(FeatureNavigationRegistration navigation, FeatureDescriptor descriptor)
        {
            Navigation = navigation ?? throw new ArgumentNullException(nameof(navigation));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            if (Navigation.FeatureId != Descriptor.Id)
                throw new InvalidOperationException("Navigation registration and feature descriptor must have the same FeatureId.");
        }

        public FeatureId FeatureId => Descriptor.Id;
        public FeatureNavigationRegistration Navigation { get; }
        public FeatureDescriptor Descriptor { get; }
        public InteractionProfile InteractionProfile => Descriptor.InteractionProfile;
        public ElementCategory? LegacyCategory => Navigation.LegacyCategory;
    }

    public sealed class FeatureNavigationRegistry
    {
        private readonly FeatureRegistry _features;
        private readonly IReadOnlyList<FeatureNavigationGroup> _groups;
        private readonly IReadOnlyList<FeatureNavigationRegistration> _registrations;
        private readonly Dictionary<string, FeatureNavigationGroup> _groupsByKey;
        private readonly Dictionary<FeatureId, FeatureNavigationRegistration> _byId;

        public FeatureNavigationRegistry(
            FeatureRegistry features,
            IEnumerable<FeatureNavigationGroup> groups,
            IEnumerable<FeatureNavigationRegistration> registrations,
            bool requireEveryFeature = true)
        {
            _features = features ?? throw new ArgumentNullException(nameof(features));
            if (groups == null) throw new ArgumentNullException(nameof(groups));
            if (registrations == null) throw new ArgumentNullException(nameof(registrations));

            var groupArray = groups.ToArray();
            if (groupArray.Any(x => x == null)) throw new InvalidOperationException("Navigation groups cannot contain null values.");
            if (groupArray.GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Navigation registry contains duplicate group keys.");
            _groupsByKey = groupArray.ToDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);

            var registrationArray = registrations.ToArray();
            if (registrationArray.Any(x => x == null)) throw new InvalidOperationException("Navigation registrations cannot contain null values.");
            if (registrationArray.GroupBy(x => x.FeatureId).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Navigation registry contains duplicate FeatureId values.");
            foreach (var registration in registrationArray)
            {
                if (!_groupsByKey.ContainsKey(registration.GroupKey))
                    throw new InvalidOperationException("Navigation registration references an unknown group: " + registration.GroupKey);
                _features.GetRequired(registration.FeatureId);
            }

            if (requireEveryFeature)
            {
                var registered = new HashSet<FeatureId>(registrationArray.Select(x => x.FeatureId));
                var missing = _features.Descriptors.Where(x => !registered.Contains(x.Id)).Select(x => x.Id.ToString()).ToArray();
                if (missing.Length > 0)
                    throw new InvalidOperationException("Feature registry contains features missing from navigation: " + string.Join(", ", missing));
            }

            _groups = new ReadOnlyCollection<FeatureNavigationGroup>(groupArray.OrderBy(x => x.Order).ThenBy(x => x.Key, StringComparer.Ordinal).ToArray());
            _registrations = new ReadOnlyCollection<FeatureNavigationRegistration>(registrationArray
                .OrderBy(x => _groupsByKey[x.GroupKey].Order)
                .ThenBy(x => x.Order)
                .ThenBy(x => x.FeatureId)
                .ToArray());
            _byId = registrationArray.ToDictionary(x => x.FeatureId);
        }

        public IReadOnlyList<FeatureNavigationGroup> Groups => _groups;
        public IReadOnlyList<FeatureNavigationRegistration> Registrations => _registrations;

        public bool TrySelect(FeatureId id, out SelectedFeatureContext? context)
        {
            if (!_byId.TryGetValue(id, out var navigation))
            {
                context = null;
                return false;
            }
            context = new SelectedFeatureContext(navigation, _features.GetRequired(id));
            return true;
        }

        public SelectedFeatureContext SelectRequired(FeatureId id)
        {
            if (!TrySelect(id, out var context) || context == null)
                throw new KeyNotFoundException("Feature is not registered for navigation: " + id);
            return context;
        }
    }

    public static class WorkspaceFeatureNavigationCatalog
    {
        private static readonly InteractionProfile SelectionOnlyProfile = new InteractionProfile(
            FeatureOnSelectBehavior.SelectContext,
            Array.Empty<CreateRecipeDescriptor>(),
            null,
            Array.Empty<InteractionSurface>(),
            FeatureCapability.None);

        private static readonly FeatureNavigationGroup[] GroupDefinitions =
        {
            G("grid", 0, "Lưới Trục", ElementCategory.Grid),
            G("room-finishes", 1, "HT_Phong", null, "HT_Phòng"),
            G("beam", 2, "Dầm", ElementCategory.Beam),
            G("slab", 3, "Sàn", ElementCategory.Slab),
            G("slab-canopy", 4, "Mái Hắt", null),
            G("column", 5, "Cột", ElementCategory.Column),
            G("structural-wall", 6, "Vách", ElementCategory.StructuralWall),
            G("architectural-wall", 7, "Tường KT"),
            G("opening", 8, "Cửa"),
            G("stair", 9, "Cầu Thang", ElementCategory.Stair),
            G("foundation", 10, "Móng", ElementCategory.Foundation),
            G("earthwork", 11, "Đào đắp", ElementCategory.Earthwork),
            G("steel", 12, "Kết cấu thép"),
            G("other", 13, "Cấu kiện khác"),
            G("custom-quantity", 14, "KL Tùy chỉnh", ElementCategory.CustomQuantity)
        };

        private static readonly FeatureNavigationRegistration[] NavigationDefinitions =
        {
            F("model.grid.straight", "grid", 0, "Lưới Thẳng", ElementCategory.Grid, "grid", "axis"),
            F("model.grid.curved", "grid", 1, "Lưới Cong", ElementCategory.Grid, "grid", "arc"),
            F("model.room", "room-finishes", 0, "Phòng", ElementCategory.Room, "room"),
            F("model.floor-finish", "room-finishes", 1, "Sàn Hoàn Thiện", ElementCategory.FloorFinish, "floor finish"),
            F("model.waterproofing", "room-finishes", 2, "Chống Thấm", ElementCategory.Waterproofing, "waterproof"),
            F("model.skirting", "room-finishes", 3, "Chân Tường", ElementCategory.Skirting, "skirting"),
            F("model.wall-finish", "room-finishes", 4, "Hoàn Thiện Tường", ElementCategory.WallFinish, "wall finish"),
            F("model.ceiling-finish", "room-finishes", 5, "Trần Hoàn Thiện", ElementCategory.CeilingFinish, "ceiling finish"),
            F("model.ceiling-plaster", "room-finishes", 6, "Trát Trần", ElementCategory.CeilingFinish, "ceiling plaster"),
            F("model.railing", "room-finishes", 7, "Lan Can", ElementCategory.Railing, "railing"),
            F("model.beam.rectangular", "beam", 0, "Dầm HCN", ElementCategory.Beam, "beam"),
            F("model.beam.wall-tie", "beam", 1, "Giằng Tường", ElementCategory.Beam, "wall tie"),
            F("model.beam.lintel", "beam", 2, "Lanh Tô", ElementCategory.Beam, "lintel"),
            F("model.slab.solid", "slab", 0, "Sàn Đặc", ElementCategory.Slab, "slab"),
            F("model.slab.ramp", "slab", 1, "Đường Dốc", ElementCategory.Slab, "ramp"),
            F("model.slab.opening", "slab", 2, "Lỗ Mở Sàn", ElementCategory.Slab, "slab opening"),
            F("model.slab.canopy.area", "slab-canopy", 0, "Mái Hắt Diện Tích", ElementCategory.Slab, "canopy area"),
            F("model.slab.canopy.profile", "slab-canopy", 1, "Mái Hắt Biên Dạng", ElementCategory.Slab, "canopy profile"),
            F("model.column", "column", 0, "Cột", ElementCategory.Column, "column"),
            F("model.structural-wall", "structural-wall", 0, "Vách BTCT", ElementCategory.StructuralWall, "structural wall"),
            F("model.architectural-wall", "architectural-wall", 0, "Tường Gạch", ElementCategory.ArchitecturalWall, "brick wall"),
            F("model.glass-wall", "architectural-wall", 1, "Vách Kính", ElementCategory.GlassWall, "glass wall"),
            F("model.wall-pier", "architectural-wall", 2, "Trụ Tường", ElementCategory.WallPier, "wall pier"),
            F("model.wall-opening", "opening", 0, "Lỗ Mở Vách", ElementCategory.WallOpening, "wall opening"),
            F("model.door", "opening", 1, "Cửa Đi", ElementCategory.Door, "door"),
            F("model.stair", "stair", 0, "Cầu Thang", ElementCategory.Stair, "stair"),
            F("model.foundation.pile", "foundation", 0, "Cọc", ElementCategory.Foundation, "pile"),
            F("model.foundation.pile-cap", "foundation", 1, "Đài Cọc", ElementCategory.Foundation, "pile cap"),
            F("model.foundation-beam", "foundation", 2, "Dầm Móng", ElementCategory.Foundation, "foundation beam"),
            F("model.foundation.strip", "foundation", 3, "Móng Băng", ElementCategory.Foundation, "strip foundation"),
            F("model.foundation.raft", "foundation", 4, "Móng Bè", ElementCategory.Foundation, "raft foundation"),
            F("model.foundation.blinding", "foundation", 5, "Bê Tông Lót", ElementCategory.Foundation, "blinding concrete"),
            F("model.earthwork.foundation-pit", "earthwork", 0, "Đào đắp hố móng", ElementCategory.Earthwork, "foundation pit"),
            F("model.earthwork.mass", "earthwork", 1, "Khối Đất", ElementCategory.Earthwork, "earth mass"),
            F("model.earthwork.intersection", "earthwork", 2, "Khối giao đào", ElementCategory.Earthwork, "excavation intersection"),
            F("model.earthwork.net", "earthwork", 3, "Khối đất sau trừ", ElementCategory.Earthwork, "net earthwork"),
            F("quantity.custom.length", "custom-quantity", 0, "KL Chiều dài", ElementCategory.CustomQuantity, "length"),
            F("quantity.custom.area", "custom-quantity", 1, "KL Diện tích", ElementCategory.CustomQuantity, "area"),
            F("quantity.custom.volume", "custom-quantity", 2, "KL Thể tích", ElementCategory.CustomQuantity, "volume"),
            F("quantity.custom.profile", "custom-quantity", 3, "KL Biên dạng", ElementCategory.CustomQuantity, "profile"),
            F("quantity.custom.plane", "custom-quantity", 4, "KL Mặt phẳng", ElementCategory.CustomQuantity, "plane")
        };

        private static readonly FeatureRegistry Features = new FeatureRegistry(
            NavigationDefinitions.Select(x => new FeatureDescriptor(
                x.FeatureId, x.GroupKey, x.Order, x.LabelKey, SelectionOnlyProfile, x.IconKey)));

        public static FeatureNavigationRegistry Navigation { get; } =
            new FeatureNavigationRegistry(Features, GroupDefinitions, NavigationDefinitions);

        private static FeatureNavigationGroup G(string key, int order, string label, ElementCategory? category = null, params string[] legacyLabels) =>
            new FeatureNavigationGroup(key, order, label, category, legacyLabels);

        private static FeatureNavigationRegistration F(string id, string groupKey, int order, string label, ElementCategory category, params string[] aliases) =>
            new FeatureNavigationRegistration(new FeatureId(id), groupKey, order, label, category, null, aliases);
    }
}
