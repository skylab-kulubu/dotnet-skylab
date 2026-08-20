using Skylab.Forms.Application.Common;
using Skylab.Forms.Application.Contracts.Exports;

namespace Skylab.Forms.Application.Abstractions;

public interface IExcelService
{
    ServiceResult<byte[]> GenerateExcel(ExcelExportRequest request);
}
