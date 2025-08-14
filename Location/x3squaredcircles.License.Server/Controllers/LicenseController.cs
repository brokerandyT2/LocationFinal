using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using x3squaredcircles.License.Server.Models;
using x3squaredcircles.License.Server.Services;

namespace x3squaredcircles.License.Server.Controllers
{
    [ApiController]
    [Route("license")]
    public class LicenseController : ControllerBase
    {
        private readonly ILicenseService _licenseService;
        private readonly ILogger<LicenseController> _logger;

        public LicenseController(ILicenseService licenseService, ILogger<LicenseController> logger)
        {
            _licenseService = licenseService;
            _logger = logger;
        }

        /// <summary>
        /// Acquire a license for tool execution
        /// </summary>
        /// <param name="request">License acquisition request</param>
        /// <returns>License acquisition response</returns>
        [HttpPost("acquire")]
        public async Task<ActionResult<LicenseAcquireResponse>> AcquireLicense([FromBody] LicenseAcquireRequest request)
        {
            try
            {
                if (request == null)
                {
                    _logger.LogWarning("License acquire request is null");
                    return BadRequest(new LicenseAcquireResponse
                    {
                        LicenseGranted = false,
                        Reason = "invalid_request"
                    });
                }

                if (string.IsNullOrEmpty(request.ToolName))
                {
                    _logger.LogWarning("License acquire request missing tool name");
                    return BadRequest(new LicenseAcquireResponse
                    {
                        LicenseGranted = false,
                        Reason = "missing_tool_name"
                    });
                }

                if (string.IsNullOrEmpty(request.ToolVersion))
                {
                    _logger.LogWarning("License acquire request missing tool version for tool: {ToolName}", request.ToolName);
                    return BadRequest(new LicenseAcquireResponse
                    {
                        LicenseGranted = false,
                        Reason = "missing_tool_version"
                    });
                }

                // Set IP address from request if not provided
                if (string.IsNullOrEmpty(request.IpAddress))
                {
                    request.IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                }

                var response = await _licenseService.AcquireLicenseAsync(request);

                // Log the result for audit trail
                var logLevel = response.LicenseGranted ? LogLevel.Information : LogLevel.Warning;
                _logger.Log(logLevel, "License acquisition result: {Result} for {ToolName} v{ToolVersion} from {IpAddress}. Reason: {Reason}",
                    response.LicenseGranted ? "GRANTED" : "DENIED",
                    request.ToolName,
                    request.ToolVersion,
                    request.IpAddress,
                    response.Reason ?? "success");

                if (response.LicenseGranted)
                {
                    return Ok(response);
                }
                else
                {
                    // Return 429 (Too Many Requests) for concurrent limit exceeded
                    if (response.Reason == "concurrent_limit_exceeded")
                    {
                        Response.Headers.Add("Retry-After", response.RetryAfterSeconds.ToString());
                        return StatusCode(429, response);
                    }

                    // Return 403 (Forbidden) for tool not licensed
                    if (response.Reason == "tool_not_licensed")
                    {
                        return StatusCode(403, response);
                    }

                    // Default to 400 (Bad Request)
                    return BadRequest(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in license acquisition for tool: {ToolName}", request?.ToolName ?? "unknown");
                return StatusCode(500, new LicenseAcquireResponse
                {
                    LicenseGranted = false,
                    Reason = "internal_error",
                    RetryAfterSeconds = 60
                });
            }
        }

        /// <summary>
        /// Send heartbeat to maintain license session
        /// </summary>
        /// <param name="request">Heartbeat request</param>
        /// <returns>Heartbeat response</returns>
        [HttpPost("heartbeat")]
        public async Task<ActionResult<LicenseHeartbeatResponse>> Heartbeat([FromBody] LicenseHeartbeatRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.SessionId))
                {
                    _logger.LogWarning("Invalid heartbeat request - missing session ID");
                    return BadRequest(new LicenseHeartbeatResponse { SessionValid = false });
                }

                var response = await _licenseService.HeartbeatAsync(request);

                if (!response.SessionValid)
                {
                    _logger.LogWarning("Heartbeat failed for session: {SessionId}", request.SessionId);
                    return NotFound(response);
                }

                _logger.LogDebug("Heartbeat successful for session: {SessionId}", request.SessionId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in heartbeat for session: {SessionId}", request?.SessionId ?? "unknown");
                return StatusCode(500, new LicenseHeartbeatResponse { SessionValid = false });
            }
        }

        /// <summary>
        /// Release a license session
        /// </summary>
        /// <param name="request">Release request</param>
        /// <returns>Release response</returns>
        [HttpPost("release")]
        public async Task<ActionResult<LicenseReleaseResponse>> ReleaseLicense([FromBody] LicenseReleaseRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.SessionId))
                {
                    _logger.LogWarning("Invalid release request - missing session ID");
                    return BadRequest(new LicenseReleaseResponse { SessionReleased = false });
                }

                var response = await _licenseService.ReleaseLicenseAsync(request);

                if (response.SessionReleased)
                {
                    _logger.LogInformation("License released successfully for session: {SessionId}", request.SessionId);
                    return Ok(response);
                }
                else
                {
                    _logger.LogWarning("License release failed for session: {SessionId}", request.SessionId);
                    return NotFound(response);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception in license release for session: {SessionId}", request?.SessionId ?? "unknown");
                return StatusCode(500, new LicenseReleaseResponse { SessionReleased = false });
            }
        }

        /// <summary>
        /// Get current license status and usage
        /// </summary>
        /// <returns>License status</returns>
        [HttpGet("status")]
        public async Task<ActionResult<LicenseStatusResponse>> GetLicenseStatus()
        {
            try
            {
                var response = await _licenseService.GetLicenseStatusAsync();
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting license status");
                return StatusCode(500, new LicenseStatusResponse());
            }
        }

        /// <summary>
        /// Manually trigger cleanup of expired sessions (for testing/admin)
        /// </summary>
        /// <returns>Cleanup result</returns>
        [HttpPost("cleanup")]
        public async Task<ActionResult> CleanupExpiredSessions()
        {
            try
            {
                await _licenseService.CleanupExpiredSessionsAsync();
                _logger.LogInformation("Manual session cleanup completed");
                return Ok(new { message = "Cleanup completed", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during manual session cleanup");
                return StatusCode(500, new { error = "Cleanup failed", message = ex.Message });
            }
        }
    }
}