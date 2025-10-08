# OPC UA to MQTT Bridge (OpcMqttBridge)

Connects to a beverage filling line OPC UA server and publishes process data to MQTT broker.

## Architecture

```
OPC UA Server (4840) → Bridge → MQTT Broker (1883)
   42 Variables         Reads     5 Topics
   Every 3s            Convert    JSON Format
```

The bridge reads 42 variables from the OPC UA server and organizes them into 5 logical MQTT topics.

## How It Works

### Variable to Topic Mapping

The bridge groups variables by function:

**1. Status Topic** (`v1/beverage/filling-line/status`)
- Machine identification: name, serial number, plant
- Current state: status, station, cleaning cycle

**2. Production Topic** (`v1/beverage/filling-line/production`)
- Order info: production order, article, quantity, lot number
- Counters: good bottles, bad bottles, progress percentage

**3. Process Topic** (`v1/beverage/filling-line/process`)
- All process values with target vs actual:
  - Fill volume, line speed, temperature
  - CO2 pressure, cap torque, cycle time
- Tank level percentage

**4. Quality Topic** (`v1/beverage/filling-line/quality`)
- Weight check result (Pass/Fail)
- Level check result (Pass/Fail)

**5. Alarms Topic** (`v1/beverage/filling-line/alarms`)
- Alarm count
- Array of active alarm messages

### Message Format

Each message includes a timestamp and relevant data:

```json
{
  "timestamp": "2025-10-01T11:29:19Z",
  "fill_volume": {
    "target": 1000.0,
    "actual": 999.22,
    "deviation": -0.78
  },
  "line_speed": {
    "target": 450.0,
    "actual": 450.57
  }
}
```

## Running the Bridge

```bash
dotnet restore
dotnet build
dotnet run
```

Expected output:
```
✓ Connected to OPC UA server
✓ Connected to MQTT broker
Bridge active. Publishing every 3 seconds...
[11:29:19] Published all data to MQTT
```

## Configuration

Edit `Program.cs` to change:
- OPC UA endpoint: `"opc.tcp://localhost:4840"`
- MQTT broker: `"localhost"`, port `1883`
- Update interval: `TimeSpan.FromSeconds(3)`
