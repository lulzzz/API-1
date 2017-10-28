﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aiursoft.API.Data;

namespace Aiursoft.API.Services
{
    public class DataCleaner
    {
        public APIDbContext _dbContext;
        public DataCleaner(APIDbContext _dbContext)
        {
            this._dbContext = _dbContext;
        }
        public async Task StartCleanerService()
        {
            await TimeoutCleaner.AllClean(_dbContext);
        }
    }
}
