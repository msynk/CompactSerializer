namespace CompactBinarySerializer;

[AttributeUsage(AttributeTargets.Property)]
public sealed class CompactIndexAttribute : Attribute
{
    public CompactIndexAttribute(int index) => Index = index;
    public int Index { get; }
}
