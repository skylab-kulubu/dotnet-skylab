using Skylab.Forms.Application.Contracts.Identity;
using Skylab.Forms.Domain.Enums;

namespace Skylab.Forms.Application.Contracts.Collaborators;

public record FormCollaboratorContract(
    UserContract User,
    CollaboratorRole Role
);
