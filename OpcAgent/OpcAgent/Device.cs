using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;
using Org.BouncyCastle.Security;
using System.Net.Mime;
using System.Text;

public class Device
    {
        private readonly DeviceClient deviceClient;
        private IEnumerable<OpcValue> telemetryData;
        private readonly OpcClient client;

        public Device(DeviceClient deviceClient, IEnumerable<OpcValue> telemetryData, OpcClient client)
        {
            this.deviceClient = deviceClient;
            this.telemetryData = telemetryData;
            this.client = client;
        }
    #region Messages
    public async Task SendTelemetryMessage()
        {
            Console.WriteLine($"Device production status:");

        var data = new
            {
                wordOrderId = telemetryData.ElementAt(0).Value,
                productionStatus = telemetryData.ElementAt(1).Value,
                goodCount = telemetryData.ElementAt(2).Value,
                badCount = telemetryData.ElementAt(3).Value,
                temperature = telemetryData.ElementAt(4).Value,
            };

            var dataString = JsonConvert.SerializeObject(data);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";

            await deviceClient.SendEventAsync(eventMessage);
        }
    public async Task SendD2CEventMessage()
    {

        var deviceError = new OpcReadNode("ns=2;s=Device 1/DeviceError");
        var data = new
        {
            DeviceError = client.ReadNode(deviceError).Value
        };
        var dataString = JsonConvert.SerializeObject(data);

        Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
        eventMessage.ContentType = MediaTypeNames.Application.Json;
        eventMessage.ContentEncoding = "utf-8";

        Console.WriteLine($"An error occured in device 1\n");

        await deviceClient.SendEventAsync(eventMessage);
    }
    #endregion
    #region DirectMethods
    private async Task<MethodResponse> EmergencyStopHandler(MethodRequest methodRequest, object userContext)
        {
        Console.WriteLine($"METHOD EXECUTED: {methodRequest.Name}");

        await CallEmergencyStop();
        return new MethodResponse(0);
        }
        private async Task<MethodResponse> ResetErrorStatusHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"METHOD EXECUTED: {methodRequest.Name}");

            await CallResetErrorStatus();
            return new MethodResponse(0);
        }
        private async Task<MethodResponse> DefaulServiceHandler(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"SELECTED METHOD IS UNDEFINED: {methodRequest.Name}");

            await Task.Delay(1000);
            return new MethodResponse(0);
        }
        private Task CallEmergencyStop()
        {
            var method = new OpcCallMethod("ns=2;s=Device 1", "ns=2;s=Device 1/EmergencyStop");
            client.CallMethod(method);
            throw new NotImplementedException();
        }
        private Task CallResetErrorStatus()
        {
            var method = new OpcCallMethod("ns=2;s=Device 1", "ns=2;s=Device 1/ResetErrorStatus");
            client.CallMethod(method);
            throw new NotImplementedException();
        }
    #endregion
    #region DeviceTwin
        public async Task UpdateTwinAsync()
        {
            var reportedProperties = new TwinCollection();
            var deviceError = new OpcReadNode("ns=2;s=Device 1/DeviceError");
            var productionRate = new OpcReadNode("ns=2;s=Device 1/ProductionRate");
            reportedProperties["DeviceError"] = client.ReadNode(deviceError).Value;
            reportedProperties["productionRate"] = client.ReadNode(productionRate).Value;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
        
        public async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object _)
        {
            var twin = await deviceClient.GetTwinAsync();
            Int32 productionRate = twin.Properties.Desired["ProductionRate"];
            client.WriteNode("ns=2;s=Device 1/ProductionRate", productionRate);
            await UpdateDesiredTwin();
        }

        private async Task UpdateDesiredTwin()
        {
            var reportedProperties = new TwinCollection();
            var productionRate = client.ReadNode("ns=2;s=Device 1/ProductionRate");
            reportedProperties["ProductionRate"] = productionRate.Value;
            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
    #endregion
    public async Task InitializeHandlers()
        {
            await deviceClient.SetMethodDefaultHandlerAsync(DefaulServiceHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, deviceClient);
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChanged, deviceClient);
        }

}
