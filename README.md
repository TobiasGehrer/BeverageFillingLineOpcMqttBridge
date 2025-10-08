# OPC UA to MQTT Bridge (OpcMqttBridge)

Connects to a beverage filling line OPC UA server and publishes process data to MQTT broker using a Unified Namespace (UNS) architecture.

## Architecture

```
OPC UA Server (4840) → Bridge → MQTT Broker (1883)
   42 Variables         Reads     42 Topics (UNS)
   Every 3s            Convert    JSON Format
```

The bridge reads 42 variables from the OPC UA server and publishes each as an individual metric following UNS best practices.

## Unified Namespace (UNS) Structure

All metrics follow a consistent topic hierarchy:

```
v1/{enterprise}/{site}/{area}/{line}/{metric}
```

**Example Topics:**
- `v1/acme/vienna/production/filling-line-1/machine_status`
- `v1/acme/vienna/production/filling-line-1/process_fill_volume_actual`
- `v1/acme/vienna/production/filling-line-1/counters_good_bottles`

### Topic Organization

**Machine Information**
- `machine_name`, `machine_serial_number`, `machine_plant`
- `machine_status`, `machine_current_station`, `machine_cleaning_status`

**Production Data**
- `production_order`, `production_article`, `production_quantity`
- `production_lot_number`, `production_expiration_date`, `production_progress_percent`

**Production Counters**
- `counters_good_bottles`, `counters_bad_bottles_total`
- `counters_bad_bottles_volume`, `counters_bad_bottles_weight`
- `counters_bad_bottles_cap`, `counters_bad_bottles_other`
- `counters_total_bottles`, `counters_good_bottles_order`, `counters_bad_bottles_order`

**Process Values**
- Fill volume: `process_fill_volume_target`, `process_fill_volume_actual`, `process_fill_volume_deviation`
- Line speed: `process_line_speed_target`, `process_line_speed_actual`
- Temperature: `process_temperature_target`, `process_temperature_actual`
- CO2 pressure: `process_co2_pressure_target`, `process_co2_pressure_actual`
- Cap torque: `process_cap_torque_target`, `process_cap_torque_actual`
- Cycle time: `process_cycle_time_target`, `process_cycle_time_actual`
- Tank level: `process_tank_level_percent`

**Quality Checks**
- `quality_weight_check`, `quality_level_check`

**Alarms**
- `alarms_count`, `alarms_active`

## Message Format

Each metric publishes with a simple, consistent payload:

```json
{
  "timestamp": "2025-10-08T11:29:19Z",
  "value": 999.22
}
```

**Examples:**

Machine status:
```json
{
  "timestamp": "2025-10-08T11:29:19Z",
  "value": "Running"
}
```

Good bottles counter:
```json
{
  "timestamp": "2025-10-08T11:29:19Z",
  "value": 15847
}
```

Process temperature:
```json
{
  "timestamp": "2025-10-08T11:29:19Z",
  "value": 4.2
}
```

## MQTT Subscription Examples

Subscribe to all metrics from one line:
```
v1/acme/vienna/production/filling-line-1/#
```

Subscribe to all machine status across all lines:
```
v1/acme/vienna/production/+/machine_status
```

Subscribe to all actual process values:
```
v1/acme/vienna/production/filling-line-1/process_*_actual
```

Subscribe to all counter metrics:
```
v1/acme/vienna/production/filling-line-1/counters_#
```

## Running the Bridge

```bash
dotnet restore
dotnet build
dotnet run
```

Expected output:
```
=== OPC UA to MQTT Bridge Agent ===
Connecting beverage filling line to MQTT broker...

✓ Connected to OPC UA server
✓ Connected to MQTT broker

Bridge active. Publishing every 3 seconds...
UNS Base Topic: v1/acme/vienna/production/filling-line-1
Press Ctrl+C to stop.

[11:29:19] Published 42 metrics to MQTT
[11:29:22] Published 42 metrics to MQTT
```

## Configuration

Edit `Program.cs` to customize:

**Connection Settings:**
- OPC UA endpoint: `"opc.tcp://localhost:4840"`
- MQTT broker: `"localhost"`, port `1883`
- Update interval: `TimeSpan.FromSeconds(3)`

**UNS Hierarchy:**
```csharp
private const string Version = "v1";
private const string Enterprise = "best-beverages";
private const string Site = "dornbirn";          
private const string Area = "production";      
private const string Line = "filling-line-1";
```

## Benefits of UNS Architecture

- **Individual Metrics**: Each variable is independently accessible
- **Scalable**: Easy to add more lines, sites, or equipment
- **Flexible Subscriptions**: Use MQTT wildcards to subscribe to specific data sets
- **Version Control**: v1 prefix allows future API evolution
- **Consistent Depth**: All metrics at same topic level for predictable queries
- **ISA-95 Alignment**: Follows enterprise/site/area/line hierarchy
- **Time-Series Ready**: Perfect for InfluxDB, TimescaleDB, or other TSDB systems