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
<PackageReference Include="CompactBinarySerializer" Version="0.1.0" />
```

## Quick start

```csharp
using static CompactBinarySerializer.CompactBinarySerializer;

var bytes = Serialize(myObject);
var copy = Deserialize<MyType>(bytes);
```

Root values passed to `Serialize` must not be null. For `Deserialize<T>`, the payload must not be empty; if deserialization yields null for a non-nullable `T`, an exception is thrown.

## Modeling rules

Property order: only public instance properties with both a getter and setter participate. Order is determined by `[CompactIndex(n)]` ascending; properties without the attribute are serialized after attributed ones, in metadata token order (stable for a given build, not a long-term compatibility contract). Annotate every property you care about for forward-compatible layouts.

```csharp
using CompactBinarySerializer;

public sealed class Example
{
    [CompactIndex(0)]
    public int Id { get; set; }

    [CompactIndex(1)]
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

## Limitations and stability

- Format versioning is not built in; changing property order, types, or serializer behavior breaks interoperability with old payloads.
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
- Contract behavior (`CompactIndex` ordering and constructor requirements)
- Payload-size sanity check against JSON for a representative model
- Performance smoke checks for serialize/deserialize loops

### Running the demo

```bash
dotnet run --project src/CompactBinarySerializer.Demo/CompactBinarySerializer.Demo.csproj
```

The demo prints byte counts and runs a multi-round benchmark comparing CompactBinarySerializer, `System.Text.Json`, and MessagePack.

## License

[MIT](LICENSE)
