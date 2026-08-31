using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using QS3D.Core.Mep;

namespace QS3D.BricsCAD.V25
{
    internal static class MepRecognitionProfileProvider
    {
        private static readonly object Gate = new object();
        private static MepRecognitionProfile _current = MepRecognitionProfiles.CreateDefault();
        private static bool _isCustom;
        private static string? _lastError;

        static MepRecognitionProfileProvider()
        {
            Reload();
        }

        internal static MepRecognitionProfile Current
        {
            get
            {
                lock (Gate) return _current;
            }
        }

        internal static string ProfilePath => MepRecognitionProfileStore.ProfilePath;

        internal static bool IsCustom
        {
            get
            {
                lock (Gate) return _isCustom;
            }
        }

        internal static string? LastError
        {
            get
            {
                lock (Gate) return _lastError;
            }
        }

        internal static bool Reload()
        {
            lock (Gate)
            {
                if (!MepRecognitionProfileStore.TryLoad(out var profile, out var exists, out var error))
                {
                    _current = MepRecognitionProfiles.CreateDefault();
                    _isCustom = false;
                    _lastError = error;
                    return false;
                }

                _current = profile;
                _isCustom = exists;
                _lastError = null;
                return true;
            }
        }

        internal static void Save(MepRecognitionProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            MepRecognitionProfileStore.SaveAtomic(profile);
            lock (Gate)
            {
                _current = profile;
                _isCustom = true;
                _lastError = null;
            }
        }

        internal static void SaveDefault()
        {
            Save(MepRecognitionProfiles.CreateDefault());
        }
    }

    internal static class MepRecognitionProfileStore
    {
        private const int MaxProfileBytes = 512 * 1024;
        private const int MaxRules = MepRecognitionLimits.MaxRules;
        private const int MaxTokensPerRule = MepRecognitionLimits.MaxTokensPerRule;
        private const string RootName = "qs3dMepRecognitionProfile";
        private const string Version = "1";
#if BRICSCAD_V26
        private const string HostVersion = "V26";
#else
        private const string HostVersion = "V25";
#endif

        internal static string ProfilePath
        {
            get
            {
                var root = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (string.IsNullOrWhiteSpace(root))
                    throw new InvalidOperationException("Windows application-data directory is unavailable.");
                return Path.Combine(root, "QS3D", "BricsCAD", HostVersion, "mep-recognition-profile.xml");
            }
        }

        internal static bool TryLoad(out MepRecognitionProfile profile, out bool exists, out string? error)
        {
            profile = MepRecognitionProfiles.CreateDefault();
            exists = false;
            error = null;

            string path;
            try { path = ProfilePath; }
            catch (Exception ex)
            {
                error = "Không resolve được profile path: " + ex.Message;
                return false;
            }

            if (!File.Exists(path)) return true;
            exists = true;

            try
            {
                var info = new FileInfo(path);
                if (info.Length <= 0 || info.Length > MaxProfileBytes)
                    throw new InvalidDataException("Profile file phải lớn hơn 0 và không vượt " + MaxProfileBytes + " bytes.");

                var settings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersInDocument = MaxProfileBytes,
                    MaxCharactersFromEntities = 0
                };
                var document = new XmlDocument { XmlResolver = null };
                using (var reader = XmlReader.Create(path, settings)) document.Load(reader);

                var root = document.DocumentElement;
                if (root == null || !StringComparer.Ordinal.Equals(root.Name, RootName))
                    throw new InvalidDataException("Profile root element không hợp lệ.");
                if (!StringComparer.Ordinal.Equals(root.GetAttribute("version"), Version))
                    throw new InvalidDataException("Profile version không được hỗ trợ.");

                var rules = new List<MepRecognitionRule>();
                foreach (XmlNode node in root.ChildNodes)
                {
                    if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace) continue;
                    var element = node as XmlElement;
                    if (element == null || !StringComparer.Ordinal.Equals(element.Name, "rule"))
                        throw new InvalidDataException("Profile chỉ được chứa các phần tử <rule>.");
                    if (rules.Count >= MaxRules) throw new InvalidDataException("Profile vượt giới hạn " + MaxRules + " rules.");
                    rules.Add(ParseRule(element));
                }

                if (rules.Count == 0) throw new InvalidDataException("Profile phải có ít nhất một recognition rule.");
                profile = new MepRecognitionProfile(rules);
                return true;
            }
            catch (Exception ex) when (IsRecoverableProfileFailure(ex))
            {
                error = "Profile không hợp lệ; đang fail-closed về default: " + ex.Message;
                profile = MepRecognitionProfiles.CreateDefault();
                return false;
            }
        }

        internal static void SaveAtomic(MepRecognitionProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (profile.Rules.Count <= 0 || profile.Rules.Count > MaxRules)
                throw new InvalidOperationException("Profile rule count phải trong 1.." + MaxRules + ".");

            var path = ProfilePath;
            var directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("Profile directory không hợp lệ.");
            Directory.CreateDirectory(directory);

            var tempPath = path + ".tmp-" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            var backupPath = path + ".bak";
            try
            {
                WriteXml(profile, tempPath);
                var info = new FileInfo(tempPath);
                if (info.Length <= 0 || info.Length > MaxProfileBytes)
                    throw new InvalidDataException("Serialized profile vượt giới hạn kích thước an toàn.");

                if (File.Exists(path))
                {
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                    File.Replace(tempPath, path, backupPath, true);
                    if (File.Exists(backupPath)) File.Delete(backupPath);
                }
                else
                {
                    File.Move(tempPath, path);
                }
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); }
                catch (Exception ex) when (IsRecoverableProfileFailure(ex)) { }
            }
        }

        private static MepRecognitionRule ParseRule(XmlElement element)
        {
            var id = RequiredAttribute(element, "id");
            if (!int.TryParse(RequiredAttribute(element, "priority"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var priority))
                throw new InvalidDataException("Rule " + id + " có priority không hợp lệ.");
            if (!Enum.TryParse(RequiredAttribute(element, "discipline"), true, out MepRecognitionDiscipline discipline) ||
                !Enum.IsDefined(typeof(MepRecognitionDiscipline), discipline))
                throw new InvalidDataException("Rule " + id + " có discipline không hợp lệ.");
            var category = RequiredAttribute(element, "category");
            if (!Enum.TryParse(RequiredAttribute(element, "source"), true, out MepRecognitionSource source) ||
                source == MepRecognitionSource.None ||
                (source & ~MepRecognitionSource.LayerOrBlockName) != MepRecognitionSource.None)
                throw new InvalidDataException("Rule " + id + " có source không hợp lệ.");

            MepElementKind? mepKind = null;
            var kindText = element.GetAttribute("mepKind");
            if (!string.IsNullOrWhiteSpace(kindText))
            {
                if (!Enum.TryParse(kindText, true, out MepElementKind parsedKind) || !Enum.IsDefined(typeof(MepElementKind), parsedKind))
                    throw new InvalidDataException("Rule " + id + " có MEP kind không hợp lệ.");
                mepKind = parsedKind;
            }

            var tokens = new List<string>();
            foreach (XmlNode node in element.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment || node.NodeType == XmlNodeType.Whitespace) continue;
                var tokenElement = node as XmlElement;
                if (tokenElement == null || !StringComparer.Ordinal.Equals(tokenElement.Name, "token"))
                    throw new InvalidDataException("Rule " + id + " chỉ được chứa <token>.");
                if (tokens.Count >= MaxTokensPerRule)
                    throw new InvalidDataException("Rule " + id + " vượt " + MaxTokensPerRule + " tokens.");
                tokens.Add(RequiredAttribute(tokenElement, "value"));
            }
            if (tokens.Count == 0) throw new InvalidDataException("Rule " + id + " không có token.");
            return new MepRecognitionRule(id, priority, discipline, category, tokens, source, mepKind);
        }

        private static void WriteXml(MepRecognitionProfile profile, string path)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                NewLineHandling = NewLineHandling.Entitize,
                CloseOutput = true
            };
            using (var writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement(RootName);
                writer.WriteAttributeString("version", Version);
                for (var i = 0; i < profile.Rules.Count; i++)
                {
                    var rule = profile.Rules[i];
                    if (rule.Tokens.Count <= 0 || rule.Tokens.Count > MaxTokensPerRule)
                        throw new InvalidOperationException("Rule " + rule.Id + " token count không hợp lệ.");
                    writer.WriteStartElement("rule");
                    writer.WriteAttributeString("id", rule.Id);
                    writer.WriteAttributeString("priority", rule.Priority.ToString(CultureInfo.InvariantCulture));
                    writer.WriteAttributeString("discipline", rule.Discipline.ToString());
                    writer.WriteAttributeString("category", rule.Category);
                    writer.WriteAttributeString("source", rule.Source.ToString());
                    if (rule.MepKind.HasValue) writer.WriteAttributeString("mepKind", rule.MepKind.Value.ToString());
                    for (var tokenIndex = 0; tokenIndex < rule.Tokens.Count; tokenIndex++)
                    {
                        writer.WriteStartElement("token");
                        writer.WriteAttributeString("value", rule.Tokens[tokenIndex]);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private static string RequiredAttribute(XmlElement element, string name)
        {
            var value = element.GetAttribute(name);
            if (string.IsNullOrWhiteSpace(value)) throw new InvalidDataException("Thiếu attribute " + name + ".");
            return value.Trim();
        }

        private static bool IsRecoverableProfileFailure(Exception exception) =>
            !(exception is OutOfMemoryException) &&
            !(exception is StackOverflowException) &&
            !(exception is AccessViolationException);
    }
}
