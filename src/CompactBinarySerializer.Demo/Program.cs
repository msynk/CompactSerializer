using System.Diagnostics;
using System.Text;
using System.Text.Json;
using static CompactBinarySerializer.CompactBinarySerializer;
using CompactBinarySerializer.Demo;
using MessagePack;
using MessagePack.Resolvers;

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = null
};

var messagePackOptions = MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

var sample = SampleDataFactory.CreateLargeSample();

var compactBytes = Serialize(sample);
var compactRoundTrip = Deserialize<SyncEnvelope>(compactBytes);

var json = JsonSerializer.Serialize(sample, jsonOptions);
var jsonBytes = Encoding.UTF8.GetBytes(json);

var messagePackBytes = MessagePackSerializer.Serialize(sample, messagePackOptions);
var messagePackRoundTrip = MessagePackSerializer.Deserialize<SyncEnvelope>(messagePackBytes, messagePackOptions);

Console.WriteLine("Schema-aware compact serializer demo");
Console.WriteLine(new string('-', 44));
Console.WriteLine($"JSON bytes:    {jsonBytes.Length}");
Console.WriteLine($"Compact bytes: {compactBytes.Length}");
Console.WriteLine($"MsgPack bytes: {messagePackBytes.Length}");
Console.WriteLine($"Compact vs JSON reduction:   {ComputeReduction(jsonBytes.Length, compactBytes.Length):F2}%");
Console.WriteLine($"MsgPack vs JSON reduction:   {ComputeReduction(jsonBytes.Length, messagePackBytes.Length):F2}%");
Console.WriteLine();
Console.WriteLine($"Roundtrip valid: {IsEquivalent(sample, compactRoundTrip)}");
Console.WriteLine($"MsgPack roundtrip valid: {IsEquivalent(sample, messagePackRoundTrip)}");
Console.WriteLine($"Compact hex sample: {Convert.ToHexString(compactBytes[..Math.Min(40, compactBytes.Length)])}...");
Console.WriteLine();

const int iterations = 100_000;
const int runs = 1;
var compactWriteRuns = new long[runs];
var compactReadRuns = new long[runs];
var jsonWriteRuns = new long[runs];
var jsonReadRuns = new long[runs];
var messagePackWriteRuns = new long[runs];
var messagePackReadRuns = new long[runs];
long compactWriteTotal = 0;
long compactReadTotal = 0;
long jsonWriteTotal = 0;
long jsonReadTotal = 0;
long messagePackWriteTotal = 0;
long messagePackReadTotal = 0;

Console.WriteLine($"Starting benchmark: {runs} rounds, {iterations:N0} iterations each...");
Console.WriteLine("Progress (running averages):");

for (var run = 0; run < runs; run++)
{
    compactWriteRuns[run] = Time(() => { _ = Serialize(sample); }, iterations);
    compactReadRuns[run] = Time(() => { _ = Deserialize<SyncEnvelope>(compactBytes); }, iterations);
    jsonWriteRuns[run] = Time(() => { _ = JsonSerializer.SerializeToUtf8Bytes(sample); }, iterations);
    jsonReadRuns[run] = Time(() => { _ = JsonSerializer.Deserialize<SyncEnvelope>(jsonBytes, jsonOptions)!; }, iterations);
    messagePackWriteRuns[run] = Time(() => { _ = MessagePackSerializer.Serialize(sample, messagePackOptions); }, iterations);
    messagePackReadRuns[run] = Time(() => { _ = MessagePackSerializer.Deserialize<SyncEnvelope>(messagePackBytes, messagePackOptions); }, iterations);

    compactWriteTotal += compactWriteRuns[run];
    compactReadTotal += compactReadRuns[run];
    jsonWriteTotal += jsonWriteRuns[run];
    jsonReadTotal += jsonReadRuns[run];
    messagePackWriteTotal += messagePackWriteRuns[run];
    messagePackReadTotal += messagePackReadRuns[run];

    var completed = run + 1;
    var compactWriteAvg = compactWriteTotal / (double)completed;
    var compactReadAvg = compactReadTotal / (double)completed;
    var jsonWriteAvg = jsonWriteTotal / (double)completed;
    var jsonReadAvg = jsonReadTotal / (double)completed;
    var messagePackWriteAvg = messagePackWriteTotal / (double)completed;
    var messagePackReadAvg = messagePackReadTotal / (double)completed;

    Console.WriteLine(
        $"\rRound {completed}/{runs} | " +
        $"Ser Avg -> Compact: {compactWriteAvg,8:F2} ms, JSON: {jsonWriteAvg,8:F2} ms, MsgPack: {messagePackWriteAvg,8:F2} ms | " +
        $"Deser Avg -> Compact: {compactReadAvg,8:F2} ms, JSON: {jsonReadAvg,8:F2} ms, MsgPack: {messagePackReadAvg,8:F2} ms   ");
}
Console.WriteLine();
Console.WriteLine();

Console.WriteLine($"Serialize {iterations:N0}x (average of {runs} runs)");
Console.WriteLine($"- Compact: {Average(compactWriteRuns):F2} ms");
Console.WriteLine($"- JSON:    {Average(jsonWriteRuns):F2} ms");
Console.WriteLine($"- MsgPack: {Average(messagePackWriteRuns):F2} ms");
Console.WriteLine();
Console.WriteLine($"Deserialize {iterations:N0}x (average of {runs} runs)");
Console.WriteLine($"- Compact: {Average(compactReadRuns):F2} ms");
Console.WriteLine($"- JSON:    {Average(jsonReadRuns):F2} ms");
Console.WriteLine($"- MsgPack: {Average(messagePackReadRuns):F2} ms");

double ComputeReduction(int baseline, int candidate)
{
    if (baseline == 0)
    {
        return 0;
    }

    return (baseline - candidate) * 100.0 / baseline;
}

long Time(Action action, int iterations)
{
    var sw = Stopwatch.StartNew();
    for (var i = 0; i < iterations; i++)
    {
        action();
    }

    sw.Stop();
    return sw.ElapsedMilliseconds;
}

double Average(long[] values) => values.Average();

bool IsEquivalent(SyncEnvelope left, SyncEnvelope right)
{
    return ToCanonicalJson(left) == ToCanonicalJson(right);
}

string ToCanonicalJson(SyncEnvelope value) => JsonSerializer.Serialize(value, jsonOptions);
