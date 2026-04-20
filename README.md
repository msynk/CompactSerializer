# CompactSerializer

A small, schema-aware binary serializer for .NET. It trades JSON’s self-describing text for a compact layout: fixed field order, length-prefixed strings and byte arrays, and variable-length integer encoding where it helps. The result is typically smaller payloads and less parsing overhead than `System.Text.Json` for the same POCO graphs at the cost of a custom, non-human-readable format.

The companion demo project compares payload size and rough serialize/deserialize throughput across CompactSerializer, `System.Text.Json`, and MessagePack on a large synthetic model.

## Requirements

- [.NET 10](https://dotnet.microsoft.com/) SDK (preview builds are fine if that is what you use locally)

## Repository layout

| Path | Purpose |
|------|---------|
| `src/CompactSerializer/` | Library: `CompactBinarySerializer`, `SyncOrderAttribute`, readers/writers |
| `src/CompactSerializer.Demo/` | Console app: sample models, benchmark vs JSON and MessagePack |
| `src/CompactSerializer.Tests/` | xUnit test project: functional, error-path, payload-size, and performance-smoke coverage |
| `src/CompactSerializer.sln` | Solution |

## Quick start

Add a project reference to `src/CompactSerializer/CompactSerializer.csproj`, then:

```csharp
using CompactSerializer;

var bytes = CompactBinarySerializer.Serialize(myObject);
var copy = CompactBinarySerializer.Deserialize<MyType>(bytes);
```

Root values passed to `Serialize` must not be null. For `Deserialize<T>`, the payload must not be empty; if deserialization yields null for a non-nullable `T`, an exception is thrown.

## Modeling rules

Property order: only public instance properties with both a getter and setter participate. Order is determined by `[SyncOrder(n)]` ascending; properties without the attribute are serialized after attributed ones, in metadata token order (stable for a given build, not a long-term compatibility contract). Annotate every property you care about for forward-compatible layouts.

```csharp
using CompactSerializer;

public sealed class Example
{
    [SyncOrder(0)]
    public int Id { get; set; }

    [SyncOrder(1)]
    public string Name { get; set; } = string.Empty;
}
```

Constructors: complex types must expose a public parameterless constructor.

Reference types: nullable reference semantics apply, reference types are written with a 1-byte presence flag before the value when null is allowed. The root `T` in `Deserialize<T>` is still validated as non-null when `T` is a non-nullable reference type.

## Supported types

Wire encoding is internal; treat payloads as opaque unless you are maintaining the format.

- Primitives: `bool`, `byte`, `short`, `int`, `long`, `ushort`, `uint`, `ulong`, `float`, `double`, `decimal`
- `string` (UTF-8, length-prefixed)
- `DateTime` (`DateTime.ToBinary` / `FromBinary`)
- `Guid` (16 raw bytes)
- Enums (stored as `long`)
- `byte[]` (length-prefixed)
- `T[]` and `List<T>` only (other `IEnumerable` types are not supported as collection roots)
- Arbitrary POCOs composed of the above, with the property rules above

Types outside this set are not supported and will fail at runtime when encountered.

## Demo

From the repository root:

```bash
dotnet run --project src/CompactSerializer.Demo/CompactSerializer.Demo.csproj
```

The demo prints JSON vs CompactSerializer vs MessagePack byte counts, checks CompactSerializer and MessagePack round-trips, then runs a simple multi-round benchmark for serialize/deserialize performance across all three serializers (not a substitute for [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet), but useful for a quick sanity check).

## Building

```bash
dotnet build src/CompactSerializer.sln
```

## Running tests

```bash
dotnet test src/CompactSerializer.sln
```

The test project includes broad serializer coverage:

- Round-trip correctness for primitives, nested objects, arrays, and lists
- Nullability and guardrail behavior (null roots, empty/truncated payloads)
- Contract behavior (`SyncOrder` ordering and constructor requirements)
- Payload-size sanity check against JSON for a representative model
- Performance smoke checks for serialize/deserialize loops

## Limitations and stability

- Format versioning is not built in; changing property order, types, or serializer behavior breaks interoperability with old payloads.
- Security: this is not a hardened interchange format. Do not deserialize untrusted data without threat modeling (no built-in schema or type IDs in the stream).
- Scope: intentionally narrow, good for internal services or caches where you control both ends and want smaller/faster serialization than JSON for compatible models.
