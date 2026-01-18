// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net.Http;
using OpenAI;
using OpenAI.Responses;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for creating <see cref="IChatClient"/> instances that connect to OpenResponses-compliant endpoints.
/// </summary>
public static class OpenResponsesClientExtensions
{
    /// <summary>
    /// Creates an <see cref="IChatClient"/> for an OpenResponses-compliant endpoint.
    /// </summary>
    /// <param name="baseUri">The base URI of the OpenResponses endpoint (e.g., "https://api.provider.com/v1").</param>
    /// <param name="modelId">The model identifier to use.</param>
    /// <param name="apiKey">The API key for authentication. If null, no authentication header is sent.</param>
    /// <param name="httpClient">Optional <see cref="HttpClient"/> to use for requests.</param>
    /// <returns>An <see cref="IChatClient"/> configured for the specified endpoint.</returns>
    /// <remarks>
    /// This method creates an <see cref="IChatClient"/> that can connect to any OpenResponses-compliant endpoint,
    /// including OpenAI, Anthropic (via compatibility layers), Ollama, OpenRouter, and other providers.
    /// 
    /// The OpenResponses specification is based on the OpenAI Responses API, so this implementation uses
    /// the OpenAI SDK internally but points it at the specified endpoint.
    /// </remarks>
    /// <example>
    /// Connect to Ollama:
    /// <code>
    /// var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
    ///     new Uri("http://localhost:11434/v1"),
    ///     "llama2",
    ///     apiKey: null);  // Ollama doesn't require auth
    /// </code>
    /// 
    /// Connect to OpenRouter:
    /// <code>
    /// var client = OpenResponsesClientExtensions.CreateOpenResponsesClient(
    ///     new Uri("https://openrouter.ai/api/v1"),
    ///     "anthropic/claude-3-opus",
    ///     apiKey: Environment.GetEnvironmentVariable("OPENROUTER_API_KEY"));
    /// </code>
    /// </example>
    public static IChatClient CreateOpenResponsesClient(
        Uri baseUri,
        string modelId,
        string? apiKey = null,
        HttpClient? httpClient = null)
    {
        if (baseUri is null)
        {
            throw new ArgumentNullException(nameof(baseUri));
        }

        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be null or whitespace.", nameof(modelId));
        }

        // Create OpenAI client configured for the specified endpoint
        // The OpenAI SDK allows custom endpoints via OpenAIClientOptions
        var options = new OpenAIClientOptions
        {
            Endpoint = baseUri,
        };

        if (httpClient is not null)
        {
            options.Transport = new HttpClientPipelineTransport(httpClient);
        }

        // Use a dummy API key if none provided (some endpoints don't require auth)
        var client = new OpenAIClient(apiKey ?? "not-needed", options);
        var responsesClient = client.GetResponsesClient(modelId);

        return responsesClient.AsIChatClient();
    }
}
