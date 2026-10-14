using System.Security.Claims;
using System.Text.Json;
using ClinicManagementSystem.Application.DTOs;
using ClinicManagementSystem.Application.Services;
using ClinicManagementSystem.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ClinicManagementSystem.Api.Middleware;

public sealed class MedicalAuditMiddleware(RequestDelegate next,
    ILogger<MedicalAuditMiddleware> logger)
{
    internal static readonly object StateKey = new();

    public async Task InvokeAsync(HttpContext context)
    {
        var descriptor = context.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
        var isPrivateFile = context.Request.Path.StartsWithSegments("/uploads/medical-records", StringComparison.OrdinalIgnoreCase);
        var endpoint = isPrivateFile
            ? new MedicalAuditEndpoint("DIRECT_ATTACHMENT_ACCESS_DENIED", "attachments")
            : MedicalAuditEndpoint.Resolve(descriptor);
        if (endpoint == null) { await next(context); return; }

        if (isPrivateFile)
        {
            // A stored URL must never bypass AttachmentController ownership
            // checks and audit. Keep only the numeric parent reference.
            var segments = context.Request.Path.Value!.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length > 2 && MedicalAuditEndpoint.TryId(segments[2], out var recordId))
                context.Request.RouteValues["medicalRecordId"] = recordId;
        }
        var state = new MedicalAuditRequest(context, endpoint,
            isPrivateFile ? "uploads/medical-records/{medicalRecordId}/{fileName}"
                : descriptor?.AttributeRouteInfo?.Template ?? endpoint.Entity);
        context.Response.Headers.CacheControl = "no-store";
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        context.Items[StateKey] = state;
        try
        {
            if (isPrivateFile)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await state.PersistAsync(context.Response.StatusCode);
                return;
            }
            await next(context);
            // Authentication/authorization short circuits never execute MVC.
            await state.PersistAsync(context.Response.StatusCode);
        }
        catch (MedicalAuditPersistenceException)
        {
            logger.LogError("Medical access audit could not be persisted. Request {RequestId}", context.TraceIdentifier);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await context.Response.WriteAsJsonAsync(new HttpResponseData<object>
            {
                StatusCode = 503,
                Message = "Không thể ghi nhật ký truy cập. Nếu vừa cập nhật dữ liệu, hãy kiểm tra trạng thái trước khi thử lại."
            });
        }
        catch (Exception)
        {
            // Append an error event if response generation failed after the
            // action result was audited. Never modify the earlier event.
            try { await state.PersistFailureAsync(); }
            catch (MedicalAuditPersistenceException)
            { logger.LogError("Medical error audit could not be persisted. Request {RequestId}", context.TraceIdentifier); }
            throw;
        }
        finally { context.Items.Remove(StateKey); }
    }
}

// Captures only IDs and persists via middleware state before MVC releases the
// result (including PDFs). No buffering, request/response body logging or PHI.
public sealed class MedicalAuditResultFilter : IAsyncAlwaysRunResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.HttpContext.Items.TryGetValue(MedicalAuditMiddleware.StateKey, out var item)
            && item is MedicalAuditRequest state)
        {
            state.ReturnedIds = state.Endpoint.GetReturnedIds((context.Result as ObjectResult)?.Value);
            var status = (context.Result as IStatusCodeActionResult)?.StatusCode
                ?? context.HttpContext.Response.StatusCode;
            await state.PersistAsync(status);
        }
        await next();
    }
}

internal sealed class MedicalAuditRequest(HttpContext context, MedicalAuditEndpoint endpoint, string template)
{
    public MedicalAuditEndpoint Endpoint { get; } = endpoint;
    public IReadOnlyList<int> ReturnedIds { get; set; } = Array.Empty<int>();
    private bool _persisted;
    private int _status;

    public async Task PersistAsync(int status)
    {
        if (_persisted) return;
        await WriteAsync(status, "action_result");
        _status = status;
        _persisted = true;
    }

    public Task PersistFailureAsync() => !_persisted
        ? PersistAsync(500)
        : _status is >= 200 and < 300 ? WriteAsync(500, "response_failed") : Task.CompletedTask;

    private async Task WriteAsync(int status, string phase)
    {
        int? actor = context.User.Identity?.IsAuthenticated == true &&
            MedicalAuditEndpoint.TryId(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId : null;
        var ip = context.Connection.RemoteIpAddress;
        if (ip?.IsIPv4MappedToIPv6 == true) ip = ip.MapToIPv4();
        var targetIds = Endpoint.TargetRoute != null &&
            MedicalAuditEndpoint.TryId(context.Request.RouteValues[Endpoint.TargetRoute], out var targetId)
            ? new[] { targetId } : ReturnedIds;
        var references = Endpoint.GetReferences(context);
        var details = JsonSerializer.Serialize(new
        {
            source = "VC24_MEDICAL_ACCESS", timeZone = "UTC", phase,
            method = context.Request.Method, endpoint = template, statusCode = status,
            requestId = context.TraceIdentifier, references, returnedCount = ReturnedIds.Count
        });
        var now = DateTime.UtcNow;
        var entries = (targetIds.Count == 0 ? new int?[] { null } : targetIds.Select(id => (int?)id))
            .Select(id => new AuditLog
            {
                UserId = actor, Action = Endpoint.Action, EntityName = Endpoint.Entity, EntityId = id,
                Details = details, IpAddress = ip?.ToString(), Succeeded = status is >= 200 and < 300,
                OccurredAt = now
            }).ToList();
        try
        {
            await using var scope = context.RequestServices.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<IMedicalAuditWriter>().WriteAsync(entries);
        }
        catch (Exception ex) { throw new MedicalAuditPersistenceException(ex); }
    }
}

internal sealed class MedicalAuditPersistenceException(Exception inner) : Exception("Medical audit persistence failed.", inner);
