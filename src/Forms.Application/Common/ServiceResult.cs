using Skylab.Forms.Application.Common;

namespace Skylab.Forms.Application.Common;

public record ServiceResult<T>(
    ServiceStatus Status,
    T? Data = default,
    string? Message = null
);
