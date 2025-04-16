using Abp.Domain.Entities;
using Abp.EntityFrameworkCore;
using AIaaS.EntityFrameworkCore;
using AIaaS.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AIaaS.RawSQL
{
    public class RawSQLRepository<TEntity, TPrimaryKey> : AIaaSRepositoryBase<TEntity, TPrimaryKey>
        where TEntity : class, IEntity<TPrimaryKey>
    {
        protected RawSQLRepository(IDbContextProvider<AIaaSDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, params object[] parameters)
        {
            return await GetDbContext().Database.ExecuteSqlRawAsync(sql, parameters);
        }
    }
}