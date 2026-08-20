using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using QS3D.Core.Features;

namespace QS3D.BricsCAD.V25.UI
{
    /// <summary>
    /// Projects the Workspace reference tree from the canonical Feature Navigation Registry while
    /// preserving legacy ElementCategory tags during the strangler migration.
    /// </summary>
    internal static class ReferenceWorkspaceTreeAugmenter
    {
        private static readonly object RegistrationGate = new object();
        private static bool _registered;

        public static bool EnsureRegistered()
        {
            lock (RegistrationGate)
            {
                if (_registered) return true;
                try
                {
                    EventManager.RegisterClassHandler(
                        typeof(WorkspacePanel),
                        FrameworkElement.LoadedEvent,
                        new RoutedEventHandler(OnWorkspaceLoaded),
                        true);
                    WorkspaceFeatureSelectionPublisher.EnsureRegistered();
                    _registered = true;
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static void OnWorkspaceLoaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is WorkspacePanel panel) || panel.ModelTree == null) return;
            EnsureReferenceTree(panel.ModelTree);
        }

        private static void EnsureReferenceTree(TreeView tree)
        {
            var navigation = WorkspaceFeatureNavigationCatalog.Navigation;
            var groupItems = navigation.Groups.ToDictionary(
                group => group.Key,
                group => EnsureTopAlias(tree, group.LabelKey, group.LegacyLabels, group.LegacyCategory?.ToString()),
                StringComparer.OrdinalIgnoreCase);

            if (groupItems.TryGetValue("slab", out var slab))
                MoveLegacyTopLevelUnder(tree, slab, "Mái Hắt");
            if (groupItems.TryGetValue("other", out var other))
                MoveLegacyTopLevelUnder(tree, other, "Modeling");

            foreach (var registration in navigation.Registrations)
            {
                if (!groupItems.TryGetValue(registration.GroupKey, out var group))
                    continue;

                TreeViewItem parent = group;
                if (string.Equals(registration.GroupKey, "slab-canopy", StringComparison.OrdinalIgnoreCase))
                {
                    var slabParent = groupItems["slab"];
                    parent = EnsureChildContainer(slabParent, group.Header as string ?? "Mái Hắt", null);
                    if (!ReferenceEquals(group, parent))
                        tree.Items.Remove(group);
                }

                var leaf = EnsureChild(parent, registration.LabelKey, registration.LegacyCategory?.ToString());
                WorkspaceFeatureSelectionPublisher.Attach(leaf, registration.FeatureId);
            }

            NormalizeReferenceTopLevelOrder(tree, navigation);
            NormalizeRegisteredChildren(groupItems, navigation);
        }

        private static void NormalizeRegisteredChildren(
            System.Collections.Generic.Dictionary<string, TreeViewItem> groupItems,
            FeatureNavigationRegistry navigation)
        {
            foreach (var group in navigation.Groups)
            {
                if (string.Equals(group.Key, "slab-canopy", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (!groupItems.TryGetValue(group.Key, out var parent))
                    continue;

                var registrations = navigation.Registrations
                    .Where(x => string.Equals(x.GroupKey, group.Key, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                for (var index = 0; index < registrations.Length; index++)
                {
                    var item = parent.Items.OfType<TreeViewItem>()
                        .FirstOrDefault(candidate => HeaderEquals(candidate, registrations[index].LabelKey));
                    if (item == null) continue;
                    var currentIndex = parent.Items.IndexOf(item);
                    if (currentIndex == index) continue;
                    parent.Items.Remove(item);
                    parent.Items.Insert(index, item);
                }
            }

            if (groupItems.TryGetValue("slab", out var slab))
            {
                var canopy = slab.Items.OfType<TreeViewItem>().FirstOrDefault(x => HeaderEquals(x, "Mái Hắt"));
                if (canopy != null)
                {
                    var registrations = navigation.Registrations
                        .Where(x => string.Equals(x.GroupKey, "slab-canopy", StringComparison.OrdinalIgnoreCase))
                        .ToArray();
                    for (var index = 0; index < registrations.Length; index++)
                    {
                        var item = canopy.Items.OfType<TreeViewItem>()
                            .FirstOrDefault(candidate => HeaderEquals(candidate, registrations[index].LabelKey));
                        if (item == null) continue;
                        var currentIndex = canopy.Items.IndexOf(item);
                        if (currentIndex == index) continue;
                        canopy.Items.Remove(item);
                        canopy.Items.Insert(index, item);
                    }
                }
            }
        }

        private static TreeViewItem EnsureTopAlias(
            TreeView tree,
            string header,
            System.Collections.Generic.IEnumerable<string> legacyHeaders,
            string? tag)
        {
            var item = FindTop(tree, header);
            if (item == null)
            {
                foreach (var legacyHeader in legacyHeaders)
                {
                    item = FindTop(tree, legacyHeader);
                    if (item != null) break;
                }
            }

            if (item == null)
            {
                item = NewItem(header, tag);
                tree.Items.Add(item);
            }
            else
            {
                item.Header = header;
                if (item.Tag == null && !string.IsNullOrWhiteSpace(tag))
                    item.Tag = tag;
            }
            return item;
        }

        private static TreeViewItem EnsureChildContainer(TreeViewItem parent, string header, string? tag)
        {
            var child = parent.Items.OfType<TreeViewItem>()
                .FirstOrDefault(candidate => HeaderEquals(candidate, header));
            if (child == null)
            {
                child = NewItem(header, tag);
                parent.Items.Add(child);
            }
            else if (child.Tag == null && !string.IsNullOrWhiteSpace(tag))
            {
                child.Tag = tag;
            }
            return child;
        }

        private static TreeViewItem EnsureChild(TreeViewItem parent, string header, string? tag)
        {
            var child = parent.Items.OfType<TreeViewItem>()
                .FirstOrDefault(candidate => HeaderEquals(candidate, header));
            if (child == null)
            {
                child = NewItem(header, tag);
                parent.Items.Add(child);
            }
            else if (child.Tag == null && !string.IsNullOrWhiteSpace(tag))
            {
                child.Tag = tag;
            }
            return child;
        }

        private static void MoveLegacyTopLevelUnder(TreeView tree, TreeViewItem parent, string legacyHeader)
        {
            var legacy = FindTop(tree, legacyHeader);
            if (legacy == null || ReferenceEquals(legacy, parent))
                return;

            tree.Items.Remove(legacy);
            if (!parent.Items.OfType<TreeViewItem>().Any(candidate => HeaderEquals(candidate, legacyHeader)))
            {
                parent.Items.Add(legacy);
                return;
            }

            var existing = parent.Items.OfType<TreeViewItem>()
                .First(candidate => HeaderEquals(candidate, legacyHeader));
            foreach (var child in legacy.Items.OfType<TreeViewItem>().ToList())
            {
                legacy.Items.Remove(child);
                if (!existing.Items.OfType<TreeViewItem>().Any(candidate => HeaderEquals(candidate, child.Header as string ?? string.Empty)))
                    existing.Items.Add(child);
            }
        }

        private static void NormalizeReferenceTopLevelOrder(TreeView tree, FeatureNavigationRegistry navigation)
        {
            var topLevel = navigation.Groups
                .Where(group => !string.Equals(group.Key, "slab-canopy", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            for (var index = 0; index < topLevel.Length; index++)
            {
                var item = FindTop(tree, topLevel[index].LabelKey);
                if (item == null) continue;
                var currentIndex = tree.Items.IndexOf(item);
                if (currentIndex == index) continue;
                tree.Items.Remove(item);
                tree.Items.Insert(index, item);
            }
        }

        private static TreeViewItem? FindTop(TreeView tree, string header) =>
            tree.Items.OfType<TreeViewItem>()
                .FirstOrDefault(candidate => HeaderEquals(candidate, header));

        private static TreeViewItem NewItem(string header, string? tag) =>
            new TreeViewItem { Header = header, Tag = tag };

        private static bool HeaderEquals(TreeViewItem item, string expected) =>
            string.Equals(item.Header as string, expected, StringComparison.OrdinalIgnoreCase);
    }
}
