using System.Collections.Generic;
using Abp;
using AIaaS.Chat.Dto;
using AIaaS.Dto;

namespace AIaaS.Chat.Exporting
{
    public interface IChatMessageListExcelExporter
    {
        FileDto ExportToFile(UserIdentifier user, List<ChatMessageExportDto> messages);
    }
}
