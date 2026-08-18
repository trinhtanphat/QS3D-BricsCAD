using System;
using System.Collections;
using System.Collections.Generic;
using QS3D.Core.Units;

namespace QS3D.Core.SmokeTests
{
    internal static class DrawingUnitWriteAtomicitySmoke
    {
        public static void Run()
        {
            SetProjectOverrideRollsBackAfterPartialWrite();
            BindQuantityUnitRollsBackAfterPartialWrite();
        }

        private static void SetProjectOverrideRollsBackAfterPartialWrite()
        {
            var seed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [DrawingUnitResolutionPolicy.BoundMetadataKey] = LengthUnit.Meter.ToString(),
                [DrawingUnitResolutionPolicy.OverrideMetadataKey] = LengthUnit.Meter.ToString(),
                [DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey] = "legacy-effective",
                [DrawingUnitResolutionPolicy.BindingSourceMetadataKey] = "legacy-source"
            };
            var metadata = new FailOnceOnSetterDictionary(seed, failOnSetterCall: 2);

            Throws<InvalidOperationException>(() =>
                DrawingUnitResolutionPolicy.SetProjectOverride(metadata, LengthUnit.Meter));

            AssertValue(metadata, DrawingUnitResolutionPolicy.BoundMetadataKey, LengthUnit.Meter.ToString());
            AssertValue(metadata, DrawingUnitResolutionPolicy.OverrideMetadataKey, LengthUnit.Meter.ToString());
            AssertValue(metadata, DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey, "legacy-effective");
            AssertValue(metadata, DrawingUnitResolutionPolicy.BindingSourceMetadataKey, "legacy-source");
        }

        private static void BindQuantityUnitRollsBackAfterPartialWrite()
        {
            var seed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Unrelated"] = "preserve"
            };
            var metadata = new FailOnceOnSetterDictionary(seed, failOnSetterCall: 2);

            Throws<InvalidOperationException>(() =>
                DrawingUnitResolutionPolicy.BindQuantityUnit(
                    metadata,
                    false,
                    LengthUnit.Millimeter,
                    DrawingUnitResolutionSource.NativeInsunits));

            if (metadata.ContainsKey(DrawingUnitResolutionPolicy.BoundMetadataKey) ||
                metadata.ContainsKey(DrawingUnitResolutionPolicy.EffectiveUnitMetadataKey) ||
                metadata.ContainsKey(DrawingUnitResolutionPolicy.BindingSourceMetadataKey))
                throw new Exception("Rejected drawing-unit binding left partial metadata evidence.");
            AssertValue(metadata, "Unrelated", "preserve");
        }

        private static void AssertValue(IDictionary<string, string> metadata, string key, string expected)
        {
            if (!metadata.TryGetValue(key, out var actual) ||
                !string.Equals(actual, expected, StringComparison.Ordinal))
                throw new Exception("Drawing-unit metadata rollback did not restore " + key + ".");
        }

        private static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            throw new Exception("Expected " + typeof(T).Name + ".");
        }

        private sealed class FailOnceOnSetterDictionary : IDictionary<string, string>
        {
            private readonly Dictionary<string, string> _inner;
            private readonly int _failOnSetterCall;
            private int _setterCalls;
            private bool _failed;

            public FailOnceOnSetterDictionary(
                IDictionary<string, string> seed,
                int failOnSetterCall)
            {
                _inner = new Dictionary<string, string>(seed, StringComparer.OrdinalIgnoreCase);
                _failOnSetterCall = failOnSetterCall;
            }

            public string this[string key]
            {
                get => _inner[key];
                set
                {
                    _setterCalls++;
                    if (!_failed && _setterCalls == _failOnSetterCall)
                    {
                        _failed = true;
                        throw new InvalidOperationException("Injected drawing-unit metadata setter failure.");
                    }
                    _inner[key] = value;
                }
            }

            public ICollection<string> Keys => _inner.Keys;
            public ICollection<string> Values => _inner.Values;
            public int Count => _inner.Count;
            public bool IsReadOnly => false;

            public void Add(string key, string value) => _inner.Add(key, value);
            public void Add(KeyValuePair<string, string> item) =>
                ((ICollection<KeyValuePair<string, string>>)_inner).Add(item);
            public void Clear() => _inner.Clear();
            public bool Contains(KeyValuePair<string, string> item) =>
                ((ICollection<KeyValuePair<string, string>>)_inner).Contains(item);
            public bool ContainsKey(string key) => _inner.ContainsKey(key);
            public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex) =>
                ((ICollection<KeyValuePair<string, string>>)_inner).CopyTo(array, arrayIndex);
            public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _inner.GetEnumerator();
            public bool Remove(string key) => _inner.Remove(key);
            public bool Remove(KeyValuePair<string, string> item) =>
                ((ICollection<KeyValuePair<string, string>>)_inner).Remove(item);
            public bool TryGetValue(string key, out string value) => _inner.TryGetValue(key, out value!);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
