using System.Collections.Generic;
using AIaaS.Auditing.Dto;
using AIaaS.Dto;

namespace AIaaS.Auditing.Exporting
{
    public interface IAuditLogListExcelExporter
    {
        FileDto ExportToFile(List<AuditLogListDto> auditLogListDtos);

        FileDto ExportToFile(List<EntityChangeListDto> entityChangeListDtos);
    }
}
