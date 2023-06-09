﻿using Microsoft.Azure.Devices.Client;
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
        string monitoredDevice;
        private readonly OpcClient client;
        private string deviceName;
        public Device(DeviceClient deviceClient, string monitoredDevice, OpcClient client)
        {
            this.deviceClient = deviceClient;
            this.monitoredDevice = monitoredDevice;
            this.client = client;
            this.deviceName = monitoredDevice;
    }
    #region Messages
    public async Task SendTelemetryMessage(OpcReadNode[] telemetryData, int delayInMs)
        {
            Console.WriteLine($"Sending telemetry data to Iot Hub");
            while (true)
            {
            var job = client.ReadNodes(telemetryData);
            List<object> listOfNodes = new List<object>();
            foreach(var item in job)
            {
                listOfNodes.Add(item.Value);
            }
            var data = new
            {
                DeviceName = deviceName,
                ProductionStatus = listOfNodes[0],
                WorkorderId = listOfNodes[1],
                GoodCount = listOfNodes[2],
                BadCount = listOfNodes[3],
                Temperature = listOfNodes[4]
            };

            var dataString = JsonConvert.SerializeObject(data);

            Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
            eventMessage.ContentType = MediaTypeNames.Application.Json;
            eventMessage.ContentEncoding = "utf-8";
            eventMessage.Properties.Add("MessageType", "Telemetry");
            await deviceClient.SendEventAsync(eventMessage);
            await Task.Delay(delayInMs);
            }
        }
    public async Task SendD2CEventMessage()
    {
        Console.WriteLine($"An Event occured in device {monitoredDevice}\n");
        Console.WriteLine("\nSending Event message to IotHub\n");
        var deviceError = new OpcReadNode($"ns=2;s={monitoredDevice}/DeviceError");
        var data = new
        {
            DeviceName = deviceName,
            DeviceError = client.ReadNode(deviceError).Value
        };
        var dataString = JsonConvert.SerializeObject(data);

        Message eventMessage = new Message(Encoding.UTF8.GetBytes(dataString));
        eventMessage.ContentType = MediaTypeNames.Application.Json;
        eventMessage.ContentEncoding = "utf-8";
        eventMessage.Properties.Add("MessageType", "Event");
        

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
            var method = new OpcCallMethod($"ns=2;s={monitoredDevice}", $"ns=2;s={monitoredDevice}/EmergencyStop");
            client.CallMethod(method);
            throw new NotImplementedException();
        }
        private Task CallResetErrorStatus()
        {
            var method = new OpcCallMethod($"ns=2;s={monitoredDevice}", $"ns=2;s={monitoredDevice}/ResetErrorStatus");
            client.CallMethod(method);
            throw new NotImplementedException();
        }
    #endregion
    #region DeviceTwin
        public async Task UpdateTwinAsync()
        {
            var reportedProperties = new TwinCollection();
            var deviceError = new OpcReadNode($"ns=2;s={monitoredDevice}/DeviceError");
            var productionRate = new OpcReadNode($"ns=2;s={monitoredDevice}/ProductionRate");
            reportedProperties["DeviceError"] = client.ReadNode(deviceError).Value;
            reportedProperties["productionRate"] = client.ReadNode(productionRate).Value;

            await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);
        }
        
        public async Task OnDesiredPropertyChanged(TwinCollection desiredProperties, object _)
        {
            var twin = await deviceClient.GetTwinAsync();
            Int32 productionRate = twin.Properties.Desired["ProductionRate"];
            client.WriteNode($"ns=2;s={monitoredDevice}/ProductionRate", productionRate);
            await UpdateDesiredTwin();
        }

        private async Task UpdateDesiredTwin()
        {
            var reportedProperties = new TwinCollection();
            var productionRate = client.ReadNode($"ns=2;s={monitoredDevice}/ProductionRate");
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
