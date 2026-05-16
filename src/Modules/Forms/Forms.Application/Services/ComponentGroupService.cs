using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Contracts;
using Skylab.Forms.Application.Contracts.ComponentGroup;
using Skylab.Forms.Domain.Entities;

namespace Skylab.Forms.Application.Services;

public class ComponentGroupService : IComponentGroupService
{
    private readonly IComponentGroupRepository _groups;
    private readonly IFormsUnitOfWork _uow;

    public ComponentGroupService(IComponentGroupRepository groups, IFormsUnitOfWork uow)
    {
        _groups = groups;
        _uow = uow;
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

        _groups.Add(newGroup);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<ComponentGroupContract>(ServiceStatus.Success, Data: MapToContract(newGroup));
    }

    public async Task<ServiceResult<ComponentGroupContract>> UpdateGroupAsync(Guid id, ComponentGroupUpsertRequest request, Guid userId, CancellationToken cancellationToken = default)
    {
        var existingGroup = await _groups.GetForEditAsync(id, cancellationToken);

        if (existingGroup == null)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotFound, Message: "Grup bulunamadı.");

        if (existingGroup.OwnedBy != userId)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotAuthorized, Message: "Bu grubu düzenleme yetkiniz yok.");

        existingGroup.Title = request.Title;
        existingGroup.Description = request.Description;
        existingGroup.Schema = request.Schema ?? new();

        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<ComponentGroupContract>(ServiceStatus.Success, Data: MapToContract(existingGroup));
    }

    public async Task<ServiceResult<PagedResult<ComponentGroupContract>>> GetUserGroupsAsync(Guid userId, GetComponentGroupsRequest request, CancellationToken cancellationToken = default)
    {
        var data = await _groups.GetUserGroupsAsync(userId, request, cancellationToken);
        return new ServiceResult<PagedResult<ComponentGroupContract>>(ServiceStatus.Success, Data: data);
    }

    public async Task<ServiceResult<ComponentGroupContract>> GetGroupByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetByIdAsync(id, cancellationToken);

        if (group == null)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotFound, Message: "Grup bulunamadı.");

        if (group.OwnedBy != userId)
            return new ServiceResult<ComponentGroupContract>(ServiceStatus.NotAuthorized, Message: "Yetkiniz yok.");

        return new ServiceResult<ComponentGroupContract>(ServiceStatus.Success, Data: MapToContract(group));
    }

    public async Task<ServiceResult<bool>> DeleteGroupAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
    {
        var group = await _groups.GetForEditAsync(id, cancellationToken);

        if (group == null || group.OwnedBy != userId)
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "Grup bulunamadı veya yetkiniz yok.");

        _groups.Remove(group);
        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Grup silindi.");
    }

    private static ComponentGroupContract MapToContract(ComponentGroup group) =>
        new(group.Id, group.Title, group.Description, group.Schema);
}
