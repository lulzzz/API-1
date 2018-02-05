using Aiursoft.Pylon.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aiursoft.API.Data
{
    public static class TimeoutCleaner
    {
        public static async Task AllClean(APIDbContext _dbContext)
        {
            try
            {
                await ClearTimeOutAccessToken(_dbContext);
                await ClearTimeOutOAuthPack(_dbContext);
            }
            catch (Exception)
            {

            }
        }
        public static Task ClearTimeOutAccessToken(APIDbContext _dbContext)
        {
            _dbContext.AccessToken.Delete(t => !t.IsAlive);
            return _dbContext.SaveChangesAsync();
        }

        public static Task ClearTimeOutOAuthPack(APIDbContext _dbContext)
        {
            _dbContext.OAuthPack.Delete(t => t.IsUsed == true);
            _dbContext.OAuthPack.Delete(t => !t.IsAlive);
            return _dbContext.SaveChangesAsync();
        }
    }
}
