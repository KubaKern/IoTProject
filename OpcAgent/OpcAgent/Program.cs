using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using Org.BouncyCastle.Security;

string deviceConnectionString = "HostName=IoTZajecia2023.azure-devices.net;DeviceId=Device;SharedAccessKey=yogE0CsnCHhc99WJCxfHjYN9lmcugP1SpF4vQpAEKtM=";

    using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
    await deviceClient.OpenAsync();

using (OpcClient client = new OpcClient("opc.tcp://localhost:4840/"))
{
    client.Connect();
    Console.WriteLine("Connected to Opc server\n");

    OpcReadNode[] telemetry = new OpcReadNode[]
    {
        new OpcReadNode("ns=2;s=Device 1/ProductionStatus"),
        new OpcReadNode("ns=2;s=Device 1/ProductionRate"),
        new OpcReadNode("ns=2;s=Device 1/WorkorderId"),
        new OpcReadNode("ns=2;s=Device 1/Temperature"),
        new OpcReadNode("ns=2;s=Device 1/GoodCount"),
        new OpcReadNode("ns=2;s=Device 1/BadCount"),
    };
    IEnumerable<OpcValue> telemetryData = client.ReadNodes(telemetry);

    var device = new Device(deviceClient, telemetryData, client);

    await device.InitializeHandlers();
    await device.SendTelemetryMessage();

    Console.ReadLine();
}

