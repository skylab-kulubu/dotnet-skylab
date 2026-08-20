namespace Skylab.Forms.Application.Contracts.Mail;

public record SingleMailRequest(
    string TemplateId,
    string RecipientEmail,
    string RecipientFullName,
    Dictionary<string, object> BodyVariables
);
