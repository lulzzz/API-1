using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Aiursoft.API.Data;
using Aiursoft.API.Models;
using Aiursoft.API.Services;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Aiursoft.Pylon;
using Aiursoft.Pylon.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;

namespace Aiursoft.API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public static string emailPassword { get; private set; }
        public bool IsDevelopment { get; set; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            IsDevelopment = env.IsDevelopment();
            if (IsDevelopment)
            {
                Values.ForceRequestHttps = false;
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<APIDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DatabaseConnection")));

            services.AddIdentity<APIUser, IdentityRole>(options =>
                options.Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequiredLength = 6,
                    RequireLowercase = false,
                    RequireUppercase = false,
                    RequireNonAlphanumeric = false
                })
                .AddEntityFrameworkStores<APIDbContext>()
                .AddDefaultTokenProviders();

            services
                .AddLocalization(options => options.ResourcesPath = "Resources");
            services.AddSingleton<IHostedService, TimedCleaner>();
            services.AddTransient<AiurEmailSender>();
            services.AddTransient<AiurSMSSender>();
            services.AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, APIDbContext dbContext)
        {
            AiurSMSSender.SMSAccountFrom = Configuration["SMSAccountFrom"];
            AiurSMSSender.SMSAccountIdentification = Configuration["SMSAccountIdentification"];
            AiurSMSSender.SMSAccountPassword = Configuration["SMSAccountPassword"];
            emailPassword = Configuration["emailpassword"];
            if (string.IsNullOrWhiteSpace(emailPassword))
            {
                throw new InvalidOperationException("Did not get email password from configuration!");
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseAiursoftSupportedCultures();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
        }
    }
}
