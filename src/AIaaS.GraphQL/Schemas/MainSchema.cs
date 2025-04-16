using Abp.Dependency;
using GraphQL.Types;
using GraphQL.Utilities;
using AIaaS.Queries.Container;
using System;

namespace AIaaS.Schemas
{
    public class MainSchema : Schema, ITransientDependency
    {
        public MainSchema(IServiceProvider provider) :
            base(provider)
        {
            Query = provider.GetRequiredService<QueryContainer>();
        }
    }
}