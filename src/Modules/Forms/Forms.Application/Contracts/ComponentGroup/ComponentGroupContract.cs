using Skylab.Forms.Domain.Models;
using Skylab.Shared.Application.Contracts.Auth;

namespace Skylab.Forms.Application.Contracts.ComponentGroup;

public record ComponentGroupContract(
    Guid Id,
    string Title,
    string? Description,
    List<FormSchemaItem> Schema,
    UserContract? SharedBy = null
);

public record ComponentGroupUpsertRequest(
    Guid? Id,
    string Title,
    string? Description,
    List<FormSchemaItem> Schema
);

public record ShareTokenContract(string Token, DateTime ExpiresAt);

public record ComponentGroupMetaContract(string Title, string? Description, UserContract? SharedBy);