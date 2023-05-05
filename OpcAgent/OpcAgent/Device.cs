using Microsoft.Azure.Devices.Client;
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

        public async Task SendTelemetryMessage()
        {
            Console.WriteLine($"Device production status:");

        var data = new
            {
              wordOrderId = telemetryData.ElementAt(2).Value,
              productionStatus = telemetryData.ElementAt(0).Value,
              productionRate = telemetryData.ElementAt(1).Value,
              goodCount = telemetryData.ElementAt(4).Value,
             badCount = telemetryData.ElementAt(5).Value,
            temperature = telemetryData.ElementAt(3).Value,
        };

            var dataString = JsonConvert.SerializeObject(data);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";

            await deviceClient.SendEventAsync(eventMessage);
        }

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

        public async Task InitializeHandlers()
        {
            await deviceClient.SetMethodDefaultHandlerAsync(DefaulServiceHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("EmergencyStop", EmergencyStopHandler, deviceClient);
            await deviceClient.SetMethodHandlerAsync("ResetErrorStatus", ResetErrorStatusHandler, deviceClient);
        }

}
