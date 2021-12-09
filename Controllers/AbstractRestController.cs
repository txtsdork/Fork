﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ProjectAvery.Controllers
{
    [ApiController]
    [Route("v1/[controller]")]
    public abstract class AbstractRestController : ControllerBase
    {
        protected readonly ILogger _logger;

        public AbstractRestController(ILogger logger)
        {
            _logger = logger;
        }

        protected void LogRequest()
        {
            _logger.LogInformation($"New request: {Request.Method} {Request.Path}");
        }
    }
}