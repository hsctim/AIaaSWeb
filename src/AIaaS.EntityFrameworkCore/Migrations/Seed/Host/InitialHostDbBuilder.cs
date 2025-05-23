﻿using AIaaS.EntityFrameworkCore;

namespace AIaaS.Migrations.Seed.Host
{
    public class InitialHostDbBuilder
    {
        private readonly AIaaSDbContext _context;

        public InitialHostDbBuilder(AIaaSDbContext context)
        {
            _context = context;
        }

        public void Create()
        {
            new DefaultEditionCreator(_context).Create();
            new DefaultLanguagesCreator(_context).Create();
            new HostRoleAndUserCreator(_context).Create();
            new DefaultSettingsCreator(_context).Create();

            _context.SaveChanges();
        }
    }
}
