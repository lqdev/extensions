# OpenResponses Samples

This directory contains sample applications demonstrating how to use the OpenResponses packages with Microsoft.Extensions.AI.

## Samples

### 1. HttpClientSample (Planned)
Demonstrates using `Microsoft.Extensions.AI.OpenResponses.Http` to connect to various OpenResponses-compliant endpoints.

**Providers shown:**
- Ollama (local)
- OpenRouter (cloud gateway)  
- Custom OpenResponses server

### 2. AspNetCoreSample (Planned)
Demonstrates using `Microsoft.Extensions.AI.OpenResponses.AspNetCore` to expose an IChatClient as an OpenResponses endpoint.

**Features shown:**
- Basic endpoint setup
- Streaming support
- Authentication
- Multiple client routing

### 3. E2ESample (Planned)
End-to-end demonstration combining both client and server:
- ASP.NET Core app exposes Ollama via OpenResponses endpoint
- Console app consumes that endpoint using the HTTP client
- Demonstrates provider-neutral interoperability

## Quick Start Examples

### Using the HTTP Client

```csharp
using Microsoft.Extensions.AI;

// Connect to Ollama
var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
    baseUri: new Uri("http://localhost:11434/v1"),
    modelId: "llama2",
    apiKey: null);

var response = await client.GetResponseAsync("What is the capital of France?");
Console.WriteLine(response.Message.Text);
```

### Using the ASP.NET Core Middleware

```csharp
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

// Register any IChatClient
builder.Services.AddSingleton<IChatClient>(sp =>
    OpenResponsesClientExtensions.CreateOpenResponsesClient(
        new Uri("http://localhost:11434/v1"),
        "llama2"));

var app = builder.Build();

// Expose as OpenResponses endpoint
app.MapOpenResponsesEndpoint();  // Available at /v1/responses

app.Run();
```

## Key Concepts

1. **Provider Neutrality**: Same code works with OpenAI, Ollama, OpenRouter, etc.
2. **Bi-directional Conversion**: IChatClient ↔ OpenResponses format
3. **Streaming**: SSE-based streaming responses  
4. **Interoperability**: .NET apps can consume and serve OpenResponses

## Additional Resources

- [OpenResponses Specification](https://www.openresponses.org/specification)
- [Microsoft.Extensions.AI Documentation](https://aka.ms/meai)
- [Ollama](https://ollama.ai/)
- [OpenRouter](https://openrouter.ai/)
