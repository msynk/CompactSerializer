using System.Diagnostics;
using System.Text;
using System.Text.Json;
using CompactSerializer;

namespace CompactSerializer.Tests;

public sealed class SerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTrips_ComplexObject()
    {
        var sample = CreateSampleEnvelope();

        var bytes = CompactBinarySerializer.Serialize(sample);
        var restored = CompactBinarySerializer.Deserialize<TestEnvelope>(bytes);

        AssertEqual(sample, restored);
    }

    [Fact]
    public void Serialize_SameObjectTwice_ProducesDeterministicBytes()
    {
        var sample = CreateSampleEnvelope();

        var first = CompactBinarySerializer.Serialize(sample);
        var second = CompactBinarySerializer.Serialize(sample);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Serialize_NullRoot_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => CompactBinarySerializer.Serialize<string>(null!));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Deserialize_EmptyPayload_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => CompactBinarySerializer.Deserialize<TestEnvelope>([]));
        Assert.Equal("payload", exception.ParamName);
    }

    [Fact]
    public void Deserialize_NullForNonNullableRoot_ThrowsInvalidOperationException()
    {
        var payloadForNullString = new byte[] { 0 };

        var exception = Assert.Throws<InvalidOperationException>(() => CompactBinarySerializer.Deserialize<string>(payloadForNullString));

        Assert.Equal("Deserialization produced null for a non-nullable root type.", exception.Message);
    }

    [Fact]
    public void SerializeAndDeserialize_NullableProperty_PreservesNull()
    {
        var sample = CreateSampleEnvelope();
        sample.OptionalCount = null;
        sample.Child = null;

        var bytes = CompactBinarySerializer.Serialize(sample);
        var restored = CompactBinarySerializer.Deserialize<TestEnvelope>(bytes);

        Assert.Null(restored.OptionalCount);
        Assert.Null(restored.Child);
    }

    [Fact]
    public void SerializeAndDeserialize_NullableValueType_PreservesValue()
    {
        var sample = CreateSampleEnvelope();
        sample.OptionalCount = 999_999;

        var bytes = CompactBinarySerializer.Serialize(sample);
        var restored = CompactBinarySerializer.Deserialize<TestEnvelope>(bytes);

        Assert.Equal(999_999, restored.OptionalCount);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTrips_PrimitiveBoundaries()
    {
        var sample = new PrimitiveEnvelope
        {
            BoolValue = true,
            ByteValue = byte.MaxValue,
            ShortValue = short.MinValue,
            IntValue = int.MaxValue,
            LongValue = long.MinValue,
            UShortValue = ushort.MaxValue,
            UIntValue = uint.MaxValue,
            ULongValue = ulong.MaxValue,
            FloatValue = 12345.5f,
            DoubleValue = -9876543.125,
            DecimalValue = 79228162514264337593543950335m,
            DateTimeValue = new DateTime(2024, 10, 15, 21, 30, 40, DateTimeKind.Utc),
            GuidValue = Guid.NewGuid(),
            EnumValue = TestPriority.High
        };

        var bytes = CompactBinarySerializer.Serialize(sample);
        var restored = CompactBinarySerializer.Deserialize<PrimitiveEnvelope>(bytes);

        Assert.Equal(sample.BoolValue, restored.BoolValue);
        Assert.Equal(sample.ByteValue, restored.ByteValue);
        Assert.Equal(sample.ShortValue, restored.ShortValue);
        Assert.Equal(sample.IntValue, restored.IntValue);
        Assert.Equal(sample.LongValue, restored.LongValue);
        Assert.Equal(sample.UShortValue, restored.UShortValue);
        Assert.Equal(sample.UIntValue, restored.UIntValue);
        Assert.Equal(sample.ULongValue, restored.ULongValue);
        Assert.Equal(sample.FloatValue, restored.FloatValue);
        Assert.Equal(sample.DoubleValue, restored.DoubleValue);
        Assert.Equal(sample.DecimalValue, restored.DecimalValue);
        Assert.Equal(sample.DateTimeValue, restored.DateTimeValue);
        Assert.Equal(sample.GuidValue, restored.GuidValue);
        Assert.Equal(sample.EnumValue, restored.EnumValue);
    }

    [Fact]
    public void SerializeAndDeserialize_RoundTrips_CollectionsOfObjects()
    {
        var sample = new CollectionEnvelope
        {
            Children = [new TestChild { Label = "a", IsActive = true }, new TestChild { Label = "b", IsActive = false }],
            ChildArray = [new TestChild { Label = "x", IsActive = true }, new TestChild { Label = "y", IsActive = true }]
        };

        var bytes = CompactBinarySerializer.Serialize(sample);
        var restored = CompactBinarySerializer.Deserialize<CollectionEnvelope>(bytes);

        Assert.Equal(sample.Children.Count, restored.Children.Count);
        Assert.Equal(sample.ChildArray.Length, restored.ChildArray.Length);
        Assert.Equal(sample.Children[0].Label, restored.Children[0].Label);
        Assert.Equal(sample.Children[1].IsActive, restored.Children[1].IsActive);
        Assert.Equal(sample.ChildArray[1].Label, restored.ChildArray[1].Label);
    }

    [Fact]
    public void Serialize_UsesSyncOrder_IrrespectiveOfDeclarationOrder()
    {
        var first = new OrderedShapeA { Name = "alpha", Count = 33 };
        var second = new OrderedShapeB { Name = "alpha", Count = 33 };

        var bytesA = CompactBinarySerializer.Serialize(first);
        var bytesB = CompactBinarySerializer.Serialize(second);

        Assert.Equal(bytesA, bytesB);
    }

    [Fact]
    public void Serialize_TypeWithoutParameterlessConstructor_ThrowsInvalidOperationException()
    {
        var sample = new NoParameterlessCtorEnvelope { Name = "fail" };

        var exception = Assert.Throws<InvalidOperationException>(() => CompactBinarySerializer.Serialize(sample));

        Assert.Contains("A public parameterless constructor is required.", exception.Message);
    }

    [Fact]
    public void Serialize_UnsupportedInterfacePropertyType_ThrowsInvalidOperationException()
    {
        var sample = new UnsupportedCollectionEnvelope { Values = new List<int> { 1, 2, 3 } };

        var exception = Assert.Throws<InvalidOperationException>(() => CompactBinarySerializer.Serialize(sample));

        Assert.Contains("A public parameterless constructor is required.", exception.Message);
    }

    [Fact]
    public void Deserialize_TruncatedPayload_ThrowsInvalidOperationException()
    {
        var sample = CreateSampleEnvelope();
        var full = CompactBinarySerializer.Serialize(sample);
        var truncated = full[..^1];

        var exception = Assert.Throws<InvalidOperationException>(() => CompactBinarySerializer.Deserialize<TestEnvelope>(truncated));

        Assert.Equal("Unexpected end of payload.", exception.Message);
    }

    [Fact]
    public void CompactPayload_IsSmallerThanJson_ForRepresentativeModel()
    {
        var sample = CreateLargeRepresentativeEnvelope();
        var json = JsonSerializer.Serialize(sample);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var compactBytes = CompactBinarySerializer.Serialize(sample);

        Assert.True(compactBytes.Length < jsonBytes.Length, $"Expected compact payload to be smaller than JSON. Compact={compactBytes.Length}, JSON={jsonBytes.Length}");
    }

    [Fact]
    public void Serialize_PerformanceSmoke_CompletesWithinReasonableTime()
    {
        var sample = CreateLargeRepresentativeEnvelope();
        const int iterations = 5_000;

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = CompactBinarySerializer.Serialize(sample);
        }
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"Serialize performance smoke test exceeded threshold: {sw.Elapsed.TotalMilliseconds:F2} ms");
    }

    [Fact]
    public void Deserialize_PerformanceSmoke_CompletesWithinReasonableTime()
    {
        var sample = CreateLargeRepresentativeEnvelope();
        var bytes = CompactBinarySerializer.Serialize(sample);
        const int iterations = 5_000;

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = CompactBinarySerializer.Deserialize<TestEnvelope>(bytes);
        }
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"Deserialize performance smoke test exceeded threshold: {sw.Elapsed.TotalMilliseconds:F2} ms");
    }

    private static TestEnvelope CreateSampleEnvelope()
    {
        return new TestEnvelope
        {
            Id = 42,
            Name = "device-alpha",
            Priority = TestPriority.High,
            CreatedAtUtc = new DateTime(2026, 4, 20, 7, 30, 0, DateTimeKind.Utc),
            CorrelationId = Guid.Parse("65bfaa3f-2792-4f66-a7e7-1cdf834a7a34"),
            OptionalCount = 17,
            Tags = ["one", "two", "three"],
            Readings = [12, -5, 77],
            Payload = [1, 2, 3, 4, 5],
            Child = new TestChild
            {
                Label = "nested",
                IsActive = true
            }
        };
    }

    private static TestEnvelope CreateLargeRepresentativeEnvelope()
    {
        var envelope = CreateSampleEnvelope();
        envelope.Tags = Enumerable.Range(1, 100).Select(i => $"tag-{i:D3}").ToList();
        envelope.Readings = Enumerable.Range(0, 200).Select(i => i * 3 - 17).ToArray();
        envelope.Payload = Enumerable.Range(0, 512).Select(i => (byte)((i * 19 + 7) % 256)).ToArray();
        return envelope;
    }

    private static void AssertEqual(TestEnvelope expected, TestEnvelope actual)
    {
        Assert.Equal(expected.Id, actual.Id);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Priority, actual.Priority);
        Assert.Equal(expected.CreatedAtUtc, actual.CreatedAtUtc);
        Assert.Equal(expected.CorrelationId, actual.CorrelationId);
        Assert.Equal(expected.OptionalCount, actual.OptionalCount);
        Assert.Equal(expected.Tags, actual.Tags);
        Assert.Equal(expected.Readings, actual.Readings);
        Assert.Equal(expected.Payload, actual.Payload);

        if (expected.Child is null)
        {
            Assert.Null(actual.Child);
            return;
        }

        Assert.NotNull(actual.Child);
        Assert.Equal(expected.Child.Label, actual.Child!.Label);
        Assert.Equal(expected.Child.IsActive, actual.Child.IsActive);
    }

    private enum TestPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    private sealed class TestEnvelope
    {
        [SyncOrder(0)]
        public int Id { get; set; }

        [SyncOrder(1)]
        public string Name { get; set; } = string.Empty;

        [SyncOrder(2)]
        public TestPriority Priority { get; set; }

        [SyncOrder(3)]
        public DateTime CreatedAtUtc { get; set; }

        [SyncOrder(4)]
        public Guid CorrelationId { get; set; }

        [SyncOrder(5)]
        public int? OptionalCount { get; set; }

        [SyncOrder(6)]
        public List<string> Tags { get; set; } = [];

        [SyncOrder(7)]
        public int[] Readings { get; set; } = [];

        [SyncOrder(8)]
        public byte[] Payload { get; set; } = [];

        [SyncOrder(9)]
        public TestChild? Child { get; set; }
    }

    private sealed class TestChild
    {
        [SyncOrder(0)]
        public string Label { get; set; } = string.Empty;

        [SyncOrder(1)]
        public bool IsActive { get; set; }
    }

    private sealed class PrimitiveEnvelope
    {
        [SyncOrder(0)] public bool BoolValue { get; set; }
        [SyncOrder(1)] public byte ByteValue { get; set; }
        [SyncOrder(2)] public short ShortValue { get; set; }
        [SyncOrder(3)] public int IntValue { get; set; }
        [SyncOrder(4)] public long LongValue { get; set; }
        [SyncOrder(5)] public ushort UShortValue { get; set; }
        [SyncOrder(6)] public uint UIntValue { get; set; }
        [SyncOrder(7)] public ulong ULongValue { get; set; }
        [SyncOrder(8)] public float FloatValue { get; set; }
        [SyncOrder(9)] public double DoubleValue { get; set; }
        [SyncOrder(10)] public decimal DecimalValue { get; set; }
        [SyncOrder(11)] public DateTime DateTimeValue { get; set; }
        [SyncOrder(12)] public Guid GuidValue { get; set; }
        [SyncOrder(13)] public TestPriority EnumValue { get; set; }
    }

    private sealed class CollectionEnvelope
    {
        [SyncOrder(0)]
        public List<TestChild> Children { get; set; } = [];

        [SyncOrder(1)]
        public TestChild[] ChildArray { get; set; } = [];
    }

    private sealed class OrderedShapeA
    {
        [SyncOrder(1)]
        public string Name { get; set; } = string.Empty;

        [SyncOrder(0)]
        public int Count { get; set; }
    }

    private sealed class OrderedShapeB
    {
        [SyncOrder(0)]
        public int Count { get; set; }

        [SyncOrder(1)]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NoParameterlessCtorEnvelope
    {
        [SyncOrder(0)]
        public NoParameterlessCtorChild Child { get; set; } = new("x");

        [SyncOrder(1)]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NoParameterlessCtorChild(string value)
    {
        [SyncOrder(0)]
        public string Value { get; set; } = value;
    }

    private sealed class UnsupportedCollectionEnvelope
    {
        [SyncOrder(0)]
        public IEnumerable<int> Values { get; set; } = [];
    }
}
