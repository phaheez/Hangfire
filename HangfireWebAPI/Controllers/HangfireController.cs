using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Hangfire;
using Newtonsoft.Json.Serialization;
using HangfireWebAPI.Services;

namespace HangfireWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HangfireController : ControllerBase
    {
        private readonly ILogger<HangfireController> _logger;
        private readonly IJobTestService _jobTestService;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly IRecurringJobManager _recurringJobManager;

        public HangfireController(
            ILogger<HangfireController> logger,
            IJobTestService jobTestService,
            IBackgroundJobClient backgroundJobClient,
            IRecurringJobManager recurringJobManager
        )
        {
            _logger = logger;
            _jobTestService = jobTestService;
            _backgroundJobClient = backgroundJobClient;
            _recurringJobManager = recurringJobManager;
        }

        /// <summary>
        /// [Fire-and-Forget Jobs]: processed single time e.g for sending welcome email upon sign-up
        /// executed only once and almost immediately after creation
        /// </summary>
        /// <returns></returns>
        [HttpGet("/FireAndForgetJob")]
        public IActionResult CreateFireAndForgetJob()
        {
            _backgroundJobClient.Enqueue(() => _jobTestService.FireAndForgetJob());
            return Ok();
        }

        /// <summary>
        /// [Delayed Jobs]: to be fired after a specified time 
        /// Delayed tasks are those that we surely want to execute, but just not right now. 
        /// We can schedule them at a certain time, maybe a minute from now or three months from now.
        /// </summary>
        /// <returns></returns>
        [HttpGet("/DelayedJob")]
        public IActionResult CreateDelayedJob()
        {
            _backgroundJobClient.Schedule(() => _jobTestService.DelayedJob(), TimeSpan.FromSeconds(60));
            return Ok();
        }

        /// <summary>
        /// [Recurring Jobs]: occurs in a recursive ways based on timestamp e.g Check for database updates
        /// repeat in a certain interval.
        /// e.g renew subscription;
        /// generating monthly discount/reports/invoice
        /// </summary>
        /// <returns></returns>
        [HttpGet("/RecurringJob")]
        public ActionResult CreateReccuringJob()
        {
            _recurringJobManager.AddOrUpdate("jobId", () => _jobTestService.ReccuringJob(), Cron.Minutely);
            return Ok();
        }

        /// <summary>
        /// [Continuations Jobs]: used when there is a change reactions e.g send confirmation email after unsubscribe
        /// getting two jobs to run one after the other in continuation
        /// </summary>
        /// <returns></returns>
        [HttpGet("/ContinuationJob")]
        public ActionResult CreateContinuationJob()
        {
            var parentJobId = _backgroundJobClient.Enqueue(() => _jobTestService.FireAndForgetJob());
            _backgroundJobClient.ContinueJobWith(parentJobId, () => _jobTestService.ContinuationJob());

            return Ok();
        }
    }
}
