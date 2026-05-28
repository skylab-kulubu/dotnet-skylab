using System.Text.Json;
using Skylab.Shared.Application.Caching;
using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Contracts.Draft;
using Skylab.Forms.Domain.Entities;

namespace Skylab.Forms.Application.Services;

public class FormDraftService : IFormDraftService
{
    private readonly ICacheService _cache;
    private readonly IFormRepository _forms;

    private static readonly TimeSpan ResponseDraftTtl = TimeSpan.FromHours(168);
    private static readonly TimeSpan FormDraftTtl = TimeSpan.FromHours(168);

    public FormDraftService(ICacheService cache, IFormRepository forms)
    {
        _cache = cache;
        _forms = forms;
    }

    public async Task<ServiceResult<bool>> SaveResponseDraftAsync(Guid formId, Guid userId, ResponseDraftRequest draft, CancellationToken ct)
    {
        if (!await _forms.IsFormOpenAsync(formId, ct))
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "Form bulunamadı.");

        var key = $"forms:draft:response:{formId}:{userId}";
        await _cache.SetAsync(key, draft, ResponseDraftTtl, ct);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true);
    }
    public async Task<ServiceResult<ResponseDraftRequest?>> GetResponseDraftAsync(Guid formId, Guid userId, CancellationToken ct = default)
    {
        var key = $"forms:draft:response:{formId}:{userId}";

        var draft = await _cache.GetAsync<ResponseDraftRequest>(key, ResponseDraftTtl, ct);

        if (draft == null)
            return new ServiceResult<ResponseDraftRequest?>(ServiceStatus.NotFound, Message: "Yanıt taslağı bulunamadı.");

        return new ServiceResult<ResponseDraftRequest?>(ServiceStatus.Success, Data: draft);
    }
    public async Task<ServiceResult<bool>> DeleteResponseDraftAsync(Guid formId, Guid userId, CancellationToken ct = default)
    {
        var key = $"forms:draft:response:{formId}:{userId}";

        await _cache.RemoveAsync(key, ct);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Taslak silindi.");
    }
    public async Task<ServiceResult<bool>> ClearResponseDraftsAsync(Guid formId, CancellationToken ct = default)
    {
        var prefix = $"forms:draft:response:{formId}:";

        await _cache.RemoveByPrefixAsync(prefix, ct);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Forma ait tüm yanıt taslakları temizlendi.");
    }

    public async Task<ServiceResult<bool>> SaveFormDraftAsync(Guid formId, Guid userId, FormDraftRequest draft, CancellationToken ct = default)
    {
        var key = $"forms:draft:form:{formId}:{userId}";

        await _cache.SetAsync(key, draft, FormDraftTtl, ct);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true);
    }

    public async Task<ServiceResult<FormDraftContract?>> GetFormDraftAsync(Guid formId, Guid userId, CancellationToken ct = default)
    {
        var key = $"forms:draft:form:{formId}:{userId}";

        var draftRequest = await _cache.GetAsync<FormDraftRequest>(key, FormDraftTtl, ct);

        if (draftRequest == null)
            return new ServiceResult<FormDraftContract?>(ServiceStatus.NotFound, Message: "Form taslağı bulunamadı.");

        var form = await _forms.GetByIdAsync(formId, ct);
        if (form != null && IsDraftIdenticalToForm(draftRequest.Data, form))
            return new ServiceResult<FormDraftContract?>(ServiceStatus.NotFound, Message: "Form taslağı bulunamadı.");

        return new ServiceResult<FormDraftContract?>(ServiceStatus.Success, Data: draftRequest.Data);
    }

    private static bool IsDraftIdenticalToForm(FormDraftContract draft, Form form)
    {
        if (draft.Title != form.Title) return false;
        if (draft.Description != form.Description) return false;
        if (draft.AllowAnonymousResponses != form.AllowAnonymousResponses) return false;
        if (draft.AllowMultipleResponses != form.AllowMultipleResponses) return false;
        if (draft.RequiresManualReview != form.RequiresManualReview) return false;
        if (draft.Status != form.Status) return false;

        var opts = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var draftSchema = JsonSerializer.Serialize(draft.Schema, opts);
        var formSchema = JsonSerializer.Serialize(form.Schema, opts);
        return draftSchema == formSchema;
    }

    public async Task<ServiceResult<bool>> DeleteFormDraftAsync(Guid formId, Guid userId, CancellationToken ct = default)
    {
        var key = $"forms:draft:form:{formId}:{userId}";

        await _cache.RemoveAsync(key, ct);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Form taslağı silindi.");
    }
    public async Task<ServiceResult<bool>> ClearFormDraftsAsync(Guid formId, CancellationToken ct = default)
    {
        var prefix = $"forms:draft:form:{formId}:";

        await _cache.RemoveByPrefixAsync(prefix, ct);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Forma ait tüm düzenleme taslakları temizlendi.");
    }
}