namespace CompactSerializer;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SyncOrderAttribute : Attribute
{
    public SyncOrderAttribute(int order) => Order = order;
    public int Order { get; }
}
