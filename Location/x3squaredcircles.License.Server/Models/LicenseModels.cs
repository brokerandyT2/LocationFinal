using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace x3squaredcircles.License.Server.Models
{
    // Database entities
    public class LicenseConfig
    {
        [Key]
        public int Id { get; set; }
        public int MaxConcurrent { get; set; }
        public string ToolsLicensed { get; set; } = string.Empty; // JSON array
        public int BurstMultiplier { get; set; } = 2;
        public int BurstAllowancePerMonth { get; set; } = 2;
    }

    public class MonthlyUsage
    {
        [Key]
        public string Month { get; set; } = string.Empty; // "2025-01"
        public int BurstEventsUsed { get; set; } = 0;
    }

    public class ActiveSession
    {
        [Key]
        public string SessionId { get; set; } = string.Empty;
        public string ToolName { get; set; } = string.Empty;
        public string ToolVersion { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime LastHeartbeat { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string BuildId { get; set; } = string.Empty;
    }

    // API Request/Response models
    public class LicenseAcquireRequest
    {
        public string ToolName { get; set; } = string.Empty;
        public string ToolVersion { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string BuildId { get; set; } = string.Empty;
    }

    public class LicenseAcquireResponse
    {
        public bool LicenseGranted { get; set; }
        public string? SessionId { get; set; }
        public bool BurstMode { get; set; }
        public int BurstCountRemaining { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? Reason { get; set; }
        public bool BurstEventsExhausted { get; set; }
        public int RetryAfterSeconds { get; set; }
    }

    public class LicenseHeartbeatRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class LicenseHeartbeatResponse
    {
        public bool SessionValid { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    public class LicenseReleaseRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }

    public class LicenseReleaseResponse
    {
        public bool SessionReleased { get; set; }
        public int TotalMonthlyUsageSessions { get; set; }
    }

    public class LicenseStatusResponse
    {
        public int MaxConcurrent { get; set; }
        public int CurrentConcurrent { get; set; }
        public bool BurstModeAvailable { get; set; }
        public int MonthlyBurstsUsed { get; set; }
        public int MonthlyBurstsRemaining { get; set; }
        public List<string> LicensedTools { get; set; } = new List<string>();
    }

    // Configuration models
    public class EmbeddedLicenseConfig
    {
        public int MaxConcurrent { get; set; }
        public List<string> ToolsLicensed { get; set; } = new List<string>();
        public int BurstMultiplier { get; set; } = 2;
        public int BurstAllowancePerMonth { get; set; } = 2;
    }

    // Health check models
    public class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Version { get; set; } = string.Empty;
    }

    public class MetricsResponse
    {
        public int ConcurrentSessions { get; set; }
        public int MaxConcurrent { get; set; }
        public int BurstEventsUsed { get; set; }
        public int BurstEventsRemaining { get; set; }
        public int LicensedToolsCount { get; set; }
        public long UptimeSeconds { get; set; }
    }
}