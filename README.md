# CompactBinarySerializer

[![NuGet](https://img.shields.io/nuget/v/CompactBinarySerializer.svg)](https://www.nuget.org/packages/CompactBinarySerializer)
[![NuGet Downloads](https://img.shields.io/nuget/dt/CompactBinarySerializer.svg)](https://www.nuget.org/packages/CompactBinarySerializer)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A small, schema-aware binary serializer for .NET. It trades JSON's self-describing text for a compact layout: fixed field order, length-prefixed strings and byte arrays, and variable-length integer encoding where it helps. The result is typically smaller payloads and less parsing overhead than `System.Text.Json` for the same POCO graphs at the cost of a custom, non-human-readable format.

## Requirements

- .NET 10 or later

## Installation

```bash
dotnet add package CompactBinarySerializer
```

Or via the NuGet Package Manager in Visual Studio, or by adding directly to your `.csproj`:

```xml
<PackageReference Include="CompactBinarySerializer" Version="0.3.0" />
```

## Quick start

```csharp
using static CompactBinarySerializer.CbSerializer;

var bytes = Serialize(myObject);
var copy = Deserialize<MyType>(bytes);
```

Root values passed to `Serialize` must not be null. For `Deserialize<T>`, the payload must not be empty; if deserialization yields null for a non-nullable `T`, an exception is thrown.

## Modeling rules

Property order: only public instance properties with both a getter and setter participate, except properties marked with `[CbIgnore]` (those are omitted from the payload). Order is determined by `[CbIndex(n)]` ascending; properties without the attribute are serialized after attributed ones, in metadata token order (stable for a given build, not a long-term compatibility contract). Annotate every serialized property you care about for forward-compatible layouts.

```csharp
using CompactBinarySerializer;

public sealed class Example
{
    [CbIndex(0)]
    public int Id { get; set; }

    [CbIndex(1)]
    public string Name { get; set; } = string.Empty;
}
```

**Excluding properties (`CbIgnore`):** apply `[CbIgnore]` to skip a property on the wire. It is neither written during serialize nor consumed during deserialize; after `Deserialize`, that property keeps the default for its type (for example `null` for reference types, `0` for `int`). Adding or removing `[CbIgnore]` changes the field sequence the same way as adding or removing a member, so both ends must agree.

```csharp
public sealed class Example
{
    [CbIndex(0)]
    public int Id { get; set; }

    [CbIgnore]
    public string? Ephemeral { get; set; }
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

## Limitations and stability

- Format versioning is not built in; changing property order, types, inclusion of `[CbIgnore]`, or serializer behavior breaks interoperability with old payloads.
- Security: this is not a hardened interchange format. Do not deserialize untrusted data without threat modeling (no built-in schema or type IDs in the stream).
- Scope: intentionally narrow, good for internal services or caches where you control both ends and want smaller/faster serialization than JSON for compatible models.

## Contributing

The repository includes a companion demo project and a full xUnit test suite. Clone the repo and see the sections below for getting started.

### Repository layout

| Path | Purpose |
|------|---------|
| `src/CompactBinarySerializer/` | Library source |
| `src/CompactBinarySerializer.Demo/` | Console app: benchmark vs JSON and MessagePack |
| `src/CompactBinarySerializer.Tests/` | xUnit test project |
| `src/CompactBinarySerializer.sln` | Solution |

### Building

```bash
dotnet build src/CompactBinarySerializer.sln
```

### Running tests

```bash
dotnet test src/CompactBinarySerializer.sln
```

The test project includes:

- Round-trip correctness for primitives, nested objects, arrays, and lists
- Nullability and guardrail behavior (null roots, empty/truncated payloads)
- Contract behavior (`CbIndex` ordering, `CbIgnore`, and constructor requirements)
- Payload-size sanity check against JSON for a representative model
- Performance smoke checks for serialize/deserialize loops

### Running the demo

```bash
dotnet run --project src/CompactBinarySerializer.Demo/CompactBinarySerializer.Demo.csproj
```

The demo prints byte counts (including gzip, deflate, and brotli sizes for each wire format) and runs a multi-round benchmark comparing CompactBinarySerializer, `System.Text.Json`, and MessagePack.

### Demo payload comparison

The demo serializes `SyncEnvelope` built by `SampleDataFactory.CreateLargeSample()` with:

- **Compact** — this library  
- **JSON** — `System.Text.Json` (default naming; property names preserved)  
- **MessagePack** — `MessagePackSerializer` with `ContractlessStandardResolver`

Uncompressed sizes and reduction versus JSON (baseline):

| Format  | Wire size (bytes) | vs JSON |
|---------|------------------:|--------:|
| JSON    | 57,394            | —       |
| Compact | 18,753            | −67.3%  |
| MsgPack | 40,190            | −30.0%  |

Compressed payload sizes use `CompressionLevel.Optimal` via `GZipStream`, `DeflateStream`, and `BrotliStream` on each serializer’s byte output:

| Encoding | JSON  | Compact | MsgPack |
|----------|------:|--------:|--------:|
| Wire     | 57,394 | 18,753 | 40,190 |
| gzip     | 5,952 | 5,879 | 5,688 |
| deflate  | 5,934 | 5,861 | 5,670 |
| brotli   | 4,056 | 4,860 | 4,446 |

Exact wire and compressed sizes can shift slightly from run to run (for example `Guid` and `DateTime.UtcNow` in the sample). Re-run the demo locally for current numbers.

## License

[MIT](LICENSE)
