using Skylab.Shared.Infrastructure.Caching;
using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Contracts.Draft;
using Skylab.Forms.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Skylab.Forms.Application.Services;

public class FormDraftService : IFormDraftService
{
    private readonly ICacheService _cache;
    private readonly IFormsDbContext _context;

    private static readonly TimeSpan ResponseDraftTtl = TimeSpan.FromHours(24);
    private static readonly TimeSpan FormDraftTtl = TimeSpan.FromHours(48);

    public FormDraftService(ICacheService cache, IFormsDbContext context)
    {
        _cache = cache;
        _context = context;
    }

    public async Task<ServiceResult<bool>> SaveResponseDraftAsync(Guid formId, Guid userId, ResponseDraftRequest draft, CancellationToken ct)
    {
        var form = await _context.Forms.AsNoTracking().Select(f => new { f.Id, f.Status }).FirstOrDefaultAsync(f => f.Id == formId, ct);

        if (form == null || form.Status != FormStatus.Open)
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "Form bulunamadı.");

        var key = $"forms:draft:response:{formId}:{userId}";
        await _cache.SetAsync(key, draft,ResponseDraftTtl, ct);

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

        return new ServiceResult<FormDraftContract?>(ServiceStatus.Success, Data: draftRequest.Data);
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