using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using HangfireWebAPI.Services;
using Hangfire.SqlServer;
using Hangfire.Dashboard;
using HangfireBasicAuthenticationFilter;

namespace HangfireWebAPI
{
    public class Startup
    {
        private static IJobTestService jobTestService;
        private readonly HangfireJobs jobscheduler = new HangfireJobs(jobTestService);

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            #region Configure Hangfire

            if (Configuration.GetSection("RunHangfire").Value == "true")
            {
                var connString = Configuration.GetConnectionString("DefaultConnection");
                services.AddHangfire(x => x.UseSqlServerStorage(connString, new SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));
                
                //Hangfire Retention Time(default=24hrs)
                GlobalConfiguration.Configuration.UseSqlServerStorage(connString).WithJobExpirationTimeout(TimeSpan.FromDays(7));
                
                services.AddHangfireServer();
            }

            #endregion

            services.AddControllers();
            services.AddScoped<IJobTestService, JobTestService>();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "HangfireWebAPI", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IRecurringJobManager recurringJobManager)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "HangfireWebAPI v1"));

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            #region Configure Hangfire

            if (Configuration.GetSection("RunHangfire").Value == "true")
            {
                var hangfireUsername = Configuration.GetSection("HangfireCredentials:UserName").Value;
                var hangfirePassword = Configuration.GetSection("HangfireCredentials:Password").Value;

                app.UseHangfireDashboard("/hangfire", new DashboardOptions
                {
                    AppPath = null,
                    DashboardTitle = "Hangfire Dashboard",
                    //IgnoreAntiforgeryToken = true,
                    IsReadOnlyFunc = (DashboardContext context) => true,
                    //Authorization = new[] { new HangfireCustomBasicAuthenticationFilter { User = hangfireUsername, Pass = hangfirePassword } }
                    Authorization = new[] { new HangireAuthorizationFilter() }
                });
            }

            #endregion

            #region Job Scheduling Tasks

            //RecurringJob.AddOrUpdate<IJobTestService>(x => x.FireAndForgetJob(), "5 * * * * *");
            // Recurring Job for every 10 secs
            recurringJobManager.AddOrUpdate("Insert Employee: Runs Every 10 secs", () => jobscheduler.RunHangfireJob(), "10 * * * * *");

            #endregion

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
