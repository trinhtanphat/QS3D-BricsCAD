namespace QS3D.Core.Takeoff
{
    public sealed class TakeoffResult
    {
        public TakeoffResult(string handle, TakeoffKind kind, double value, string unit) { Handle = handle; Kind = kind; Value = value; Unit = unit; }
        public string Handle { get; }
        public TakeoffKind Kind { get; }
        public double Value { get; }
        public string Unit { get; }
    }
}
