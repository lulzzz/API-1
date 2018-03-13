using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Aiursoft.API.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aiursoft.Pylon.Services;

namespace Aiursoft.API.Services
{
    public class TimedCleaner : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;
        private APIDbContext _dbContext;

        public TimedCleaner(
            ILogger<TimedCleaner> logger,
            APIDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            await AllClean(_dbContext);
        }
        public async Task AllClean(APIDbContext _dbContext)
        {
            try
            {
                await ClearTimeOutAccessToken(_dbContext);
                await ClearTimeOutOAuthPack(_dbContext);
                _logger.LogInformation("Clean finished!");
            }
            catch (Exception e)
            {
                _logger.LogCritical(e.Message);
            }
        }
        public Task ClearTimeOutAccessToken(APIDbContext _dbContext)
        {
            _dbContext.AccessToken.Delete(t => !t.IsAlive);
            return _dbContext.SaveChangesAsync();
        }

        public Task ClearTimeOutOAuthPack(APIDbContext _dbContext)
        {
            _dbContext.OAuthPack.Delete(t => t.IsUsed == true);
            _dbContext.OAuthPack.Delete(t => !t.IsAlive);
            return _dbContext.SaveChangesAsync();
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
