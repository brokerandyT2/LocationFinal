using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using x3squaredcircles.License.Server.Data;
using x3squaredcircles.License.Server.Models;

namespace x3squaredcircles.License.Server.Services
{
    public interface ILicenseService
    {
        Task<LicenseAcquireResponse> AcquireLicenseAsync(LicenseAcquireRequest request);
        Task<LicenseHeartbeatResponse> HeartbeatAsync(LicenseHeartbeatRequest request);
        Task<LicenseReleaseResponse> ReleaseLicenseAsync(LicenseReleaseRequest request);
        Task<LicenseStatusResponse> GetLicenseStatusAsync();
        Task CleanupExpiredSessionsAsync();
    }

    public class LicenseService : ILicenseService
    {
        private readonly LicenseDbContext _context;
        private readonly ILicenseConfigService _configService;
        private readonly ILogger<LicenseService> _logger;
        private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(5); // 5 minute heartbeat timeout

        public LicenseService(
            LicenseDbContext context,
            ILicenseConfigService configService,
            ILogger<LicenseService> logger)
        {
            _context = context;
            _configService = configService;
            _logger = logger;
        }

        public async Task<LicenseAcquireResponse> AcquireLicenseAsync(LicenseAcquireRequest request)
        {
            try
            {
                _logger.LogInformation("License acquisition requested for tool: {ToolName} v{ToolVersion} from {IpAddress}",
                    request.ToolName, request.ToolVersion, request.IpAddress);

                // Validate tool is licensed
                var isToolLicensed = await _configService.IsToolLicensedAsync(request.ToolName);
                if (!isToolLicensed)
                {
                    _logger.LogWarning("License denied - tool not licensed: {ToolName}", request.ToolName);
                    return new LicenseAcquireResponse
                    {
                        LicenseGranted = false,
                        Reason = "tool_not_licensed",
                        RetryAfterSeconds = 0
                    };
                }

                // Clean up expired sessions first
                await CleanupExpiredSessionsAsync();

                // Get current configuration and usage
                var config = await _configService.GetLicenseConfigAsync();
                var currentConcurrent = await GetCurrentConcurrentCountAsync();
                var monthlyUsage = await GetCurrentMonthlyUsageAsync();

                // Check if we can grant within base license
                if (currentConcurrent < config.MaxConcurrent)
                {
                    var sessionId = await CreateSessionAsync(request, false);
                    _logger.LogInformation("License granted within base capacity: {SessionId}", sessionId);

                    return new LicenseAcquireResponse
                    {
                        LicenseGranted = true,
                        SessionId = sessionId,
                        BurstMode = false,
                        BurstCountRemaining = config.BurstAllowancePerMonth - monthlyUsage.BurstEventsUsed,
                        ExpiresAt = DateTime.UtcNow.Add(_sessionTimeout)
                    };
                }

                // Check if we can grant in burst mode
                var burstLimit = config.MaxConcurrent * config.BurstMultiplier;
                var canUseBurst = monthlyUsage.BurstEventsUsed < config.BurstAllowancePerMonth;

                if (currentConcurrent < burstLimit && canUseBurst)
                {
                    // Increment burst usage
                    await IncrementBurstUsageAsync();

                    var sessionId = await CreateSessionAsync(request, true);
                    _logger.LogWarning("License granted in BURST MODE: {SessionId} (Bursts remaining: {Remaining})",
                        sessionId, config.BurstAllowancePerMonth - monthlyUsage.BurstEventsUsed - 1);

                    return new LicenseAcquireResponse
                    {
                        LicenseGranted = true,
                        SessionId = sessionId,
                        BurstMode = true,
                        BurstCountRemaining = config.BurstAllowancePerMonth - monthlyUsage.BurstEventsUsed - 1,
                        ExpiresAt = DateTime.UtcNow.Add(_sessionTimeout)
                    };
                }

                // License denied - no capacity available
                _logger.LogWarning("License denied - concurrent limit exceeded. Current: {Current}, Max: {Max}, Burst: {Burst}, Bursts used: {BurstsUsed}",
                    currentConcurrent, config.MaxConcurrent, burstLimit, monthlyUsage.BurstEventsUsed);

                return new LicenseAcquireResponse
                {
                    LicenseGranted = false,
                    Reason = "concurrent_limit_exceeded",
                    BurstEventsExhausted = monthlyUsage.BurstEventsUsed >= config.BurstAllowancePerMonth,
                    RetryAfterSeconds = 300 // Suggest retry in 5 minutes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during license acquisition for tool: {ToolName}", request.ToolName);
                return new LicenseAcquireResponse
                {
                    LicenseGranted = false,
                    Reason = "internal_error",
                    RetryAfterSeconds = 60
                };
            }
        }

        public async Task<LicenseHeartbeatResponse> HeartbeatAsync(LicenseHeartbeatRequest request)
        {
            try
            {
                var session = await _context.ActiveSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (session == null)
                {
                    _logger.LogWarning("Heartbeat failed - session not found: {SessionId}", request.SessionId);
                    return new LicenseHeartbeatResponse { SessionValid = false };
                }

                // Update last heartbeat
                session.LastHeartbeat = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogDebug("Heartbeat received for session: {SessionId}", request.SessionId);

                return new LicenseHeartbeatResponse
                {
                    SessionValid = true,
                    ExpiresAt = DateTime.UtcNow.Add(_sessionTimeout)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during heartbeat for session: {SessionId}", request.SessionId);
                return new LicenseHeartbeatResponse { SessionValid = false };
            }
        }

        public async Task<LicenseReleaseResponse> ReleaseLicenseAsync(LicenseReleaseRequest request)
        {
            try
            {
                var session = await _context.ActiveSessions
                    .FirstOrDefaultAsync(s => s.SessionId == request.SessionId);

                if (session == null)
                {
                    _logger.LogWarning("Release failed - session not found: {SessionId}", request.SessionId);
                    return new LicenseReleaseResponse
                    {
                        SessionReleased = false,
                        TotalMonthlyUsageSessions = 0
                    };
                }

                var sessionDuration = DateTime.UtcNow - session.StartTime;
                _logger.LogInformation("License released: {SessionId} for {ToolName} (Duration: {Duration})",
                    request.SessionId, session.ToolName, sessionDuration);

                // Remove session
                _context.ActiveSessions.Remove(session);
                await _context.SaveChangesAsync();

                // Get total monthly usage for response
                var totalMonthlySessions = await GetTotalMonthlySessionsAsync();

                return new LicenseReleaseResponse
                {
                    SessionReleased = true,
                    TotalMonthlyUsageSessions = totalMonthlySessions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during license release for session: {SessionId}", request.SessionId);
                return new LicenseReleaseResponse { SessionReleased = false };
            }
        }

        public async Task<LicenseStatusResponse> GetLicenseStatusAsync()
        {
            try
            {
                await CleanupExpiredSessionsAsync();

                var config = await _configService.GetLicenseConfigAsync();
                var currentConcurrent = await GetCurrentConcurrentCountAsync();
                var monthlyUsage = await GetCurrentMonthlyUsageAsync();
                var licensedTools = await _configService.GetLicensedToolsAsync();

                return new LicenseStatusResponse
                {
                    MaxConcurrent = config.MaxConcurrent,
                    CurrentConcurrent = currentConcurrent,
                    BurstModeAvailable = monthlyUsage.BurstEventsUsed < config.BurstAllowancePerMonth,
                    MonthlyBurstsUsed = monthlyUsage.BurstEventsUsed,
                    MonthlyBurstsRemaining = config.BurstAllowancePerMonth - monthlyUsage.BurstEventsUsed,
                    LicensedTools = licensedTools
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception getting license status");
                return new LicenseStatusResponse();
            }
        }

        public async Task CleanupExpiredSessionsAsync()
        {
            try
            {
                var cutoffTime = DateTime.UtcNow.Subtract(_sessionTimeout);
                var expiredSessions = await _context.ActiveSessions
                    .Where(s => s.LastHeartbeat < cutoffTime)
                    .ToListAsync();

                if (expiredSessions.Any())
                {
                    _logger.LogInformation("Cleaning up {Count} expired sessions", expiredSessions.Count);
                    _context.ActiveSessions.RemoveRange(expiredSessions);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during session cleanup");
            }
        }

        private async Task<string> CreateSessionAsync(LicenseAcquireRequest request, bool isBurstMode)
        {
            var sessionId = Guid.NewGuid().ToString();
            var now = DateTime.UtcNow;

            var session = new ActiveSession
            {
                SessionId = sessionId,
                ToolName = request.ToolName,
                ToolVersion = request.ToolVersion,
                StartTime = now,
                LastHeartbeat = now,
                IpAddress = request.IpAddress,
                BuildId = request.BuildId
            };

            _context.ActiveSessions.Add(session);
            await _context.SaveChangesAsync();

            return sessionId;
        }

        private async Task<int> GetCurrentConcurrentCountAsync()
        {
            return await _context.ActiveSessions.CountAsync();
        }

        private async Task<MonthlyUsage> GetCurrentMonthlyUsageAsync()
        {
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            var usage = await _context.MonthlyUsages
                .FirstOrDefaultAsync(u => u.Month == currentMonth);

            if (usage == null)
            {
                usage = new MonthlyUsage
                {
                    Month = currentMonth,
                    BurstEventsUsed = 0
                };
                _context.MonthlyUsages.Add(usage);
                await _context.SaveChangesAsync();
            }

            return usage;
        }

        private async Task IncrementBurstUsageAsync()
        {
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            var usage = await GetCurrentMonthlyUsageAsync();

            usage.BurstEventsUsed++;
            await _context.SaveChangesAsync();
        }

        private async Task<int> GetTotalMonthlySessionsAsync()
        {
            // This could be enhanced to track total sessions, not just current active ones
            // For now, return current active sessions count
            return await GetCurrentConcurrentCountAsync();
        }
    }
}