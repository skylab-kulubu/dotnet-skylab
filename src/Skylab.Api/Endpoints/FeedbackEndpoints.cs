using Skylab.Feedbacks.Application.Contracts;
using Skylab.Feedbacks.Application.Services;
using Skylab.Api.Extensions;
using Skylab.Shared.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Skylab.Api.Endpoints;

public static class FeedbackEndpoints
{
    public static void MapFeedbackEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("api/feedbacks").WithTags("Feedbacks");

        group.MapPost("/", async ([FromBody] CreateFeedbackRequest request, IFeedbackService service, CancellationToken ct) =>
        {
            var result = await service.CreateFeedbackAsync(request, ct);

            if (result.Status == ServiceStatus.Created)
                return Results.Created($"/api/feedbacks/{result.Data?.Id}", result);

            return result.ToApiResult();
        });

        group.MapGet("/", async ([AsParameters] GetFeedbacksRequest request, IFeedbackService service, CancellationToken ct) =>
        {
            var result = await service.GetFeedbacksAsync(request, ct);
            return result.ToApiResult();
        });

        group.MapGet("/{id:guid}", async (Guid id, IFeedbackService service, CancellationToken ct) =>
        {
            var result = await service.GetFeedbackByIdAsync(id, ct);
            return result.ToApiResult();
        });
    }
}