using CompactBinarySerializer;

namespace CompactBinarySerializer.Demo;

public enum SyncPriority
{
    Low = 0,
    Normal = 1,
    High = 2
}

public enum DeviceMode
{
    Offline = 0,
    Standby = 1,
    Active = 2,
    Maintenance = 3
}

public enum AlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}

public enum CommandType
{
    Reboot = 0,
    RefreshConfig = 1,
    SetMode = 2
}

public sealed class SyncEnvelope
{
    [CompactIndex(0)]
    public long SequenceId { get; set; }

    [CompactIndex(1)]
    public DateTime SentAtUtc { get; set; }

    [CompactIndex(2)]
    public string SourceNode { get; set; } = string.Empty;

    [CompactIndex(3)]
    public string TargetNode { get; set; } = string.Empty;

    [CompactIndex(4)]
    public bool IsDelta { get; set; }

    [CompactIndex(5)]
    public SyncPriority Priority { get; set; }

    [CompactIndex(6)]
    public Guid CorrelationId { get; set; }

    [CompactIndex(7)]
    public List<string> Tags { get; set; } = [];

    [CompactIndex(8)]
    public RouteInfo Route { get; set; } = new();

    [CompactIndex(9)]
    public DeviceState Payload { get; set; } = new();

    [CompactIndex(10)]
    public AuditTrail? Audit { get; set; }

    [CompactIndex(11)]
    public List<DeviceCommand> PendingCommands { get; set; } = [];

    [CompactIndex(12)]
    public SyncCheckpoint[] Checkpoints { get; set; } = [];
}

public sealed class DeviceState
{
    [CompactIndex(0)]
    public Guid DeviceId { get; set; }

    [CompactIndex(1)]
    public string FirmwareVersion { get; set; } = string.Empty;

    [CompactIndex(2)]
    public DeviceMode Mode { get; set; }

    [CompactIndex(3)]
    public SensorReadings Readings { get; set; } = new();

    [CompactIndex(4)]
    public GeoPoint? LastKnownLocation { get; set; }

    [CompactIndex(5)]
    public List<ModuleState> Modules { get; set; } = [];

    [CompactIndex(6)]
    public List<DeviceAlert> Alerts { get; set; } = [];

    [CompactIndex(7)]
    public List<StatusSnapshot> StatusHistory { get; set; } = [];

    [CompactIndex(8)]
    public CalibrationProfile Calibration { get; set; } = new();

    [CompactIndex(9)]
    public DateTime? LastMaintenanceUtc { get; set; }

    [CompactIndex(10)]
    public byte[] Signature { get; set; } = [];
}

public sealed class RouteInfo
{
    [CompactIndex(0)]
    public string Gateway { get; set; } = string.Empty;

    [CompactIndex(1)]
    public int HopCount { get; set; }

    [CompactIndex(2)]
    public List<string> RelayNodes { get; set; } = [];
}

public sealed class AuditTrail
{
    [CompactIndex(0)]
    public string UpdatedBy { get; set; } = string.Empty;

    [CompactIndex(1)]
    public DateTime UpdatedAtUtc { get; set; }

    [CompactIndex(2)]
    public string? ChangeReason { get; set; }
}

public sealed class DeviceCommand
{
    [CompactIndex(0)]
    public CommandType Type { get; set; }

    [CompactIndex(1)]
    public DateTime RequestedAtUtc { get; set; }

    [CompactIndex(2)]
    public string? Argument { get; set; }

    [CompactIndex(3)]
    public int RetryCount { get; set; }
}

public sealed class SyncCheckpoint
{
    [CompactIndex(0)]
    public long Sequence { get; set; }

    [CompactIndex(1)]
    public DateTime ProcessedAtUtc { get; set; }

    [CompactIndex(2)]
    public bool IsApplied { get; set; }
}

public sealed class SensorReadings
{
    [CompactIndex(0)]
    public int TemperatureCentiDegrees { get; set; }

    [CompactIndex(1)]
    public int PressurePa { get; set; }

    [CompactIndex(2)]
    public double HumidityPercent { get; set; }

    [CompactIndex(3)]
    public decimal BatteryVoltage { get; set; }
}

public sealed class GeoPoint
{
    [CompactIndex(0)]
    public double Latitude { get; set; }

    [CompactIndex(1)]
    public double Longitude { get; set; }

    [CompactIndex(2)]
    public double? AltitudeMeters { get; set; }
}

public sealed class ModuleState
{
    [CompactIndex(0)]
    public string Name { get; set; } = string.Empty;

    [CompactIndex(1)]
    public bool Enabled { get; set; }

    [CompactIndex(2)]
    public int BuildNumber { get; set; }

    [CompactIndex(3)]
    public List<int> ErrorCodes { get; set; } = [];
}

public sealed class DeviceAlert
{
    [CompactIndex(0)]
    public AlertSeverity Severity { get; set; }

    [CompactIndex(1)]
    public string Code { get; set; } = string.Empty;

    [CompactIndex(2)]
    public string Message { get; set; } = string.Empty;

    [CompactIndex(3)]
    public DateTime RaisedAtUtc { get; set; }
}

public sealed class StatusSnapshot
{
    [CompactIndex(0)]
    public DateTime TimestampUtc { get; set; }

    [CompactIndex(1)]
    public DeviceMode Mode { get; set; }

    [CompactIndex(2)]
    public int HealthScore { get; set; }
}

public sealed class CalibrationProfile
{
    [CompactIndex(0)]
    public int Revision { get; set; }

    [CompactIndex(1)]
    public DateTime AppliedAtUtc { get; set; }

    [CompactIndex(2)]
    public List<CalibrationPoint> Points { get; set; } = [];
}

public sealed class CalibrationPoint
{
    [CompactIndex(0)]
    public double Input { get; set; }

    [CompactIndex(1)]
    public double Output { get; set; }

    [CompactIndex(2)]
    public double ErrorMargin { get; set; }
}
