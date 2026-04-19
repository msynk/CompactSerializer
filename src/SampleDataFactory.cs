namespace SerializerDemo;

public static class SampleDataFactory
{
    public static SyncEnvelope CreateLargeSample()
    {
        var now = DateTime.UtcNow;
        var sequence = 490_112_774L;

        var tags = Enumerable.Range(1, 60)
            .Select(i => $"tag-{i:D3}")
            .ToList();

        var relayNodes = Enumerable.Range(1, 25)
            .Select(i => $"relay-{i:D2}")
            .ToList();

        var modules = Enumerable.Range(1, 45)
            .Select(i => new ModuleState
            {
                Name = $"module-{i:D2}",
                Enabled = i % 5 != 0,
                BuildNumber = 1_000 + i,
                ErrorCodes = Enumerable.Range(0, 8)
                    .Select(e => (i * 10 + e) % 32)
                    .ToList()
            })
            .ToList();

        var alerts = Enumerable.Range(1, 80)
            .Select(i => new DeviceAlert
            {
                Severity = i % 9 == 0 ? AlertSeverity.Critical : (i % 3 == 0 ? AlertSeverity.Warning : AlertSeverity.Info),
                Code = $"AL-{i:D4}",
                Message = $"Synthetic alert #{i} generated for benchmark payload.",
                RaisedAtUtc = now.AddSeconds(-i * 15)
            })
            .ToList();

        var statusHistory = Enumerable.Range(0, 120)
            .Select(i => new StatusSnapshot
            {
                TimestampUtc = now.AddMinutes(-(120 - i)),
                Mode = (i % 4) switch
                {
                    0 => DeviceMode.Standby,
                    1 => DeviceMode.Active,
                    2 => DeviceMode.Maintenance,
                    _ => DeviceMode.Active
                },
                HealthScore = 90 + (i % 10)
            })
            .ToList();

        var calibrationPoints = Enumerable.Range(0, 150)
            .Select(i => new CalibrationPoint
            {
                Input = i * 0.5,
                Output = i * 0.5 + 0.01,
                ErrorMargin = 0.001 + (i % 4) * 0.0005
            })
            .ToList();

        var pendingCommands = Enumerable.Range(1, 60)
            .Select(i => new DeviceCommand
            {
                Type = (i % 3) switch
                {
                    0 => CommandType.RefreshConfig,
                    1 => CommandType.SetMode,
                    _ => CommandType.Reboot
                },
                RequestedAtUtc = now.AddSeconds(-i * 20),
                Argument = i % 2 == 0 ? $"mode:{(i % 4)}" : $"profile:edge-{i % 7}",
                RetryCount = i % 4
            })
            .ToList();

        var checkpoints = Enumerable.Range(0, 200)
            .Select(i => new SyncCheckpoint
            {
                Sequence = sequence - (200 - i),
                ProcessedAtUtc = now.AddSeconds(-(200 - i) * 3),
                IsApplied = i % 11 != 0
            })
            .ToArray();

        var signature = Enumerable.Range(0, 256)
            .Select(i => (byte)((i * 37 + 11) % 256))
            .ToArray();

        return new SyncEnvelope
        {
            SequenceId = sequence,
            SentAtUtc = now,
            SourceNode = "node-east-1",
            TargetNode = "node-west-2",
            IsDelta = true,
            Priority = SyncPriority.High,
            CorrelationId = Guid.NewGuid(),
            Tags = tags,
            Route = new RouteInfo
            {
                Gateway = "gateway-12",
                HopCount = relayNodes.Count,
                RelayNodes = relayNodes
            },
            Payload = new DeviceState
            {
                DeviceId = Guid.NewGuid(),
                FirmwareVersion = "v5.8.14",
                Mode = DeviceMode.Active,
                Readings = new SensorReadings
                {
                    TemperatureCentiDegrees = 2_135,
                    PressurePa = 101_235,
                    HumidityPercent = 47.2,
                    BatteryVoltage = 3.712m
                },
                LastKnownLocation = new GeoPoint
                {
                    Latitude = 37.7749,
                    Longitude = -122.4194,
                    AltitudeMeters = 15.4
                },
                Modules = modules,
                Alerts = alerts,
                StatusHistory = statusHistory,
                Calibration = new CalibrationProfile
                {
                    Revision = 7,
                    AppliedAtUtc = now.AddDays(-3),
                    Points = calibrationPoints
                },
                LastMaintenanceUtc = now.AddDays(-14),
                Signature = signature
            },
            Audit = new AuditTrail
            {
                UpdatedBy = "sync-engine",
                UpdatedAtUtc = now.AddSeconds(-3),
                ChangeReason = "Large benchmark payload generation"
            },
            PendingCommands = pendingCommands,
            Checkpoints = checkpoints
        };
    }
}
