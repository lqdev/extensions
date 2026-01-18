using Microsoft.Extensions.AI;

// Sample demonstrating how to expose an IChatClient as an OpenResponses-compliant endpoint
// This creates a web API that wraps an Ollama client and serves it via OpenResponses protocol

var builder = WebApplication.CreateBuilder(args);

// Register an IChatClient - in this case, wrapping Ollama
// You could replace this with any IChatClient implementation
builder.Services.AddSingleton<IChatClient>(sp =>
{
    // Connect to Ollama running locally
    return OpenResponsesClientExtensions.CreateOpenResponsesClient(
        baseUri: new Uri("http://localhost:11434/v1"),
        modelId: "llama2",
        apiKey: null);
});

// Add Swagger/OpenAPI support (optional)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Map the OpenResponses endpoint at /v1/responses
// This endpoint will accept OpenResponses-compliant requests and delegate to the registered IChatClient
app.MapOpenResponsesEndpoint();

// Add a simple health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "OpenResponses Proxy" }));

// Add an informational root endpoint
app.MapGet("/", () => Results.Ok(new
{
    name = "OpenResponses ASP.NET Core Sample",
    description = "This server exposes an IChatClient (wrapping Ollama) as an OpenResponses-compliant endpoint",
    endpoints = new
    {
        openresponses = "/v1/responses (POST)",
        health = "/health (GET)",
        swagger = "/swagger (GET)"
    },
    usage = @"
        curl -X POST http://localhost:5000/v1/responses \
          -H 'Content-Type: application/json' \
          -d '{
            ""model"": ""llama2"",
            ""input"": [
              {
                ""type"": ""message"",
                ""role"": ""user"",
                ""content"": ""Hello, how are you?""
              }
            ]
          }'
    "
}));

Console.WriteLine("OpenResponses ASP.NET Core Sample");
Console.WriteLine("==================================");
Console.WriteLine($"Server starting on: {builder.Configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000"}");
Console.WriteLine();
Console.WriteLine("Available endpoints:");
Console.WriteLine("  POST /v1/responses  - OpenResponses chat completion");
Console.WriteLine("  GET  /health        - Health check");
Console.WriteLine("  GET  /              - API information");
Console.WriteLine("  GET  /swagger       - API documentation");
Console.WriteLine();
Console.WriteLine("Note: This sample wraps Ollama. Make sure Ollama is running on localhost:11434");
Console.WriteLine();

app.Run();
