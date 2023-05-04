using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Opc.UaFx;
using Opc.UaFx.Client;
using System.Net.Mime;
using System.Text;

public class Device
    {
        private readonly DeviceClient deviceClient;
        private IEnumerable<OpcValue> telemetryData;

        public Device(DeviceClient deviceClient, IEnumerable<OpcValue> telemetryData)
        {
            this.deviceClient = deviceClient;
            this.telemetryData = telemetryData;
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

        public async Task SendWorkOrderId()
        {

        }

        public async Task SendProductionRate()
        {

        }

        public async Task SendGoodCount()
        {

        }

        public async Task SendBadCount()
        {

        }

        public async Task SendTemperature()
        {

        }

        public async Task SendDeviceErrors()
        {

        }
    }
