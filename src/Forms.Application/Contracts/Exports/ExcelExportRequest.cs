namespace Skylab.Forms.Application.Contracts.Exports;

public record ExcelExportRequest(
    string? SheetName,
    List<string> Headers,
    List<List<string>> Rows
);
