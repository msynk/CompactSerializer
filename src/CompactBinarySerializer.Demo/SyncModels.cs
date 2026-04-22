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
    [SyncOrder(0)]
    public long SequenceId { get; set; }

    [SyncOrder(1)]
    public DateTime SentAtUtc { get; set; }

    [SyncOrder(2)]
    public string SourceNode { get; set; } = string.Empty;

    [SyncOrder(3)]
    public string TargetNode { get; set; } = string.Empty;

    [SyncOrder(4)]
    public bool IsDelta { get; set; }

    [SyncOrder(5)]
    public SyncPriority Priority { get; set; }

    [SyncOrder(6)]
    public Guid CorrelationId { get; set; }

    [SyncOrder(7)]
    public List<string> Tags { get; set; } = [];

    [SyncOrder(8)]
    public RouteInfo Route { get; set; } = new();

    [SyncOrder(9)]
    public DeviceState Payload { get; set; } = new();

    [SyncOrder(10)]
    public AuditTrail? Audit { get; set; }

    [SyncOrder(11)]
    public List<DeviceCommand> PendingCommands { get; set; } = [];

    [SyncOrder(12)]
    public SyncCheckpoint[] Checkpoints { get; set; } = [];
}

public sealed class DeviceState
{
    [SyncOrder(0)]
    public Guid DeviceId { get; set; }

    [SyncOrder(1)]
    public string FirmwareVersion { get; set; } = string.Empty;

    [SyncOrder(2)]
    public DeviceMode Mode { get; set; }

    [SyncOrder(3)]
    public SensorReadings Readings { get; set; } = new();

    [SyncOrder(4)]
    public GeoPoint? LastKnownLocation { get; set; }

    [SyncOrder(5)]
    public List<ModuleState> Modules { get; set; } = [];

    [SyncOrder(6)]
    public List<DeviceAlert> Alerts { get; set; } = [];

    [SyncOrder(7)]
    public List<StatusSnapshot> StatusHistory { get; set; } = [];

    [SyncOrder(8)]
    public CalibrationProfile Calibration { get; set; } = new();

    [SyncOrder(9)]
    public DateTime? LastMaintenanceUtc { get; set; }

    [SyncOrder(10)]
    public byte[] Signature { get; set; } = [];
}

public sealed class RouteInfo
{
    [SyncOrder(0)]
    public string Gateway { get; set; } = string.Empty;

    [SyncOrder(1)]
    public int HopCount { get; set; }

    [SyncOrder(2)]
    public List<string> RelayNodes { get; set; } = [];
}

public sealed class AuditTrail
{
    [SyncOrder(0)]
    public string UpdatedBy { get; set; } = string.Empty;

    [SyncOrder(1)]
    public DateTime UpdatedAtUtc { get; set; }

    [SyncOrder(2)]
    public string? ChangeReason { get; set; }
}

public sealed class DeviceCommand
{
    [SyncOrder(0)]
    public CommandType Type { get; set; }

    [SyncOrder(1)]
    public DateTime RequestedAtUtc { get; set; }

    [SyncOrder(2)]
    public string? Argument { get; set; }

    [SyncOrder(3)]
    public int RetryCount { get; set; }
}

public sealed class SyncCheckpoint
{
    [SyncOrder(0)]
    public long Sequence { get; set; }

    [SyncOrder(1)]
    public DateTime ProcessedAtUtc { get; set; }

    [SyncOrder(2)]
    public bool IsApplied { get; set; }
}

public sealed class SensorReadings
{
    [SyncOrder(0)]
    public int TemperatureCentiDegrees { get; set; }

    [SyncOrder(1)]
    public int PressurePa { get; set; }

    [SyncOrder(2)]
    public double HumidityPercent { get; set; }

    [SyncOrder(3)]
    public decimal BatteryVoltage { get; set; }
}

public sealed class GeoPoint
{
    [SyncOrder(0)]
    public double Latitude { get; set; }

    [SyncOrder(1)]
    public double Longitude { get; set; }

    [SyncOrder(2)]
    public double? AltitudeMeters { get; set; }
}

public sealed class ModuleState
{
    [SyncOrder(0)]
    public string Name { get; set; } = string.Empty;

    [SyncOrder(1)]
    public bool Enabled { get; set; }

    [SyncOrder(2)]
    public int BuildNumber { get; set; }

    [SyncOrder(3)]
    public List<int> ErrorCodes { get; set; } = [];
}

public sealed class DeviceAlert
{
    [SyncOrder(0)]
    public AlertSeverity Severity { get; set; }

    [SyncOrder(1)]
    public string Code { get; set; } = string.Empty;

    [SyncOrder(2)]
    public string Message { get; set; } = string.Empty;

    [SyncOrder(3)]
    public DateTime RaisedAtUtc { get; set; }
}

public sealed class StatusSnapshot
{
    [SyncOrder(0)]
    public DateTime TimestampUtc { get; set; }

    [SyncOrder(1)]
    public DeviceMode Mode { get; set; }

    [SyncOrder(2)]
    public int HealthScore { get; set; }
}

public sealed class CalibrationProfile
{
    [SyncOrder(0)]
    public int Revision { get; set; }

    [SyncOrder(1)]
    public DateTime AppliedAtUtc { get; set; }

    [SyncOrder(2)]
    public List<CalibrationPoint> Points { get; set; } = [];
}

public sealed class CalibrationPoint
{
    [SyncOrder(0)]
    public double Input { get; set; }

    [SyncOrder(1)]
    public double Output { get; set; }

    [SyncOrder(2)]
    public double ErrorMargin { get; set; }
}
