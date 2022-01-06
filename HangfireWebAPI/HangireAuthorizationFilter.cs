using Hangfire;
using Hangfire.Annotations;
using Hangfire.Dashboard;
using HangfireWebAPI.Services;
using System.Threading.Tasks;

namespace HangfireWebAPI
{
    public class HangireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize([NotNull] DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow all authenticated users to see the Dashboard (potentially dangerous).
            //return httpContext.User.Identity.IsAuthenticated;
            return true;
        }
    }

    public class HangfireJobs
    {
        private readonly IJobTestService _jobTestService;
        public HangfireJobs(IJobTestService jobTestService)
        {
            _jobTestService = jobTestService;
        }

        public void RunHangfireJob()
        {
            _jobTestService.ReccuringJob();
        }
    }
}