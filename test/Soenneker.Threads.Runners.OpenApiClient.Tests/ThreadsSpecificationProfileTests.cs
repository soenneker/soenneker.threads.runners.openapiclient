using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Soenneker.OpenApi.Converters.Meta;
using Soenneker.OpenApi.Converters.Meta.Models;
using Soenneker.Threads.Runners.OpenApiClient.Profiles;

namespace Soenneker.Threads.Runners.OpenApiClient.Tests;

public sealed class ThreadsSpecificationProfileTests
{
    [Test]
    public void ConvertsBundledDefinitionsWithTypedResponsesAndStringIds()
    {
        var input = Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Specifications"), "*.json")
            .ToDictionary(path => Path.GetFileNameWithoutExtension(path)!, File.ReadAllText, StringComparer.Ordinal);
        var result = ThreadsSpecificationProfile.Convert(new MetaOpenApiConverter(), input,
            new MetaOpenApiConverterOptions { GraphApiVersion = "v1.0", ServerUrl = "https://graph.threads.com" }, "test");
        JsonObject paths = result.Document["paths"]!.AsObject();
        JsonNode publish = paths["/me/threads_publish"]!["post"]!;
        if (publish["requestBody"]!["content"]!["application/x-www-form-urlencoded"]!["schema"]!["properties"]!["creation_id"]!["type"]!.GetValue<string>() != "string")
            throw new InvalidOperationException("Threads container IDs must remain strings.");
        if (paths["/me/threads"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>() != "#/components/schemas/ThreadsMediaList")
            throw new InvalidOperationException("Media reads must use the paginated media model.");
        if (paths["/oauth/access_token"]!["get"]!["responses"]!["200"]!["content"]!["application/json"]!["schema"]!["$ref"]!.GetValue<string>() != "#/components/schemas/ThreadsToken")
            throw new InvalidOperationException("OAuth tokens must not be modeled as Graph edge lists.");
        if (result.Document["x-meta-source"]!["includedOperationCount"]!.GetValue<int>() != 31)
            throw new InvalidOperationException("A bundled Threads operation was lost.");
        result.ValidateOperationCoverage(result.ToJson());
        var changed = (JsonObject)result.Document.DeepClone();
        changed["paths"]!.AsObject().Remove("/me/threads_publish");
        try { result.ValidateOperationCoverage(changed.ToJsonString()); }
        catch (InvalidOperationException) { return; }
        throw new InvalidOperationException("Lost publishing operations must be detected.");
    }
}
