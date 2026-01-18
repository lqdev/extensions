using Microsoft.Extensions.AI;
using System;
using System.Threading.Tasks;

// Sample demonstrating how to use the OpenResponses HTTP client
// to connect to various OpenResponses-compliant endpoints

Console.WriteLine("OpenResponses HTTP Client Sample");
Console.WriteLine("==================================\n");

// Example 1: Connect to Ollama (local)
Console.WriteLine("Example 1: Connecting to Ollama");
Console.WriteLine("Note: Requires Ollama to be running on localhost:11434\n");

try
{
    var ollamaClient = OpenResponsesClientExtensions.CreateOpenResponsesClient(
        baseUri: new Uri("http://localhost:11434/v1"),
        modelId: "llama2",
        apiKey: null);  // No API key needed for Ollama

    Console.WriteLine("Sending request to Ollama...");
    var response = await ollamaClient.GetResponseAsync("Say 'Hello from Ollama!' in exactly 5 words.");
    Console.WriteLine($"Response: {response.Message.Text}\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error connecting to Ollama: {ex.Message}");
    Console.WriteLine("Make sure Ollama is running and the model 'llama2' is available.\n");
}

// Example 2: Connect to OpenRouter (cloud gateway)
Console.WriteLine("Example 2: Connecting to OpenRouter");
Console.WriteLine("Note: Requires OPENROUTER_API_KEY environment variable\n");

var openRouterApiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");
if (!string.IsNullOrEmpty(openRouterApiKey))
{
    try
    {
        var openRouterClient = OpenResponsesClientExtensions.CreateOpenResponsesClient(
            baseUri: new Uri("https://openrouter.ai/api/v1"),
            modelId: "meta-llama/llama-3.1-8b-instruct:free",
            apiKey: openRouterApiKey);

        Console.WriteLine("Sending request to OpenRouter...");
        var response = await openRouterClient.GetResponseAsync("What is 2+2? Answer in one word.");
        Console.WriteLine($"Response: {response.Message.Text}\n");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error connecting to OpenRouter: {ex.Message}\n");
    }
}
else
{
    Console.WriteLine("Skipping OpenRouter example (no API key set)");
    Console.WriteLine("Set OPENROUTER_API_KEY environment variable to try this example.\n");
}

// Example 3: Streaming responses
Console.WriteLine("Example 3: Streaming Response");
Console.WriteLine("Note: Requires Ollama to be running\n");

try
{
    var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
        new Uri("http://localhost:11434/v1"),
        "llama2");

    Console.Write("Response (streaming): ");
    await foreach (var update in client.GetStreamingResponseAsync("Count from 1 to 5, one number per word."))
    {
        if (update.Text is { } text)
        {
            Console.Write(text);
        }
    }
    Console.WriteLine("\n");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}\n");
}

Console.WriteLine("Sample completed!");
Console.WriteLine("\nNext steps:");
Console.WriteLine("- Try connecting to different OpenResponses providers");
Console.WriteLine("- Experiment with streaming, tool calling, and multimodal inputs");
Console.WriteLine("- Check out the AspNetCoreSample to see server-side usage");
