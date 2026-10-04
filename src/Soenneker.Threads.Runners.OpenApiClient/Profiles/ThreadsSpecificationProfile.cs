using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using Soenneker.OpenApi.Converters.Meta.Abstract;
using Soenneker.OpenApi.Converters.Meta.Models;

namespace Soenneker.Threads.Runners.OpenApiClient.Profiles;

internal static class ThreadsSpecificationProfile
{
    internal static MetaOpenApiConversionResult Convert(IMetaOpenApiConverter converter,
        IReadOnlyDictionary<string, string> input, MetaOpenApiConverterOptions options, string sourceRevision,
        CancellationToken token = default)
    {
        var responses = new Dictionary<string, JsonObject>(StringComparer.Ordinal);
        int expected = 0;
        foreach ((string key, string json) in input)
        {
            token.ThrowIfCancellationRequested();
            string name = Path.GetFileNameWithoutExtension(key);
            JsonObject node = JsonNode.Parse(json)!.AsObject();
            foreach (JsonObject api in node["apis"]!.AsArray().OfType<JsonObject>())
            {
                expected++;
                string operation = $"{name} {api["method"]!.GetValue<string>()} {api["endpoint"]?.GetValue<string>()}".TrimEnd();
                responses[operation] = new JsonObject { ["$ref"] = "#/components/schemas/" + api["return"]!.GetValue<string>() };
            }
        }
        foreach (var entry in options.ResponseSchemaOverrides) responses[entry.Key] = entry.Value;
        MetaOpenApiConversionResult result = converter.Convert(input, new MetaOpenApiConverterOptions
        {
            GraphApiVersion = options.GraphApiVersion, Title = options.Title, ServerUrl = options.ServerUrl,
            NormalizeForKiota = true, PruneUnusedSchemas = true,
            ThrowOnUnknownTypes = true, ResponseSchemaOverrides = responses
        });
        int actual = result.Document["paths"]!.AsObject().SelectMany(path => path.Value!.AsObject())
            .Sum(operation => operation.Value?["x-meta-operations"]?.AsArray().Count ?? 0);
        if (actual != expected) throw new InvalidOperationException($"Threads coverage mismatch: expected {expected} operations, emitted {actual}.");
        result.Document["x-meta-source"] = new JsonObject
        {
            ["repository"] = "https://github.com/fbsamples/threads_api",
            ["sampleRevision"] = "854fc140a37e20f6a7086cf3ee0065f99d41f646",
            ["documentation"] = "https://www.postman.com/meta/threads/documentation/dht3nzz/threads-api",
            ["revision"] = sourceRevision, ["profile"] = "Threads", ["includedOperationCount"] = expected
        };
        return result;
    }
}
