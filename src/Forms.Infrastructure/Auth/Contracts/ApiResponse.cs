namespace Skylab.Forms.Infrastructure.Auth.Contracts;

internal sealed record ApiResponse<T>(
    bool Success,
    string Message,
    T? Data
);
