namespace CompactBinarySerializer;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CbIndexAttribute : Attribute
{
    public CbIndexAttribute(int index) => Index = index;
    public int Index { get; }
}
