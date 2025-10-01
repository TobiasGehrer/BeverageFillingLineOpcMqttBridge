using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace OpcMqttBridge
{
    public class OpcUaClient
    {
        private readonly string _endpointUrl;
        private readonly string _applicationName;
        private Session? _session;
        private readonly List<string> _nodeIds;

        public OpcUaClient(string endpointUrl, string applicationName)
        {
            _endpointUrl = endpointUrl;
            _applicationName = applicationName;
            _nodeIds = InitializeNodeIds();
        }

        private List<string> InitializeNodeIds()
        {           
            return new List<string>
            {
                // Machine Identification
                "ns=2;s=BeverageFillingLine.MachineName",
                "ns=2;s=BeverageFillingLine.MachineSerialNumber",
                "ns=2;s=BeverageFillingLine.Plant",
                "ns=2;s=BeverageFillingLine.ProductionSegment",
                "ns=2;s=BeverageFillingLine.ProductionLine",
            
                // Production Order
                "ns=2;s=BeverageFillingLine.ProductionOrder",
                "ns=2;s=BeverageFillingLine.Article",
                "ns=2;s=BeverageFillingLine.Quantity",
                "ns=2;s=BeverageFillingLine.CurrentLotNumber",
                "ns=2;s=BeverageFillingLine.ExpirationDate",
            
                // Target Values
                "ns=2;s=BeverageFillingLine.TargetFillVolume",
                "ns=2;s=BeverageFillingLine.TargetLineSpeed",
                "ns=2;s=BeverageFillingLine.TargetProductTemperature",
                "ns=2;s=BeverageFillingLine.TargetCO2Pressure",
                "ns=2;s=BeverageFillingLine.TargetCapTorque",
                "ns=2;s=BeverageFillingLine.TargetCycleTime",
            
                // Actual Values
                "ns=2;s=BeverageFillingLine.ActualFillVolume",
                "ns=2;s=BeverageFillingLine.ActualLineSpeed",
                "ns=2;s=BeverageFillingLine.ActualProductTemperature",
                "ns=2;s=BeverageFillingLine.ActualCO2Pressure",
                "ns=2;s=BeverageFillingLine.ActualCapTorque",
                "ns=2;s=BeverageFillingLine.ActualCycleTime",
                "ns=2;s=BeverageFillingLine.FillAccuracyDeviation",
            
                // System Status
                "ns=2;s=BeverageFillingLine.MachineStatus",
                "ns=2;s=BeverageFillingLine.CurrentStation",
                "ns=2;s=BeverageFillingLine.ProductLevelTank",
                "ns=2;s=BeverageFillingLine.CleaningCycleStatus",
                "ns=2;s=BeverageFillingLine.QualityCheckWeight",
                "ns=2;s=BeverageFillingLine.QualityCheckLevel",
            
                // Counters
                "ns=2;s=BeverageFillingLine.GoodBottles",
                "ns=2;s=BeverageFillingLine.BadBottlesVolume",
                "ns=2;s=BeverageFillingLine.BadBottlesWeight",
                "ns=2;s=BeverageFillingLine.BadBottlesCap",
                "ns=2;s=BeverageFillingLine.BadBottlesOther",
                "ns=2;s=BeverageFillingLine.TotalBadBottles",
                "ns=2;s=BeverageFillingLine.TotalBottles",
                "ns=2;s=BeverageFillingLine.GoodBottlesOrder",
                "ns=2;s=BeverageFillingLine.BadBottlesOrder",
                "ns=2;s=BeverageFillingLine.TotalBottlesOrder",
                "ns=2;s=BeverageFillingLine.ProductionOrderProgress",
            
                // Alarms
                "ns=2;s=BeverageFillingLine.ActiveAlarms",
                "ns=2;s=BeverageFillingLine.AlarmCount"
            };
        }

        public async Task ConnectAsync()
        {
            var application = new ApplicationInstance
            {
                ApplicationName = _applicationName,
                ApplicationType = ApplicationType.Client,
                ConfigSectionName = _applicationName
            };

            var config = await application.LoadApplicationConfiguration(false);
            config.ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:{_applicationName}";
            config.SecurityConfiguration.AutoAcceptUntrustedCertificates = true;

            await application.CheckApplicationInstanceCertificate(false, 0);

            var endpoint = CoreClientUtils.SelectEndpoint(_endpointUrl, false);
            var endpointConfiguration = EndpointConfiguration.Create(config);
            var configuredEndpoint = new ConfiguredEndpoint(null, endpoint, endpointConfiguration);

            _session = await Session.Create(
                config,
                configuredEndpoint,
                false,
                _applicationName,
                60000,
                new UserIdentity(new AnonymousIdentityToken()),
                null
            );        
        }

        public Task<Dictionary<string, object>> ReadAllVariablesAsync()
        {
            var result = new Dictionary<string, object>();

            foreach (var nodeId in _nodeIds)
            {
                try
                {
                    var value = _session!.ReadValue(nodeId);
                    var variableName = nodeId.Split('.').Last();
                    result[variableName] = value.Value ?? "null";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error reading {nodeId}: {ex.Message}");                   
                }
            }

            return result;
        }

        public async Task DisconnectAsync()
        {
            if (_session != null)
            {
                _session.Close();
                _session.Dispose();                
            }

            await Task.CompletedTask;
        }
        
    }
}
