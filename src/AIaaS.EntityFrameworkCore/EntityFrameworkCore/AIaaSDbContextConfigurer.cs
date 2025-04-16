using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace AIaaS.EntityFrameworkCore
{
    public static class AIaaSDbContextConfigurer
    {
        public static void Configure(DbContextOptionsBuilder<AIaaSDbContext> builder, string connectionString)
        {
            builder.UseSqlServer(connectionString);
        }

        public static void Configure(DbContextOptionsBuilder<AIaaSDbContext> builder, DbConnection connection)
        {
            builder.UseSqlServer(connection);
        }
    }
}