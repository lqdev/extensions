# Microsoft.Extensions.AI.OpenResponses.Http

HTTP client for connecting to OpenResponses-compliant AI endpoints.

## Overview

This package provides an `IChatClient` implementation that can connect to any OpenResponses-compliant endpoint. The OpenResponses specification is an open standard for multi-provider LLM interoperability, inspired by the OpenAI Responses API.

## Supported Providers

- **OpenAI** - Native OpenAI endpoints
- **Ollama** - Local model serving
- **OpenRouter** - Multi-provider AI gateway
- **Anthropic** - Via OpenResponses compatibility layers
- **Google Gemini** - Via OpenResponses compatibility layers
- **Custom endpoints** - Any server implementing the OpenResponses specification

## Usage

### Basic Example

```csharp
using Microsoft.Extensions.AI;

// Connect to Ollama running locally
var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
    baseUri: new Uri("http://localhost:11434/v1"),
    modelId: "llama2",
    apiKey: null);  // Ollama doesn't require authentication

// Use the client
var response = await client.GetResponseAsync("What is the capital of France?");
Console.WriteLine(response.Message.Text);
```

###Using with OpenRouter

```csharp
// Connect to OpenRouter to access multiple AI providers
var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
    baseUri: new Uri("https://openrouter.ai/api/v1"),
    modelId: "anthropic/claude-3-opus",
    apiKey: Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));

var response = await client.GetResponseAsync("Explain quantum computing");
```

### Streaming Responses

```csharp
var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
    new Uri("http://localhost:11434/v1"),
    "llama2");

await foreach (var update in client.GetStreamingResponseAsync("Tell me a story"))
{
    Console.Write(update.Text);
}
```

### With Dependency Injection

```csharp
services.AddSingleton<IChatClient>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient();
    return OpenResponsesClientExtensions.CreateOpenResponsesClient(
        new Uri("http://localhost:11434/v1"),
        "llama2",
        httpClient: httpClient);
});
```

## Configuration

The client supports all standard `ChatOptions` including:
- Temperature and TopP for response randomness
- Max output tokens
- Tool/function calling
- Multimodal inputs (images, files)
- Streaming

## Implementation Notes

This implementation uses the OpenAI .NET SDK internally, configured to point at non-OpenAI endpoints. This is possible because:

1. The OpenResponses specification is based on the OpenAI Responses API
2. The schema is nearly identical (with provider-specific extensions)
3. The OpenAI SDK supports custom endpoints via `OpenAIClientOptions`

This approach provides maximum compatibility while avoiding reimplementation of complex HTTP/SSE handling logic.

## See Also

- [OpenResponses Specification](https://www.openresponses.org/specification)
- [Microsoft.Extensions.AI Documentation](https://aka.ms/meai)
- [OpenAI .NET SDK](https://github.com/openai/openai-dotnet)
