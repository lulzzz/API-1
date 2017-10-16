using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using API.Data;
using API.Models;
using API.Services;
using Microsoft.AspNetCore.Identity;
using System;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using AiursoftBase;

namespace API
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public bool IsDevelopment { get; set; }

        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            IsDevelopment = env.IsDevelopment();
            if (IsDevelopment)
            {
                Values.Schema = "http";
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.ConnectToAiursoftDatabase<APIDbContext>("API",IsDevelopment);
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
                .AddLocalization(options => options.ResourcesPath = "Resources")
                .AddTransient<IEmailSender, AuthMessageSender>()
                .AddTransient<ISmsSender, AuthMessageSender>()
                .AddTransient<DataCleaner>()
                .AddMvc()
                .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
                .AddDataAnnotationsLocalization();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, APIDbContext dbContext, DataCleaner dataCleaner)
        {
            var SupportedCultures = new CultureInfo[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("zh-CN")
            };
            app.UseRequestLocalization(new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = SupportedCultures,
                SupportedUICultures = SupportedCultures
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseMvcWithDefaultRoute();
            dataCleaner.StartCleanerService().Wait();
        }
    }
}
