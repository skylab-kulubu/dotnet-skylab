using Skylab.Shared.Application.Contracts;
using Skylab.Shared.Application.Contracts.Auth;
using Skylab.Shared.Application.Services;
using Skylab.Shared.Domain.Enums;
using Skylab.Exports.Application.Contracts;
using Skylab.Exports.Application.Services;
using Skylab.Forms.Application.Abstractions.Storage;
using Skylab.Forms.Application.Contracts;
using Skylab.Forms.Application.Contracts.Responses;
using Skylab.Forms.Domain.Entities;
using Skylab.Forms.Domain.Enums;
using Skylab.Forms.Domain.Models;

namespace Skylab.Forms.Application.Services;

public class FormResponseService : IFormResponseService
{
    private readonly IFormRepository _forms;
    private readonly IFormResponseRepository _responses;
    private readonly IFormsUnitOfWork _uow;
    private readonly IExternalUserService _userService;
    private readonly IExcelService _excelService;
    private readonly IFormDraftService _draftService;

    public FormResponseService(
        IFormRepository forms,
        IFormResponseRepository responses,
        IFormsUnitOfWork uow,
        IExternalUserService userService,
        IExcelService excelService,
        IFormDraftService draftService)
    {
        _forms = forms;
        _responses = responses;
        _uow = uow;
        _userService = userService;
        _excelService = excelService;
        _draftService = draftService;
    }

    public async Task<ServiceResult<ResponseSubmitResult>> SubmitResponseAsync(ResponseSubmitRequest contract, Guid? userId, CancellationToken cancellationToken = default)
    {
        var form = await _forms.GetByIdAsync(contract.FormId, cancellationToken);
        if (form == null) return new ServiceResult<ResponseSubmitResult>(ServiceStatus.NotFound, Message: "Form bulunamadı.");
        if (form.Status != FormStatus.Open) return new ServiceResult<ResponseSubmitResult>(ServiceStatus.NotAcceptable, Message: "Form kapalı.");

        if (!form.AllowAnonymousResponses && userId == null) return new ServiceResult<ResponseSubmitResult>(ServiceStatus.Unauthorized, Message: "Bu formu doldurmak için giriş yapmalısınız.");

        if (userId.HasValue && !form.AllowMultipleResponses)
        {
            var hasExistingResponse = await _responses.HasNonArchivedResponseAsync(form.Id, userId.Value, cancellationToken);
            if (hasExistingResponse) return new ServiceResult<ResponseSubmitResult>(ServiceStatus.NotAcceptable, Message: "Bu formu daha önce doldurdunuz.");
        }

        var parentForm = await _forms.GetParentOfAsync(form.Id, cancellationToken);

        if (parentForm != null && userId.HasValue)
        {
            var parentResponse = await _responses.GetLatestForUserAsync(parentForm.Id, userId.Value, cancellationToken);

            if (parentResponse == null)
                return new ServiceResult<ResponseSubmitResult>(ServiceStatus.RequiresParentApproval, Message: "Bu formu doldurmak için önceki aşamayı doldurmanız gerekmektedir.");

            if (parentForm.RequiresManualReview && parentResponse.Status != FormResponseStatus.Approved)
                return new ServiceResult<ResponseSubmitResult>(ServiceStatus.RequiresParentApproval, Message: "Bu formu doldurmak için önceki aşamanın onaylanması gerekmektedir.");

            if (form.AllowMultipleResponses)
            {
                var lastChildResponse = await _responses.GetLatestForUserAsync(form.Id, userId.Value, cancellationToken);

                if (lastChildResponse != null && parentResponse.SubmittedAt <= lastChildResponse.SubmittedAt)
                    return new ServiceResult<ResponseSubmitResult>(ServiceStatus.NotAcceptable, Message: "Yeni bir yanıt göndermek için önceki aşamayı tekrar doldurmanız gerekmektedir.");
            }
        }

        var response = MapToEntity(form, contract.Responses, contract.TimeSpent, userId);

        _responses.Add(response);
        await _uow.SaveChangesAsync(cancellationToken);

        if (userId.HasValue)
        {
            await _draftService.DeleteResponseDraftAsync(form.Id, userId.Value, cancellationToken);
        }

        bool isChild = parentForm != null;
        bool isLinkedFlow = form.LinkedFormId.HasValue || isChild;
        int step = 0;
        string message;
        ServiceStatus status;

        if (isLinkedFlow)
        {
            if (form.RequiresManualReview)
            {
                step = isChild ? 4 : 2;
                status = ServiceStatus.PendingApproval;
                message = "Yanıtınız incelemeye alındı.";
            }
            else if (!isChild && form.LinkedFormId.HasValue)
            {
                step = 3;
                status = ServiceStatus.Success;
                message = "Yanıt kaydedildi, bir sonraki adıma geçebilirsiniz.";
            }
            else
            {
                step = 5;
                status = ServiceStatus.Completed;
                message = "Tüm adımları tamamladınız.";
            }
        }
        else
        {
            status = form.RequiresManualReview ? ServiceStatus.PendingApproval : ServiceStatus.Success;
            message = form.RequiresManualReview ? "Yanıtınız incelemeye alındı." : "Yanıt kaydedildi.";
        }

        var result = new ResponseSubmitResult(response.Id, form.LinkedFormId, step);
        return new ServiceResult<ResponseSubmitResult>(status, Data: result, Message: message);
    }

    public async Task<ServiceResult<FormResponsesListResult>> GetFormResponsesAsync(Guid formId, Guid userId, GetResponsesRequest request, CancellationToken cancellationToken = default)
    {
        var isAuthorized = await _forms.IsUserCollaboratorAsync(formId, userId, cancellationToken);
        if (!isAuthorized) return new ServiceResult<FormResponsesListResult>(ServiceStatus.NotAuthorized, Message: "Bu formun yanıtlarını görüntüleme yetkiniz yok.");

        var paged = await _responses.GetPagedAsync(formId, request, cancellationToken);

        var userIds = paged.Items.Where(r => r.UserId.HasValue).Select(r => r.UserId!.Value)
            .Concat(paged.Items.Where(r => r.ReviewedBy.HasValue).Select(r => r.ReviewedBy!.Value))
            .Distinct()
            .ToList();
        var users = await _userService.GetUsersAsync(userIds, cancellationToken);

        var mappedItems = paged.Items.Select(r =>
        {
            var userDetail = r.UserId.HasValue ? users.FirstOrDefault(u => u.Id == r.UserId) : null;
            var reviewerDetail = r.ReviewedBy.HasValue ? users.FirstOrDefault(u => u.Id == r.ReviewedBy) : null;

            if (userDetail == null && r.UserId.HasValue)
                userDetail = new UserContract(r.UserId.Value, null, "??", null);

            if (reviewerDetail == null && r.ReviewedBy.HasValue)
                reviewerDetail = new UserContract(r.ReviewedBy.Value, null, "??", null);

            return new ResponseSummaryContract(r.Id, userDetail, r.Status, r.IsArchived, reviewerDetail, r.ArchivedBy, r.SubmittedAt, r.ReviewedAt, r.ArchivedAt);
        }).ToList();

        var resultData = new PagedResult<ResponseSummaryContract>(
            mappedItems,
            paged.TotalCount,
            request.Page,
            request.PageSize
        );

        var finalResult = new FormResponsesListResult(resultData, paged.AverageTimeSpent);

        return new ServiceResult<FormResponsesListResult>(ServiceStatus.Success, Data: finalResult);
    }

    public async Task<ServiceResult<ResponseContract>> GetResponseByIdAsync(Guid responseId, Guid userId, CancellationToken cancellationToken = default)
    {
        var response = await _responses.GetByIdWithFormAndCollaboratorsAsync(responseId, cancellationToken);

        if (response == null)
            return new ServiceResult<ResponseContract>(ServiceStatus.NotFound, Message: "Yanıt bulunamadı.");

        var isAuthorized = response.Form.Collaborators.Any(c => c.UserId == userId && c.Role != CollaboratorRole.None);

        if (!isAuthorized)
            return new ServiceResult<ResponseContract>(ServiceStatus.NotAuthorized, Message: "Bu yanıtı görüntüleme yetkiniz yok.");

        FormRelationshipStatus relationshipStatus = FormRelationshipStatus.None;
        Guid? targetLinkedFormId = null;

        if (response.Form.LinkedFormId.HasValue)
        {
            relationshipStatus = FormRelationshipStatus.Parent;
            targetLinkedFormId = response.Form.LinkedFormId.Value;
        }
        else
        {
            var parentForm = await _forms.GetParentOfAsync(response.FormId, cancellationToken);
            if (parentForm != null)
            {
                relationshipStatus = FormRelationshipStatus.Child;
                targetLinkedFormId = parentForm.Id;
            }
        }

        Guid? linkedResponseId = null;

        if (targetLinkedFormId.HasValue && response.UserId.HasValue)
        {
            linkedResponseId = relationshipStatus == FormRelationshipStatus.Parent
                ? await _responses.GetFirstChildResponseIdAsync(targetLinkedFormId.Value, response.UserId.Value, response.SubmittedAt, cancellationToken)
                : await _responses.GetLatestParentResponseIdAsync(targetLinkedFormId.Value, response.UserId.Value, response.SubmittedAt, cancellationToken);
        }

        var userIds = new List<Guid>();
        if (response.UserId.HasValue) userIds.Add(response.UserId.Value);
        if (response.ReviewedBy.HasValue) userIds.Add(response.ReviewedBy.Value);
        if (response.ArchivedBy.HasValue) userIds.Add(response.ArchivedBy.Value);

        var users = await _userService.GetUsersAsync(userIds, cancellationToken);

        var responderUser = response.UserId.HasValue ? users.FirstOrDefault(u => u.Id == response.UserId) : null;
        var reviewerUser = response.ReviewedBy.HasValue ? users.FirstOrDefault(u => u.Id == response.ReviewedBy) : null;
        var archiverUser = response.ArchivedBy.HasValue ? users.FirstOrDefault(u => u.Id == response.ArchivedBy) : null;

        return new ServiceResult<ResponseContract>(
            ServiceStatus.Success,
            Data: MapToDetailContract(response, relationshipStatus, linkedResponseId, responderUser, reviewerUser, archiverUser)
        );
    }

    public async Task<ServiceResult<bool>> UpdateResponseStatusAsync(ResponseStatusUpdateRequest contract, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        var response = await _responses.GetForEditByIdWithFormAndCollaboratorsAsync(contract.ResponseId, cancellationToken);

        if (response == null)
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "İlgili yanıt bulunamadı.");

        var isAuthorized = response.Form.Collaborators.Any(c => c.UserId == reviewerId && c.Role != CollaboratorRole.None);

        if (!isAuthorized)
            return new ServiceResult<bool>(ServiceStatus.NotAuthorized, Message: "Bu yanıtı onaylama veya reddetme yetkiniz yok.");

        if (response.IsArchived)
            return new ServiceResult<bool>(ServiceStatus.NotAcceptable, Message: "Arşivlenmiş yanıtlar üzerinde değişiklik yapılamaz.");

        response.Status = contract.NewStatus;
        response.ReviewedBy = reviewerId;
        response.ReviewNote = contract.Note;
        response.ReviewedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);

        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Yanıt durumu başarıyla güncellendi.");
    }

    public async Task<ServiceResult<bool>> ArchiveResponseAsync(Guid responseId, Guid archiverId, CancellationToken cancellationToken = default)
    {
        var response = await _responses.GetForEditByIdWithFormAndCollaboratorsAsync(responseId, cancellationToken);

        if (response == null)
            return new ServiceResult<bool>(ServiceStatus.NotFound, Message: "İlgili yanıt bulunamadı.");

        var isAuthorized = response.Form.Collaborators.Any(c => c.UserId == archiverId && c.Role != CollaboratorRole.None);

        if (!isAuthorized)
            return new ServiceResult<bool>(ServiceStatus.NotAuthorized, Message: "Bu yanıtı arşivleme yetkiniz yok.");

        if (response.IsArchived)
            return new ServiceResult<bool>(ServiceStatus.NotAcceptable, Message: "Bu yanıt zaten arşivlenmiş.");

        if (response.Status == FormResponseStatus.Pending)
        {
            response.Status = FormResponseStatus.Declined;
            response.ReviewNote = "Arşivlendiği için sistem tarafından otomatik olarak reddedildi.";
            response.ReviewedBy = archiverId;
            response.ReviewedAt = DateTime.UtcNow;
        }

        response.IsArchived = true;
        response.ArchivedBy = archiverId;
        response.ArchivedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(cancellationToken);
        return new ServiceResult<bool>(ServiceStatus.Success, Data: true, Message: "Yanıt başarıyla arşivlendi.");
    }

    public async Task<ServiceResult<byte[]>> ExportResponsesToExcelAsync(Guid formId, Guid userId, CancellationToken cancellationToken = default)
    {
        var form = await _forms.GetWithCollaboratorsAsync(formId, cancellationToken);

        if (form == null)
            return new ServiceResult<byte[]>(ServiceStatus.NotFound, Message: "Form bulunamadı.");

        var isAuthorized = form.Collaborators.Any(c => c.UserId == userId && c.Role != CollaboratorRole.None);
        if (!isAuthorized)
            return new ServiceResult<byte[]>(ServiceStatus.NotAuthorized, Message: "Bu formun yanıtlarını dışa aktarma yetkiniz yok.");

        var responses = await _responses.GetNonArchivedByFormAsync(formId, cancellationToken);

        var headers = new List<string>
        {
            "Yanıt ID",
            "Kullanıcı ID",
            "Gönderim Tarihi",
            "Durum",
            "İncelenme Notu"
        };

        foreach (var schemaItem in form.Schema)
        {
            string questionText = schemaItem.Props.TryGetValue("question", out var qVal) ? qVal?.ToString() ?? "İsimsiz Soru" : "Soru";
            headers.Add(questionText);
        }

        var rows = new List<List<string>>();

        foreach (var response in responses)
        {
            var row = new List<string>
            {
                response.Id.ToString(),
                response.UserId?.ToString() ?? "Anonim",
                response.SubmittedAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"),
                response.Status.ToString(),
                response.ReviewNote ?? ""
            };

            foreach (var schemaItem in form.Schema)
            {
                var answerItem = response.Data.FirstOrDefault(d => d.Id == schemaItem.Id);
                row.Add(answerItem?.Answer ?? string.Empty);
            }

            rows.Add(row);
        }

        string sheetName = form.Title.Length > 31 ? form.Title.Substring(0, 31) : form.Title;

        var exportRequest = new ExcelExportRequest(sheetName, headers, rows);
        return _excelService.GenerateExcel(exportRequest);
    }

    private static FormResponse MapToEntity(Form form, List<FormResponseSchemaItem> userResponses, int? timeSpent, Guid? userId)
    {
        var responseData = new List<FormResponseSchemaItem>();

        foreach (var schemaItem in form.Schema)
        {
            var userAnswer = userResponses.FirstOrDefault(r => r.Id == schemaItem.Id)?.Answer ?? string.Empty;

            string questionText = "";
            if (schemaItem.Props.TryGetValue("question", out var qVal) && qVal != null) questionText = qVal.ToString() ?? "";

            responseData.Add(new FormResponseSchemaItem
            {
                Id = schemaItem.Id,
                Type = schemaItem.Type,
                Question = questionText,
                Answer = userAnswer
            });
        }

        return new FormResponse
        {
            FormId = form.Id,
            UserId = userId,
            Data = responseData,
            TimeSpent = timeSpent,
            Status = form.RequiresManualReview ? FormResponseStatus.Pending : FormResponseStatus.NonRestrict,
            SubmittedAt = DateTime.UtcNow
        };
    }

    private static ResponseContract MapToDetailContract(FormResponse response, FormRelationshipStatus relationshipStatus, Guid? linkedResponseId, UserContract? responderUser, UserContract? reviewerUser, UserContract? archiverUser)
    {
        return new ResponseContract(
            response.Id,
            response.FormId,
            responderUser,
            reviewerUser,
            archiverUser,
            response.Data,
            response.TimeSpent,
            response.Status,
            response.IsArchived,
            relationshipStatus,
            response.ReviewNote,
            linkedResponseId,
            response.SubmittedAt,
            response.ReviewedAt,
            response.ArchivedAt
        );
    }
}