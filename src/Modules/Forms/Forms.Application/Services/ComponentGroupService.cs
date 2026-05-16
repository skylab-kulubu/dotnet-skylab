using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Contracts;
using Skylab.Forms.Application.Contracts.ComponentGroup;
using Skylab.Forms.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Skylab.Forms.Application.Services;

public class ComponentGroupService : IComponentGroupService
{
    private readonly IFormsDbContext _context;

    public ComponentGroupService(IFormsDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceResult<ComponentGroupContract>> CreateGroupAsync(ComponentGroupUpsertRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var newGroup = new ComponentGroup
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Schema = request.Schema ?? new(),
            OwnedBy = userId
        };

        _context.ComponentGroups.Add(newGroup);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceResult<ComponentGroupContract>(ServiceStatus.Success, Data: MapToContract(newGroup));
    }

    public async Task<ServiceResult<ComponentGroupContract>> UpdateGroupAsync(Guid id, ComponentGroupUpsertRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var existingGroup = await _context.ComponentGroups.FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (existingGroup == null)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotFound, Message: "Grup bulunamadı.");

        if (existingGroup.OwnedBy != userId)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotAuthorized, Message: "Bu grubu düzenleme yetkiniz yok.");

        existingGroup.Title = request.Title;
        existingGroup.Description = request.Description;
        existingGroup.Schema = request.Schema ?? new();

        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceResult<ComponentGroupContract>(ServiceStatus.Success, Data: MapToContract(existingGroup));
    }

    public async Task<ServiceResult<PagedResult<ComponentGroupContract>>> GetUserGroupsAsync(Guid userId, GetComponentGroupsRequest request, CancellationToken cancellationToken = default)
    {
        var query = _context.ComponentGroups.AsNoTracking().Where(g => g.OwnedBy == userId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchTerm = request.Search.Trim().ToLower();
            query = query.Where(g => g.Title.ToLower().Contains(searchTerm));
        }

        if (request.SortDirection?.ToLower() == "ascending")
            query = query.OrderBy(g => g.CreatedAt);
        else
            query = query.OrderByDescending(g => g.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var groups = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new ComponentGroupContract(g.Id, g.Title, g.Description, g.Schema))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<ComponentGroupContract>(groups, totalCount, request.Page, request.PageSize);

        return new ServiceResult<PagedResult<ComponentGroupContract>>(ServiceStatus.Success, Data: result);
    }

    public async Task<ServiceResult<ComponentGroupContract>> GetGroupByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await _context.ComponentGroups.AsNoTracking().FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group == null)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotFound, Message: "Grup bulunamadı.");

        if (group.OwnedBy != userId)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotAuthorized, Message: "Yetkiniz yok.");

        return new ServiceResult<ComponentGroupContract>(ServiceStatus.Success, Data: MapToContract(group));
    }

    public async Task<ServiceResult<bool>> DeleteGroupAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await _context.ComponentGroups.FirstOrDefaultAsync(g => g.Id == id && g.OwnedBy == userId, cancellationToken);

        if (group == null)
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "Grup bulunamadı veya yetkiniz yok.");

        _context.ComponentGroups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Grup silindi.");
    }

    private static ComponentGroupContract MapToContract(ComponentGroup group)
    {
        return new ComponentGroupContract(group.Id, group.Title, group.Description, group.Schema);
    }
}
