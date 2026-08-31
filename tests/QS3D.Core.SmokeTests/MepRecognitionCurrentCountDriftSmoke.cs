using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using QS3D.Core.Mep;

namespace QS3D.Core.SmokeTests
{
    internal static class MepRecognitionCurrentCountDriftSmoke
    {
        [ModuleInitializer]
        internal static void Initialize() => Run();

        internal static void Run()
        {
            TokenCurrentCountDriftWinsBeforeMalformedTokenValidation();
            RuleCurrentCountDriftWinsBeforeNullRuleValidation();
        }

        private static void TokenCurrentCountDriftWinsBeforeMalformedTokenValidation()
        {
            var source = new CurrentCountDriftProbe<string>(" ");
            var error = Capture<ArgumentException>(() => new MepRecognitionRule(
                "current-count-token",
                1,
                MepRecognitionDiscipline.Structure,
                "Structure",
                source,
                MepRecognitionSource.LayerOrBlockName));

            Contains("known count changed during traversal", error.Message,
                "Token Current-induced Count drift must win before malformed-token validation.");
            Equal(1, source.CurrentReads, "Token Current must be read exactly once before Count rebound fails.");
        }

        private static void RuleCurrentCountDriftWinsBeforeNullRuleValidation()
        {
            var source = new CurrentCountDriftProbe<MepRecognitionRule>(null!);
            var error = Capture<ArgumentException>(() => new MepRecognitionProfile(source));

            Contains("known count changed during traversal", error.Message,
                "Rule Current-induced Count drift must win before null-rule validation.");
            Equal(1, source.CurrentReads, "Rule Current must be read exactly once before Count rebound fails.");
        }

        private static TException Capture<TException>(Action action)
            where TException : Exception
        {
            try
            {
                action();
            }
            catch (TException error)
            {
                return error;
            }

            throw new InvalidOperationException("Expected " + typeof(TException).Name + ".");
        }

        private static void Contains(string expected, string actual, string message)
        {
            if (actual == null || actual.IndexOf(expected, StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidOperationException(message + " Actual=" + actual + ".");
        }

        private static void Equal<T>(T expected, T actual, string message)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
                throw new InvalidOperationException(message + " Expected=" + expected + ", actual=" + actual + ".");
        }

        private sealed class CurrentCountDriftProbe<T> : ICollection<T>
        {
            private readonly T _value;
            private int _count = 1;

            internal CurrentCountDriftProbe(T value) => _value = value;

            public int Count => _count;
            public bool IsReadOnly => true;
            internal int CurrentReads { get; private set; }

            public IEnumerator<T> GetEnumerator() => new ProbeEnumerator(this);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public bool Contains(T item) => EqualityComparer<T>.Default.Equals(_value, item);
            public void CopyTo(T[] array, int arrayIndex) => array[arrayIndex] = _value;
            public void Add(T item) => throw new NotSupportedException();
            public void Clear() => throw new NotSupportedException();
            public bool Remove(T item) => throw new NotSupportedException();

            private sealed class ProbeEnumerator : IEnumerator<T>
            {
                private readonly CurrentCountDriftProbe<T> _owner;
                private bool _moved;

                internal ProbeEnumerator(CurrentCountDriftProbe<T> owner) => _owner = owner;

                public T Current
                {
                    get
                    {
                        _owner.CurrentReads++;
                        _owner._count = 2;
                        return _owner._value;
                    }
                }

                object IEnumerator.Current => Current!;

                public bool MoveNext()
                {
                    if (_moved) return false;
                    _moved = true;
                    return true;
                }

                public void Reset() => throw new NotSupportedException();
                public void Dispose() { }
            }
        }
    }
}
