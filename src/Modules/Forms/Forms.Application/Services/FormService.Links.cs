using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Domain.Enums;
using Skylab.Forms.Domain.Entities;
using Skylab.Forms.Domain.Enums;
using Skylab.Forms.Application.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Skylab.Forms.Application.Services;

public partial class FormService
{
    private async Task<ServiceResult<bool>> ApplyLinkInternalAsync(Form parentForm, Guid childId, Guid userId, CancellationToken ct)
    {
        var isOwner = parentForm.Collaborators.Any(c => c.UserId == userId && c.Role == CollaboratorRole.Owner);

        if (!isOwner)
            return new ServiceResult<bool>(ServiceStatus.NotAuthorized, Message: "Bu formu bağlamak için yetkiniz yok.");

        if (parentForm.Id == childId) 
            return new ServiceResult<bool>(ServiceStatus.NotAcceptable, Message: "Form kendisine bağlanamaz.");

        var childForm = await _context.Forms.Include(f => f.Collaborators).FirstOrDefaultAsync(f => f.Id == childId, ct);
        
        if (childForm == null) 
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "Bağlanacak alt form bulunamadı.");

        var isChildOwner = childForm.Collaborators.Any(c => c.UserId == userId && c.Role == CollaboratorRole.Owner);

        if (!isChildOwner) 
            return new ServiceResult<bool>(ServiceStatus.NotAuthorized, Message: "Alt formda Owner yetkiniz olmalı.");

        if (parentForm.AllowAnonymousResponses) 
            return new ServiceResult<bool>(ServiceStatus.NotAcceptable, Message: "Anonim formlar bağlanamaz.");

        if (childForm.LinkedFormId.HasValue) 
            return new ServiceResult<bool>(ServiceStatus.NotAcceptable, Message: "Seçilen form zaten başka bir formun alt formu.");

        var isChildAlreadyLinked = await _context.Forms.AnyAsync(f => f.LinkedFormId == childId && f.Id != parentForm.Id, ct);

        if (isChildAlreadyLinked) 
            return new ServiceResult<bool>(ServiceStatus.NotAcceptable, Message: "Bu form zaten başka bir form tarafından kullanılıyor.");

        parentForm.LinkedFormId = childId;

        childForm.Status = parentForm.Status;
        childForm.AllowAnonymousResponses = parentForm.AllowAnonymousResponses;
        childForm.AllowMultipleResponses = parentForm.AllowMultipleResponses;
        childForm.RequiresManualReview = parentForm.RequiresManualReview;

        childForm.SyncChildCollaborators(parentForm.Collaborators);

        return new ServiceResult<bool>(ServiceStatus.Success, true);
    }

    private async Task<ServiceResult<bool>> ApplyUnlinkInternalAsync(Form parentForm, Guid userId, CancellationToken ct)
    {
        var isOwner = parentForm.Collaborators.Any(c => c.UserId == userId && c.Role == CollaboratorRole.Owner);

        if (!isOwner)
            return new ServiceResult<bool>(ServiceStatus.NotAuthorized, Message: "Bu formun bağlantısını koparmak için yetkiniz yok.");

        if (parentForm.LinkedFormId.HasValue)
        {
            var childForm = await _context.Forms.Include(f => f.Collaborators).FirstOrDefaultAsync(f => f.Id == parentForm.LinkedFormId.Value, ct);

            if (childForm != null)
            {
                childForm.Status = FormStatus.Closed;
                childForm.AllowAnonymousResponses = false;
                childForm.AllowMultipleResponses = false;
                childForm.RequiresManualReview = false;
                childForm.Collaborators.Clear();
            }
        }

        parentForm.LinkedFormId = null;
        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Form bağlantısı kaldırıldı.");
    }
}