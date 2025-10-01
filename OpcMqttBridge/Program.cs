namespace OpcMqttBridge
{
    class Program
    {
        private static OpcUaClient? _opcUaClient;
        private static MqttPublisher? _mqttPublisher;
        private static Timer? _publishTimer;

        static async Task Main(string[] args)
        {
            Console.WriteLine("=== OPC UA to MQTT Bridge Agent ===");
            Console.WriteLine("Connecting baverage filling lint to MQTT broker...\n");

            try
            {
                // Initialize OPC UA client
                _opcUaClient = new OpcUaClient("opc.tcp://localhost:4840", "OpcMqttBridge");
                await _opcUaClient.ConnectAsync();
                Console.WriteLine("Connected to OPC UA server");

                // Initialize MQTT publisher
                _mqttPublisher = new MqttPublisher("localhost", 1883, "beverage-filling-line-bridge");
                await _mqttPublisher.ConnectAsync();
                Console.WriteLine("Connected to MQTT broker\n");

                // Start periodic publishing
                _publishTimer = new Timer(async _ => await PublishAllDataAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));

                Console.WriteLine("Bridge active. Publishing every 3 seconds...");
                Console.WriteLine("Press Ctrl+C to stop.\n");

                // Keep running
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _publishTimer?.Dispose();
                await _mqttPublisher?.DisconnectAsync();
                await _opcUaClient?.DisconnectAsync();
            }
        }

        private static async Task PublishAllDataAsync()
        {
            try
            {
                var timestamp = DateTime.UtcNow;

                // Read all OPC UA variables
                var machineData = await _opcUaClient!.ReadAllVariablesAsync();

                // Publish to different MQTT topics
                await PublishMachineStatus(machineData, timestamp);
                await PublishProductionData(machineData, timestamp);
                await PublishProcessValues(machineData, timestamp);
                await PublishQualityData(machineData, timestamp);
                await PublishAlarms(machineData, timestamp);

                Console.WriteLine($"[{timestamp:HH:mm:ss}] Published all data to MQTT");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Publish error: {ex.Message}");
            }
        }

        private static async Task PublishMachineStatus(Dictionary<string, object> data, DateTime timestamp)
        {
            var payload = new
            {
                timestamp,
                machine_name = data.GetValueOrDefault("MachineName"),
                serial_number = data.GetValueOrDefault("MachineSerialNumber"),
                plant = data.GetValueOrDefault("Plant"),
                status = data.GetValueOrDefault("MachineStatus"),
                current_station = data.GetValueOrDefault("CurrentStation"),
                cleaning_status = data.GetValueOrDefault("CleaningCycleStatus")
            };

            await _mqttPublisher!.PublishAsync("beverage/filling-line/status", payload);
        }

        private static async Task PublishProductionData(Dictionary<string, object> data, DateTime timestamp)
        {
            var payload = new
            {
                timestamp,
                production_order = data.GetValueOrDefault("ProductionOrder"),
                article = data.GetValueOrDefault("Article"),
                quantity = data.GetValueOrDefault("Quantity"),
                lot_number = data.GetValueOrDefault("CurrentLotNumber"),
                expiration_date = data.GetValueOrDefault("ExpirationDate"),
                progress_percent = data.GetValueOrDefault("ProductionOrderProgress"),
                counters = new
                {
                    good_bottles = data.GetValueOrDefault("GoodBottles"),
                    bad_bottles_total = data.GetValueOrDefault("TotalBadBottles"),
                    bad_bottles_volume = data.GetValueOrDefault("BadBottlesVolume"),
                    bad_bottles_weight = data.GetValueOrDefault("BadBottlesWeight"),
                    bad_bottles_cap = data.GetValueOrDefault("BadBottlesCap"),
                    bad_bottles_other = data.GetValueOrDefault("BadBottlesOther"),
                    total_bottles = data.GetValueOrDefault("TotalBottles"),
                    good_bottles_order = data.GetValueOrDefault("GoodBottlesOrder"),
                    bad_bottles_order = data.GetValueOrDefault("BadBottlesOrder")
                }
            };

            await _mqttPublisher!.PublishAsync("beverage/filling-line/production", payload);
        }

        private static async Task PublishProcessValues(Dictionary<string, object> data, DateTime timestamp)
        {
            var payload = new
            {
                timestamp,
                fill_volume = new
                {
                    target = data.GetValueOrDefault("TargetFillVolume"),
                    actual = data.GetValueOrDefault("ActualFillVolume"),
                    deviation = data.GetValueOrDefault("FillAccuracyDeviation")
                },
                line_speed = new
                {
                    target = data.GetValueOrDefault("TargetLineSpeed"),
                    actual = data.GetValueOrDefault("ActualLineSpeed")
                },
                temperature = new
                {
                    target = data.GetValueOrDefault("TargetProductTemperature"),
                    actual = data.GetValueOrDefault("ActualProductTemperature")
                },
                co2_pressure = new
                {
                    target = data.GetValueOrDefault("TargetCO2Pressure"),
                    actual = data.GetValueOrDefault("ActualCO2Pressure")
                },
                cap_torque = new
                {
                    target = data.GetValueOrDefault("TargetCapTorque"),
                    actual = data.GetValueOrDefault("ActualCapTorque")
                },
                cycle_time = new
                {
                    target = data.GetValueOrDefault("TargetCycleTime"),
                    actual = data.GetValueOrDefault("ActualCycleTime")
                },
                tank_level_percent = data.GetValueOrDefault("ProductLevelTank")
            };

            await _mqttPublisher!.PublishAsync("beverage/filling-line/process", payload);
        }

        private static async Task PublishQualityData(Dictionary<string, object> data, DateTime timestamp)
        {
            var payload = new
            {
                timestamp,
                weight_check = data.GetValueOrDefault("QualityCheckWeight"),
                level_check = data.GetValueOrDefault("QualityCheckLevel"),
            };

            await _mqttPublisher!.PublishAsync("beverage/filling-line/quality", payload);
        }

        private static async Task PublishAlarms(Dictionary<string, object> data, DateTime timestamp)
        {
            var activeAlarms = data.GetValueOrDefault("ActiveAlarms") as string[] ?? Array.Empty<string>();


            var payload = new
            {
                timestamp,
                alarm_count = data.GetValueOrDefault("AlarmCount"),
                active_alarms = activeAlarms
            };

            await _mqttPublisher!.PublishAsync("beverage/filling-line/alarms", payload);
        }
    }
}
