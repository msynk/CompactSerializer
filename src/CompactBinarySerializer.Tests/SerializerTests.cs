using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static CompactBinarySerializer.CbSerializer;

namespace CompactBinarySerializer.Tests;

public sealed class SerializerTests
{
    [Fact]
    public void SerializeAndDeserialize_RoundTrips_ComplexObject()
    {
        var sample = CreateSampleEnvelope();

        var bytes = Serialize(sample);
        var restored = Deserialize<TestEnvelope>(bytes);

        AssertEqual(sample, restored);
    }

    [Fact]
    public void Serialize_SameObjectTwice_ProducesDeterministicBytes()
    {
        var sample = CreateSampleEnvelope();

        var first = Serialize(sample);
        var second = Serialize(sample);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Serialize_NullRoot_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => Serialize<string>(null!));
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Serialize_ObjectWithRuntimeType_RoundTrips_AsRuntimeLayout()
    {
        BaseModel value = new DerivedModel { BaseField = 1, DerivedField = "extra" };
        var bytes = Serialize(value, typeof(BaseModel));
        var restored = Deserialize<BaseModel>(bytes);

        Assert.Equal(1, restored.BaseField);
        Assert.IsNotType<DerivedModel>(restored);
    }

    [Fact]
    public void Serialize_ObjectWithRuntimeType_MismatchedInstance_ThrowsArgumentException()
    {
        object value = new DerivedModel { BaseField = 1, DerivedField = "x" };

        var exception = Assert.Throws<ArgumentException>(() => Serialize(value, typeof(UnrelatedModel)));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Deserialize_WithRuntimeType_RoundTrips()
    {
        var sample = CreateSampleEnvelope();
        var bytes = Serialize(sample);

        var restored = Deserialize(bytes, typeof(TestEnvelope));

        Assert.IsType<TestEnvelope>(restored);
        AssertEqual(sample, (TestEnvelope)restored);
    }

    [Fact]
    public void Deserialize_WithRuntimeType_NullType_ThrowsArgumentNullException()
    {
        var bytes = Serialize(CreateSampleEnvelope());

        var exception = Assert.Throws<ArgumentNullException>(() => Deserialize(bytes, null!));

        Assert.Equal("runtimeType", exception.ParamName);
    }

    [Fact]
    public async Task DeserializeAsync_FromStream_RoundTrips()
    {
        var sample = CreateSampleEnvelope();
        var bytes = Serialize(sample);
        await using var stream = new System.IO.MemoryStream(bytes);

        var restored = await DeserializeAsync<TestEnvelope>(stream);

        AssertEqual(sample, restored);
    }

    [Fact]
    public async Task DeserializeAsync_WithRuntimeType_RoundTrips()
    {
        var sample = CreateSampleEnvelope();
        var bytes = Serialize(sample);
        await using var stream = new System.IO.MemoryStream(bytes);

        var restored = await DeserializeAsync(stream, typeof(TestEnvelope));

        Assert.IsType<TestEnvelope>(restored);
        AssertEqual(sample, (TestEnvelope)restored);
    }

    [Fact]
    public async Task DeserializeAsync_EmptyStream_ThrowsArgumentException()
    {
        await using var stream = new System.IO.MemoryStream();

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => DeserializeAsync(stream, typeof(TestEnvelope)));

        Assert.Equal("stream", exception.ParamName);
    }

    [Fact]
    public void Deserialize_EmptyPayload_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => Deserialize<TestEnvelope>([]));
        Assert.Equal("payload", exception.ParamName);
    }

    [Fact]
    public void Deserialize_NullForNonNullableRoot_ThrowsInvalidOperationException()
    {
        var payloadForNullString = new byte[] { 0 };

        var exception = Assert.Throws<InvalidOperationException>(() => Deserialize<string>(payloadForNullString));

        Assert.Equal("Deserialization produced null for a non-nullable root type.", exception.Message);
    }

    [Fact]
    public void SerializeAndDeserialize_NullableProperty_PreservesNull()
    {
        var sample = CreateSampleEnvelope();
        sample.OptionalCount = null;
        sample.Child = null;

        var bytes = Serialize(sample);
        var restored = Deserialize<TestEnvelope>(bytes);

        Assert.Null(restored.OptionalCount);
        Assert.Null(restored.Child);
    }

    [Fact]
    public void SerializeAndDeserialize_NullableValueType_PreservesValue()
    {
        var sample = CreateSampleEnvelope();
        sample.OptionalCount = 999_999;

        var bytes = Serialize(sample);
        var restored = Deserialize<TestEnvelope>(bytes);

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

        var bytes = Serialize(sample);
        var restored = Deserialize<PrimitiveEnvelope>(bytes);

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

        var bytes = Serialize(sample);
        var restored = Deserialize<CollectionEnvelope>(bytes);

        Assert.Equal(sample.Children.Count, restored.Children.Count);
        Assert.Equal(sample.ChildArray.Length, restored.ChildArray.Length);
        Assert.Equal(sample.Children[0].Label, restored.Children[0].Label);
        Assert.Equal(sample.Children[1].IsActive, restored.Children[1].IsActive);
        Assert.Equal(sample.ChildArray[1].Label, restored.ChildArray[1].Label);
    }

    [Fact]
    public void SerializeAndDeserialize_CbIgnore_ExcludesPropertyFromWire()
    {
        var sample = new ModelWithIgnoredProperty
        {
            Included = 42,
            Ignored = "should-not-round-trip"
        };

        var bytes = Serialize(sample);
        var restored = Deserialize<ModelWithIgnoredProperty>(bytes);

        Assert.Equal(42, restored.Included);
        Assert.Null(restored.Ignored);
    }

    [Fact]
    public void Serialize_CbIgnore_ProducesSmallerPayloadThanIfSerialized()
    {
        var withIgnore = new ModelWithIgnoredProperty { Included = 1, Ignored = new string('x', 100) };
        var equivalent = new ModelWithoutIgnoredProperty { Included = 1 };

        var bytesWithIgnore = Serialize(withIgnore);
        var bytesEquivalent = Serialize(equivalent);

        Assert.Equal(bytesEquivalent, bytesWithIgnore);
    }

    [Fact]
    public void Serialize_UsesSyncOrder_IrrespectiveOfDeclarationOrder()
    {
        var first = new OrderedShapeA { Name = "alpha", Count = 33 };
        var second = new OrderedShapeB { Name = "alpha", Count = 33 };

        var bytesA = Serialize(first);
        var bytesB = Serialize(second);

        Assert.Equal(bytesA, bytesB);
    }

    [Fact]
    public void Serialize_TypeWithoutParameterlessConstructor_ThrowsInvalidOperationException()
    {
        var sample = new NoParameterlessCtorEnvelope { Name = "fail" };

        var exception = Assert.Throws<InvalidOperationException>(() => Serialize(sample));

        Assert.Contains("A public parameterless constructor is required.", exception.Message);
    }

    [Fact]
    public void Serialize_UnsupportedInterfacePropertyType_ThrowsInvalidOperationException()
    {
        var sample = new UnsupportedCollectionEnvelope { Values = new List<int> { 1, 2, 3 } };

        var exception = Assert.Throws<InvalidOperationException>(() => Serialize(sample));

        Assert.Contains("A public parameterless constructor is required.", exception.Message);
    }

    [Fact]
    public void Deserialize_TruncatedPayload_ThrowsInvalidOperationException()
    {
        var sample = CreateSampleEnvelope();
        var full = Serialize(sample);
        var truncated = full[..^1];

        var exception = Assert.Throws<InvalidOperationException>(() => Deserialize<TestEnvelope>(truncated));

        Assert.Equal("Unexpected end of payload.", exception.Message);
    }

    [Fact]
    public void CompactPayload_IsSmallerThanJson_ForRepresentativeModel()
    {
        var sample = CreateLargeRepresentativeEnvelope();
        var json = JsonSerializer.Serialize(sample);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var compactBytes = Serialize(sample);

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
            _ = Serialize(sample);
        }
        sw.Stop();

        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), $"Serialize performance smoke test exceeded threshold: {sw.Elapsed.TotalMilliseconds:F2} ms");
    }

    [Fact]
    public void Deserialize_PerformanceSmoke_CompletesWithinReasonableTime()
    {
        var sample = CreateLargeRepresentativeEnvelope();
        var bytes = Serialize(sample);
        const int iterations = 5_000;

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < iterations; i++)
        {
            _ = Deserialize<TestEnvelope>(bytes);
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
        [CbIndex(0)]
        public int Id { get; set; }

        [CbIndex(1)]
        public string Name { get; set; } = string.Empty;

        [CbIndex(2)]
        public TestPriority Priority { get; set; }

        [CbIndex(3)]
        public DateTime CreatedAtUtc { get; set; }

        [CbIndex(4)]
        public Guid CorrelationId { get; set; }

        [CbIndex(5)]
        public int? OptionalCount { get; set; }

        [CbIndex(6)]
        public List<string> Tags { get; set; } = [];

        [CbIndex(7)]
        public int[] Readings { get; set; } = [];

        [CbIndex(8)]
        public byte[] Payload { get; set; } = [];

        [CbIndex(9)]
        public TestChild? Child { get; set; }
    }

    private sealed class TestChild
    {
        [CbIndex(0)]
        public string Label { get; set; } = string.Empty;

        [CbIndex(1)]
        public bool IsActive { get; set; }
    }

    private sealed class PrimitiveEnvelope
    {
        [CbIndex(0)] public bool BoolValue { get; set; }
        [CbIndex(1)] public byte ByteValue { get; set; }
        [CbIndex(2)] public short ShortValue { get; set; }
        [CbIndex(3)] public int IntValue { get; set; }
        [CbIndex(4)] public long LongValue { get; set; }
        [CbIndex(5)] public ushort UShortValue { get; set; }
        [CbIndex(6)] public uint UIntValue { get; set; }
        [CbIndex(7)] public ulong ULongValue { get; set; }
        [CbIndex(8)] public float FloatValue { get; set; }
        [CbIndex(9)] public double DoubleValue { get; set; }
        [CbIndex(10)] public decimal DecimalValue { get; set; }
        [CbIndex(11)] public DateTime DateTimeValue { get; set; }
        [CbIndex(12)] public Guid GuidValue { get; set; }
        [CbIndex(13)] public TestPriority EnumValue { get; set; }
    }

    private sealed class CollectionEnvelope
    {
        [CbIndex(0)]
        public List<TestChild> Children { get; set; } = [];

        [CbIndex(1)]
        public TestChild[] ChildArray { get; set; } = [];
    }

    private sealed class ModelWithIgnoredProperty
    {
        [CbIndex(0)]
        public int Included { get; set; }

        [CbIgnore]
        public string? Ignored { get; set; }
    }

    private sealed class ModelWithoutIgnoredProperty
    {
        [CbIndex(0)]
        public int Included { get; set; }
    }

    private sealed class OrderedShapeA
    {
        [CbIndex(1)]
        public string Name { get; set; } = string.Empty;

        [CbIndex(0)]
        public int Count { get; set; }
    }

    private sealed class OrderedShapeB
    {
        [CbIndex(0)]
        public int Count { get; set; }

        [CbIndex(1)]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NoParameterlessCtorEnvelope
    {
        [CbIndex(0)]
        public NoParameterlessCtorChild Child { get; set; } = new("x");

        [CbIndex(1)]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class NoParameterlessCtorChild(string value)
    {
        [CbIndex(0)]
        public string Value { get; set; } = value;
    }

    private sealed class UnsupportedCollectionEnvelope
    {
        [CbIndex(0)]
        public IEnumerable<int> Values { get; set; } = [];
    }

    private class BaseModel
    {
        [CbIndex(0)]
        public int BaseField { get; set; }
    }

    private sealed class DerivedModel : BaseModel
    {
        [CbIndex(1)]
        public string DerivedField { get; set; } = string.Empty;
    }

    private sealed class UnrelatedModel
    {
        [CbIndex(0)]
        public int Id { get; set; }
    }
}
