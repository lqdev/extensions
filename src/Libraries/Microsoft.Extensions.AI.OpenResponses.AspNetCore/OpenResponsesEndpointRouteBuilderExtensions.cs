// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Responses;

namespace Microsoft.Extensions.AI;

/// <summary>
/// Extension methods for mapping OpenResponses endpoints in ASP.NET Core applications.
/// </summary>
public static class OpenResponsesEndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps an OpenResponses-compliant endpoint at POST /v1/responses that exposes the specified <see cref="IChatClient"/>.
    /// </summary>
    /// <param name="endpoints">The <see cref="IEndpointRouteBuilder"/>.</param>
    /// <param name="pattern">The route pattern (default: "/v1/responses").</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/> for the mapped endpoint.</returns>
    /// <remarks>
    /// This endpoint accepts OpenResponses-compliant requests and delegates to an <see cref="IChatClient"/>
    /// registered in dependency injection. Both streaming (SSE) and non-streaming responses are supported.
    /// 
    /// The <see cref="IChatClient"/> must be registered in the service collection before calling this method.
    /// </remarks>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// 
    /// // Register your IChatClient implementation
    /// builder.Services.AddSingleton&lt;IChatClient&gt;(/* your client */);
    /// 
    /// var app = builder.Build();
    /// 
    /// // Map the OpenResponses endpoint
    /// app.MapOpenResponsesEndpoint();
    /// 
    /// app.Run();
    /// </code>
    /// </example>
    public static IEndpointConventionBuilder MapOpenResponsesEndpoint(
        this IEndpointRouteBuilder endpoints,
        string pattern = "/v1/responses")
    {
        if (endpoints is null)
        {
            throw new ArgumentNullException(nameof(endpoints));
        }

        return endpoints.MapPost(pattern, HandleOpenResponsesRequest)
            .WithName("OpenResponses")
            .WithDescription("OpenResponses-compliant chat completion endpoint")
            .WithOpenApi();
    }

    private static async Task<IResult> HandleOpenResponsesRequest(
        HttpContext context,
        IChatClient chatClient,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse the incoming OpenResponses request
            var request = await JsonSerializer.DeserializeAsync<CreateResponseOptions>(
                context.Request.Body,
                cancellationToken: cancellationToken);

            if (request is null)
            {
                return Results.BadRequest(new { error = new { message = "Invalid request body", code = "invalid_request" } });
            }

            // Convert OpenResponses request to ChatMessage[] and ChatOptions
            var messages = request.InputItems?.AsChatMessages().ToList() ?? [];
            var options = ConvertToChatOptions(request);

            // Check if streaming is requested
            bool isStreaming = request.StreamingEnabled ?? false;

            if (isStreaming)
            {
                // Return SSE stream
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.Connection = "keep-alive";

                await foreach (var update in chatClient.GetStreamingResponseAsync(messages, options, cancellationToken))
                {
                    // Convert to OpenResponses streaming update format
                    var openResponsesUpdate = ConvertToStreamingUpdate(update);
                    var eventData = JsonSerializer.Serialize(openResponsesUpdate);

                    await context.Response.WriteAsync($"data: {eventData}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }

                await context.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
                await context.Response.Body.FlushAsync(cancellationToken);

                return Results.Empty;
            }
            else
            {
                // Non-streaming response
                var response = await chatClient.GetResponseAsync(messages, options, cancellationToken);

                // Convert to OpenResponses response format
                var openResponsesResponse = response.AsOpenAIResponseResult(options);

                return Results.Json(openResponsesResponse);
            }
        }
        catch (Exception ex)
        {
            return Results.Json(
                new
                {
                    error = new
                    {
                        message = ex.Message,
                        code = "internal_error",
                        type = ex.GetType().Name
                    }
                },
                statusCode: 500);
        }
    }

    private static ChatOptions ConvertToChatOptions(CreateResponseOptions request)
    {
        var options = new ChatOptions
        {
            ModelId = request.Model,
            Temperature = (float?)request.Temperature,
            TopP = (float?)request.TopP,
            MaxOutputTokens = request.MaxOutputTokenCount,
            Instructions = request.Instructions,
            AllowMultipleToolCalls = request.ParallelToolCallsEnabled ?? true,
        };

        // Convert tools if present
        if (request.Tools is { Count: > 0 })
        {
            options.Tools = new List<AITool>();
            foreach (var tool in request.Tools)
            {
                if (tool is FunctionTool functionTool)
                {
                    // Convert function tool to AIFunction
                    var aiFunction = ConvertToAIFunction(functionTool);
                    if (aiFunction is not null)
                    {
                        options.Tools.Add(aiFunction);
                    }
                }
                else
                {
                    // Wrap other tool types
                    options.Tools.Add(tool.AsAITool());
                }
            }
        }

        return options;
    }

    private static AIFunction? ConvertToAIFunction(FunctionTool functionTool)
    {
        try
        {
            // Extract function metadata
            var metadata = new AIFunctionMetadata(functionTool.FunctionName)
            {
                Description = functionTool.FunctionDescription,
                Parameters = []
            };

            // The actual function implementation would need to be provided by the host
            // For now, we create a placeholder that returns the function tool back
            return AIFunctionFactory.Create(
                metadata,
                (IReadOnlyDictionary<string, object?> args, CancellationToken ct) =>
                {
                    // This is a server implementation - actual execution handled by model
                    return Task.FromResult<object?>(null);
                });
        }
        catch
        {
            return null;
        }
    }

    private static object ConvertToStreamingUpdate(ChatResponseUpdate update)
    {
        // Create an OpenResponses streaming event
        // The exact format depends on the update type
        if (update.Contents.Any())
        {
            var content = update.Contents.First();
            if (content is TextContent textContent)
            {
                return new
                {
                    type = "response.output_text.delta",
                    delta = textContent.Text,
                    response_id = update.ResponseId,
                    item_id = update.MessageId,
                    role = update.Role?.Value
                };
            }
        }

        // Generic update
        return new
        {
            type = "response.update",
            response_id = update.ResponseId,
            role = update.Role?.Value
        };
    }
}
