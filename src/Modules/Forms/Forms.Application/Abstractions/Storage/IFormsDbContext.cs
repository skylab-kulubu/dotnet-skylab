using Microsoft.EntityFrameworkCore;
using Skylab.Forms.Domain.Entities;

namespace Skylab.Forms.Application.Abstractions.Storage;

// Bu interface'i ileride yavaş yavaş sileceğiz. Şu an
// FormService dışındaki servislerin (ComponentGroup, FormDraft, FormResponse)
// DbContext'e doğrudan bağımlılığını ters çevirmek için burada.
// Her servis kendi repository'sine geçtikçe bu interface'den ilgili DbSet kaldırılabilir.
public interface IFormsDbContext
{
    DbSet<Form> Forms { get; }
    DbSet<FormResponse> Responses { get; }
    DbSet<FormCollaborator> Collaborators { get; }
    DbSet<ComponentGroup> ComponentGroups { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
