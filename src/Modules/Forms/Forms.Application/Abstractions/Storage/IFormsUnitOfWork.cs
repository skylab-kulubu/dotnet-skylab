namespace Skylab.Forms.Application.Abstractions.Storage;

public interface IFormsUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
