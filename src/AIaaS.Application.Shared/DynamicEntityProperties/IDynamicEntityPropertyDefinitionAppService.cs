using System.Collections.Generic;

namespace AIaaS.DynamicEntityProperties
{
    public interface IDynamicEntityPropertyDefinitionAppService
    {
        List<string> GetAllAllowedInputTypeNames();

        List<string> GetAllEntities();
    }
}
