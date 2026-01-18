# Microsoft.Extensions.AI.OpenResponses.AspNetCore

ASP.NET Core middleware for exposing IChatClient implementations as OpenResponses-compliant endpoints.

## Overview

This package provides ASP.NET Core extensions to expose any `IChatClient` implementation as an OpenResponses-compliant HTTP endpoint. This enables you to wrap existing AI clients (OpenAI, Ollama, custom implementations, etc.) and serve them via a standard OpenResponses API.

## Features

- ✅ Expose any `IChatClient` as OpenResponses endpoint
- ✅ Support for streaming (SSE) and non-streaming responses
- ✅ Automatic request/response conversion
- ✅ Compatible with OpenResponses clients and tools
- ✅ Built on ASP.NET Core Minimal APIs

## Usage

### Basic Setup

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Register your IChatClient (example: Ollama)
builder.Services.AddSingleton<IChatClient>(sp =>
{
    return OpenResponsesClientExtensions.CreateOpenResponsesClient(
        new Uri("http://localhost:11434/v1"),
        "llama2");
});

var app = builder.Build();

// Map the OpenResponses endpoint at POST /v1/responses
app.MapOpenResponsesEndpoint();

app.Run();
```

Now your application exposes an OpenResponses-compliant endpoint at `http://localhost:5000/v1/responses`.

### Custom Route

```csharp
// Map to a custom route
app.MapOpenResponsesEndpoint("/api/chat");
```

### Testing the Endpoint

```bash
# Non-streaming request
curl -X POST http://localhost:5000/v1/responses \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama2",
    "input": [
      {
        "type": "message",
        "role": "user",
        "content": "Hello, how are you?"
      }
    ]
  }'

# Streaming request
curl -X POST http://localhost:5000/v1/responses \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama2",
    "input": [
      {
        "type": "message",
        "role": "user",
        "content": "Tell me a story"
      }
    ],
    "stream": true
  }'
```

### With Authentication

```csharp
app.MapOpenResponsesEndpoint()
    .RequireAuthorization(); // Require authentication

// Or with a specific policy
app.MapOpenResponsesEndpoint()
    .RequireAuthorization("ApiKeyPolicy");
```

### Complete Example: Wrapping Multiple Clients

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register multiple named clients
builder.Services.AddKeyedSingleton<IChatClient>("ollama", (sp, key) =>
    OpenResponsesClientExtensions.CreateOpenResponsesClient(
        new Uri("http://localhost:11434/v1"),
        "llama2"));

builder.Services.AddKeyedSingleton<IChatClient>("openai", (sp, key) =>
    OpenResponsesClientExtensions.CreateOpenResponsesClient(
        new Uri("https://api.openai.com/v1"),
        "gpt-4",
        Environment.GetEnvironmentVariable("OPENAI_API_KEY")));

var app = builder.Build();

// Endpoint for Ollama
app.MapPost("/v1/ollama/responses", async (HttpContext context, [FromKeyedServices("ollama")] IChatClient client) =>
{
    /* Handle with Ollama client */
});

// Endpoint for OpenAI
app.MapPost("/v1/openai/responses", async (HttpContext context, [FromKeyedServices("openai")] IChatClient client) =>
{
    /* Handle with OpenAI client */
});

app.Run();
```

## Use Cases

### 1. **Local Model Gateway**
Expose locally-running models (Ollama, LM Studio) via a standard API that can be consumed by OpenResponses-compatible tools.

### 2. **AI Proxy/Gateway**
Create a unified API gateway that wraps multiple AI providers behind a single OpenResponses interface.

### 3. **Custom Model Serving**
Wrap custom `IChatClient` implementations (e.g., fine-tuned models, specialized pipelines) and serve them via standard protocol.

### 4. **Testing and Development**
Quickly spin up test endpoints for AI functionality during development.

## OpenResponses Compliance

This implementation supports the core OpenResponses specification:

- ✅ Message input/output (user, assistant, system, developer roles)
- ✅ Streaming via Server-Sent Events (SSE)
- ✅ Function/tool calling
- ✅ Multimodal inputs (text, images, files)
- ✅ Response metadata (usage, timing, etc.)

## See Also

- [Microsoft.Extensions.AI.OpenResponses.Http](../Microsoft.Extensions.AI.OpenResponses.Http) - Client implementation
- [OpenResponses Specification](https://www.openresponses.org/specification)
- [ASP.NET Core Minimal APIs](https://learn.microsoft.com/aspnet/core/fundamentals/minimal-apis)
