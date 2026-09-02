using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace QS3D.BricsCAD.V25
{
    internal sealed class McpTransportProfile
    {
        internal McpTransportProfile(
            string id,
            McpTransportProvider provider,
            string displayName,
            bool enabled,
            bool autoStart,
            bool isLegacyDefault,
            string registrationIdentity)
        {
            Id = id ?? string.Empty;
            Provider = provider;
            DisplayName = displayName ?? string.Empty;
            Enabled = enabled;
            AutoStart = autoStart;
            IsLegacyDefault = isLegacyDefault;
            RegistrationIdentity = registrationIdentity ?? string.Empty;
        }

        internal string Id { get; }
        internal McpTransportProvider Provider { get; }
        internal string DisplayName { get; }
        internal bool Enabled { get; }
        internal bool AutoStart { get; }
        internal bool IsLegacyDefault { get; }
        internal string RegistrationIdentity { get; }
    }

    /// <summary>
    /// Secret-free, versioned registry for external MCP transport profiles. This registry owns
    /// only profile metadata/migration/status. Provider process supervisors remain separate and
    /// all transports continue to terminate at the one embedded MCP server.
    /// </summary>
    internal static class McpTransportProfileRegistry
    {
        private const int SchemaVersion = 1;
        private const int MaxDisplayNameLength = 120;
        private const int MaxRegistrationIdentityLength = 2048;
        private const string LegacyDefaultName = "legacy-default";
        private const string RecoveryProfileId = "00000000000000000000000000000001";
        private static readonly object Sync = new object();
        private static string _lastError = string.Empty;

        private static string SettingsDirectory => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "QS3D", "MCP", "Transport");
        private static string RegistryPath => Path.Combine(SettingsDirectory, "profiles-v1.txt");
        private static string RegistrationPath => Path.Combine(SettingsDirectory, "profiles-v1-registration.txt");

        internal static IReadOnlyList<McpTransportProfile> LoadProfiles()
        {
            lock (Sync)
            {
                List<McpTransportProfile> profiles;
                string error;
                if (!File.Exists(RegistryPath))
                {
                    var created = CreateLegacyDefaultProfile(false);
                    SaveProfiles(new List<McpTransportProfile> { created });
                    _lastError = string.Empty;
                    return new List<McpTransportProfile> { created }.AsReadOnly();
                }

                if (!TryReadProfiles(out profiles, out error))
                {
                    _lastError = SanitizeError(error);
                    return new List<McpTransportProfile> { CreateLegacyDefaultProfile(true) }.AsReadOnly();
                }

                _lastError = string.Empty;
                return profiles.AsReadOnly();
            }
        }

        internal static McpTransportProfile EnsureLegacyDefaultProfile()
        {
            lock (Sync)
            {
                if (!File.Exists(RegistryPath))
                {
                    var created = CreateLegacyDefaultProfile(false);
                    SaveProfiles(new List<McpTransportProfile> { created });
                    _lastError = string.Empty;
                    return created;
                }

                List<McpTransportProfile> profiles;
                string error;
                if (!TryReadProfiles(out profiles, out error))
                {
                    _lastError = SanitizeError(error);
                    return CreateLegacyDefaultProfile(true);
                }

                foreach (var profile in profiles)
                {
                    if (profile.IsLegacyDefault)
                    {
                        _lastError = string.Empty;
                        return profile;
                    }
                }

                var legacy = CreateLegacyDefaultProfile(false);
                profiles.Add(legacy);
                SaveProfiles(profiles);
                _lastError = string.Empty;
                return legacy;
            }
        }

        internal static McpTransportProfile UpsertProfile(McpTransportProfile profile)
        {
            lock (Sync)
            {
                var normalized = NormalizeProfile(profile);
                List<McpTransportProfile> profiles;
                string error;
                if (!File.Exists(RegistryPath))
                {
                    profiles = new List<McpTransportProfile> { CreateLegacyDefaultProfile(false) };
                }
                else if (!TryReadProfiles(out profiles, out error))
                {
                    _lastError = SanitizeError(error);
                    throw new InvalidOperationException("Transport profile registry is malformed or unsupported and was left untouched.");
                }

                var replaced = false;
                for (var i = 0; i < profiles.Count; i++)
                {
                    if (!string.Equals(profiles[i].Id, normalized.Id, StringComparison.Ordinal)) continue;
                    profiles[i] = normalized;
                    replaced = true;
                    break;
                }
                if (!replaced) profiles.Add(normalized);

                ValidateProfileSet(profiles);
                SaveProfiles(profiles);
                _lastError = string.Empty;
                return normalized;
            }
        }

        internal static McpTransportProfile CreateProfile(
            McpTransportProvider provider,
            string displayName,
            bool enabled,
            bool autoStart,
            string registrationIdentity)
        {
            return NormalizeProfile(new McpTransportProfile(
                Guid.NewGuid().ToString("N"),
                provider,
                displayName,
                enabled,
                autoStart,
                false,
                registrationIdentity));
        }

        internal static bool RemoveProfile(string profileId)
        {
            lock (Sync)
            {
                var id = NormalizeProfileId(profileId);
                List<McpTransportProfile> profiles;
                string error;
                if (!File.Exists(RegistryPath)) return false;
                if (!TryReadProfiles(out profiles, out error))
                {
                    _lastError = SanitizeError(error);
                    throw new InvalidOperationException("Transport profile registry is malformed or unsupported and was left untouched.");
                }

                var index = profiles.FindIndex(p => string.Equals(p.Id, id, StringComparison.Ordinal));
                if (index < 0) return false;

                var target = profiles[index];
                var enabledCount = 0;
                foreach (var profile in profiles) if (profile.Enabled) enabledCount++;
                if (target.Enabled && enabledCount <= 1) return false;
                if (target.IsLegacyDefault && target.Enabled) return false;

                profiles.RemoveAt(index);
                ValidateProfileSet(profiles);
                SaveProfiles(profiles);
                RemoveRegistrationAcknowledgement(id);
                _lastError = string.Empty;
                return true;
            }
        }

        internal static void SetRegistrationAcknowledged(string profileId, string registrationIdentity)
        {
            lock (Sync)
            {
                var id = NormalizeProfileId(profileId);
                var identity = NormalizeRegistrationIdentity(registrationIdentity);
                var profile = FindProfileRequired(id);
                if (identity.Length == 0 || !string.Equals(profile.RegistrationIdentity, identity, StringComparison.Ordinal))
                    throw new InvalidOperationException("Registration identity does not match the selected transport profile.");

                Dictionary<string, string> acknowledgements;
                string error;
                if (!TryReadAcknowledgements(out acknowledgements, out error))
                {
                    _lastError = SanitizeError(error);
                    throw new InvalidOperationException("Transport registration state is malformed or unsupported and was left untouched.");
                }
                acknowledgements[id] = identity;
                SaveAcknowledgements(acknowledgements);
            }
        }

        internal static bool IsRegistrationAcknowledged(string profileId, string registrationIdentity)
        {
            lock (Sync)
            {
                string id;
                string identity;
                try
                {
                    id = NormalizeProfileId(profileId);
                    identity = NormalizeRegistrationIdentity(registrationIdentity);
                }
                catch { return false; }
                if (identity.Length == 0) return false;

                McpTransportProfile profile;
                try { profile = FindProfileRequired(id); }
                catch { return false; }
                if (!string.Equals(profile.RegistrationIdentity, identity, StringComparison.Ordinal)) return false;

                Dictionary<string, string> acknowledgements;
                string error;
                if (!TryReadAcknowledgements(out acknowledgements, out error))
                {
                    _lastError = SanitizeError(error);
                    return false;
                }
                string saved;
                return acknowledgements.TryGetValue(id, out saved)
                    && string.Equals(saved, identity, StringComparison.Ordinal);
            }
        }

        internal static string StatusJson()
        {
            lock (Sync)
            {
                var profiles = LoadProfiles();
                var builder = new StringBuilder();
                builder.Append("{\"schemaVersion\":").Append(SchemaVersion)
                    .Append(",\"profiles\":[");
                for (var i = 0; i < profiles.Count; i++)
                {
                    if (i != 0) builder.Append(',');
                    var profile = profiles[i];
                    builder.Append("{\"id\":\"").Append(JsonEscape(profile.Id))
                        .Append("\",\"provider\":\"").Append(JsonEscape(profile.Provider.ToString()))
                        .Append("\",\"displayName\":\"").Append(JsonEscape(profile.DisplayName))
                        .Append("\",\"enabled\":").Append(Bool(profile.Enabled))
                        .Append(",\"autoStart\":").Append(Bool(profile.AutoStart))
                        .Append(",\"legacyDefault\":").Append(Bool(profile.IsLegacyDefault))
                        .Append(",\"registrationIdentityPresent\":").Append(Bool(profile.RegistrationIdentity.Length != 0))
                        .Append('}');
                }
                builder.Append(']');
                if (_lastError.Length != 0)
                    builder.Append(",\"registryError\":\"").Append(JsonEscape(_lastError)).Append('"');
                builder.Append('}');
                return builder.ToString();
            }
        }

        private static McpTransportProfile FindProfileRequired(string id)
        {
            List<McpTransportProfile> profiles;
            string error;
            if (!File.Exists(RegistryPath))
                profiles = new List<McpTransportProfile> { EnsureLegacyDefaultProfile() };
            else if (!TryReadProfiles(out profiles, out error))
                throw new InvalidOperationException("Transport profile registry is malformed or unsupported.");

            foreach (var profile in profiles)
                if (string.Equals(profile.Id, id, StringComparison.Ordinal)) return profile;
            throw new InvalidOperationException("Transport profile was not found.");
        }

        private static McpTransportProfile CreateLegacyDefaultProfile(bool recovery)
        {
            McpTransportProvider provider;
            try { provider = McpTransportCoordinator.SelectedProvider; }
            catch { provider = McpTransportProvider.OpenAiSecureTunnel; }

            var identity = ResolveLegacyRegistrationIdentity(provider);
            var displayName = LegacyDefaultName + " · " + ProviderLabel(provider);
            return NormalizeProfile(new McpTransportProfile(
                recovery ? RecoveryProfileId : Guid.NewGuid().ToString("N"),
                provider,
                displayName,
                true,
                provider != McpTransportProvider.CloudflareQuickTunnel,
                true,
                identity));
        }

        private static string ResolveLegacyRegistrationIdentity(McpTransportProvider provider)
        {
            try
            {
                if (provider == McpTransportProvider.OpenAiSecureTunnel)
                {
                    var tunnelId = (McpOpenAiSecureTunnelManager.SavedTunnelId ?? string.Empty).Trim();
                    return McpOpenAiSecureTunnelManager.IsValidTunnelId(tunnelId) ? "openai:" + tunnelId : string.Empty;
                }

                var publicUrl = McpPublicEndpointResolver.Resolve();
                if (string.IsNullOrWhiteSpace(publicUrl)) return string.Empty;
                return "cloudflare:" + publicUrl.Trim().TrimEnd('/').ToLowerInvariant();
            }
            catch { return string.Empty; }
        }

        private static string ProviderLabel(McpTransportProvider provider)
        {
            switch (provider)
            {
                case McpTransportProvider.CloudflareNamedTunnel: return "Cloudflare Named Tunnel";
                case McpTransportProvider.CloudflareQuickTunnel: return "Cloudflare Quick Tunnel";
                default: return "OpenAI Secure MCP Tunnel";
            }
        }

        private static McpTransportProfile NormalizeProfile(McpTransportProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var id = NormalizeProfileId(profile.Id);
            if (!Enum.IsDefined(typeof(McpTransportProvider), profile.Provider))
                throw new ArgumentException("Unknown MCP transport provider.", nameof(profile));
            var displayName = NormalizeDisplayName(profile.DisplayName);
            var identity = NormalizeRegistrationIdentity(profile.RegistrationIdentity);
            return new McpTransportProfile(
                id,
                profile.Provider,
                displayName,
                profile.Enabled,
                profile.AutoStart,
                profile.IsLegacyDefault,
                identity);
        }

        private static string NormalizeProfileId(string value)
        {
            var id = (value ?? string.Empty).Trim();
            if (id.Length != 32) throw new ArgumentException("Transport profile id must be 32 lowercase hexadecimal characters.");
            for (var i = 0; i < id.Length; i++)
            {
                var c = id[i];
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                    throw new ArgumentException("Transport profile id must be 32 lowercase hexadecimal characters.");
            }
            return id;
        }

        private static string NormalizeDisplayName(string value)
        {
            var source = (value ?? string.Empty).Trim();
            var builder = new StringBuilder(Math.Min(source.Length, MaxDisplayNameLength));
            foreach (var c in source)
            {
                if (char.IsControl(c)) continue;
                if (builder.Length >= MaxDisplayNameLength) break;
                builder.Append(c);
            }
            var normalized = builder.ToString().Trim();
            if (normalized.Length == 0) throw new ArgumentException("Transport profile display name is required.");
            return normalized;
        }

        private static string NormalizeRegistrationIdentity(string value)
        {
            var identity = (value ?? string.Empty).Trim();
            if (identity.Length > MaxRegistrationIdentityLength)
                throw new ArgumentException("Transport registration identity is too long.");
            foreach (var c in identity)
                if (char.IsControl(c)) throw new ArgumentException("Transport registration identity contains control characters.");
            return identity;
        }

        private static void ValidateProfileSet(List<McpTransportProfile> profiles)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var legacyDefaults = 0;
            foreach (var raw in profiles)
            {
                var profile = NormalizeProfile(raw);
                if (!ids.Add(profile.Id)) throw new InvalidDataException("Duplicate transport profile id.");
                if (profile.IsLegacyDefault) legacyDefaults++;
            }
            if (legacyDefaults > 1) throw new InvalidDataException("Only one legacy-default transport profile is allowed.");
        }

        private static bool TryReadProfiles(out List<McpTransportProfile> profiles, out string error)
        {
            profiles = new List<McpTransportProfile>();
            error = string.Empty;
            try
            {
                var lines = File.ReadAllLines(RegistryPath, Encoding.UTF8);
                if (lines.Length == 0 || !string.Equals(lines[0], "schema|" + SchemaVersion, StringComparison.Ordinal))
                {
                    error = "unsupported-or-missing-schema";
                    return false;
                }

                for (var i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var parts = lines[i].Split(new[] { '|' }, StringSplitOptions.None);
                    if (parts.Length != 8 || !string.Equals(parts[0], "p", StringComparison.Ordinal))
                    {
                        error = "invalid-profile-record";
                        return false;
                    }
                    McpTransportProvider provider;
                    if (!Enum.TryParse(parts[2], false, out provider) || !Enum.IsDefined(typeof(McpTransportProvider), provider))
                    {
                        error = "invalid-provider";
                        return false;
                    }
                    bool enabled;
                    bool autoStart;
                    bool legacy;
                    if (!TryParseBool(parts[3], out enabled) || !TryParseBool(parts[4], out autoStart) || !TryParseBool(parts[5], out legacy))
                    {
                        error = "invalid-profile-flags";
                        return false;
                    }
                    profiles.Add(NormalizeProfile(new McpTransportProfile(
                        Unescape(parts[1]),
                        provider,
                        Unescape(parts[6]),
                        enabled,
                        autoStart,
                        legacy,
                        Unescape(parts[7]))));
                }
                ValidateProfileSet(profiles);
                return true;
            }
            catch
            {
                error = "registry-read-or-parse-failed";
                profiles.Clear();
                return false;
            }
        }

        private static void SaveProfiles(List<McpTransportProfile> profiles)
        {
            ValidateProfileSet(profiles);
            var copy = new List<McpTransportProfile>(profiles);
            copy.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            var lines = new List<string> { "schema|" + SchemaVersion };
            foreach (var profile in copy)
            {
                lines.Add("p|" + Escape(profile.Id)
                    + "|" + profile.Provider
                    + "|" + Flag(profile.Enabled)
                    + "|" + Flag(profile.AutoStart)
                    + "|" + Flag(profile.IsLegacyDefault)
                    + "|" + Escape(profile.DisplayName)
                    + "|" + Escape(profile.RegistrationIdentity));
            }
            AtomicWrite(RegistryPath, lines);
        }

        private static bool TryReadAcknowledgements(out Dictionary<string, string> acknowledgements, out string error)
        {
            acknowledgements = new Dictionary<string, string>(StringComparer.Ordinal);
            error = string.Empty;
            if (!File.Exists(RegistrationPath)) return true;
            try
            {
                var lines = File.ReadAllLines(RegistrationPath, Encoding.UTF8);
                if (lines.Length == 0 || !string.Equals(lines[0], "schema|" + SchemaVersion, StringComparison.Ordinal))
                {
                    error = "unsupported-registration-schema";
                    return false;
                }
                for (var i = 1; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i])) continue;
                    var parts = lines[i].Split(new[] { '|' }, StringSplitOptions.None);
                    if (parts.Length != 3 || !string.Equals(parts[0], "a", StringComparison.Ordinal))
                    {
                        error = "invalid-registration-record";
                        return false;
                    }
                    var id = NormalizeProfileId(Unescape(parts[1]));
                    var identity = NormalizeRegistrationIdentity(Unescape(parts[2]));
                    if (identity.Length == 0 || acknowledgements.ContainsKey(id))
                    {
                        error = "invalid-registration-entry";
                        return false;
                    }
                    acknowledgements.Add(id, identity);
                }
                return true;
            }
            catch
            {
                error = "registration-read-or-parse-failed";
                acknowledgements.Clear();
                return false;
            }
        }

        private static void SaveAcknowledgements(Dictionary<string, string> acknowledgements)
        {
            var ids = new List<string>(acknowledgements.Keys);
            ids.Sort(StringComparer.Ordinal);
            var lines = new List<string> { "schema|" + SchemaVersion };
            foreach (var id in ids)
                lines.Add("a|" + Escape(NormalizeProfileId(id)) + "|" + Escape(NormalizeRegistrationIdentity(acknowledgements[id])));
            AtomicWrite(RegistrationPath, lines);
        }

        private static void RemoveRegistrationAcknowledgement(string profileId)
        {
            Dictionary<string, string> acknowledgements;
            string error;
            if (!TryReadAcknowledgements(out acknowledgements, out error)) return;
            if (!acknowledgements.Remove(profileId)) return;
            SaveAcknowledgements(acknowledgements);
        }

        private static void AtomicWrite(string path, List<string> lines)
        {
            Directory.CreateDirectory(SettingsDirectory);
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    foreach (var line in lines) writer.WriteLine(line);
                    writer.Flush();
                    stream.Flush(true);
                }
                if (File.Exists(path)) File.Replace(temp, path, null);
                else File.Move(temp, path);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { }
            }
        }

        private static string Escape(string value)
        {
            return Uri.EscapeDataString(value ?? string.Empty);
        }

        private static string Unescape(string value)
        {
            return Uri.UnescapeDataString(value ?? string.Empty);
        }

        private static bool TryParseBool(string value, out bool result)
        {
            if (string.Equals(value, "1", StringComparison.Ordinal)) { result = true; return true; }
            if (string.Equals(value, "0", StringComparison.Ordinal)) { result = false; return true; }
            result = false;
            return false;
        }

        private static string Flag(bool value) => value ? "1" : "0";
        private static string Bool(bool value) => value ? "true" : "false";

        private static string SanitizeError(string value)
        {
            var source = value ?? string.Empty;
            var builder = new StringBuilder(Math.Min(source.Length, 160));
            foreach (var c in source)
            {
                if (char.IsControl(c) || c == '/' || c == '\\' || c == ':') continue;
                if (builder.Length >= 160) break;
                builder.Append(c);
            }
            return builder.ToString();
        }

        private static string JsonEscape(string value)
        {
            return (value ?? string.Empty)
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }
    }
}
