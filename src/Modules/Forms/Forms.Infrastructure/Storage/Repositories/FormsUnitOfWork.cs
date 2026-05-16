using Skylab.Forms.Application.Abstractions.Storage;

namespace Skylab.Forms.Infrastructure.Storage.Repositories;

public sealed class FormsUnitOfWork : IFormsUnitOfWork
{
    private readonly FormsDbContext _context;

    public FormsUnitOfWork(FormsDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
