using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using Org.BouncyCastle.Security;
using System.Net.Sockets;

string deviceConnectionString;
int delayInMs = 5000;

Console.WriteLine("Enter device connection string...");
deviceConnectionString = Console.ReadLine();

using var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Mqtt);
await deviceClient.OpenAsync();

using (OpcClient client = new OpcClient("opc.tcp://localhost:4840/"))
{
    client.Connect();
    Console.WriteLine("Connected to Opc server\n");

    var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
    Console.WriteLine("\nAvailable devices:\n");
    foreach (var deviceNode in node.Children())
    {   if (deviceNode.DisplayName.Value == "Server")
        { continue; }
        Console.WriteLine(deviceNode.DisplayName.Value);
    }
    Console.WriteLine("\nSelect device you want to monitor by entering the device number:\n");
    String monitoredDevice = Console.ReadLine();

    OpcReadNode[] telemetryData = new OpcReadNode[]
    {
        new OpcReadNode($"ns=2;s=Device {monitoredDevice}/WorkorderId"),
        new OpcReadNode($"ns=2;s=Device {monitoredDevice}/ProductionStatus"),
        new OpcReadNode($"ns=2;s=Device {monitoredDevice}/GoodCount"),
        new OpcReadNode($"ns=2;s=Device {monitoredDevice}/BadCount"),
        new OpcReadNode($"ns=2;s=Device {monitoredDevice}/Temperature"),
    };

    var device = new Device(deviceClient, monitoredDevice, client);

    await device.InitializeHandlers();
    await device.SendTelemetryMessage(telemetryData, delayInMs);

    async Task CallTwin(bool messageEvent)
    {
        await device.UpdateTwinAsync();
        if (messageEvent)
        {
            await device.SendD2CEventMessage();
        }
    }
    void DataChangeDeviceError(object sender, OpcDataChangeReceivedEventArgs e)
    {
        _ = CallTwin(true);
    }

    void DataChangeProductionRate(object sender, OpcDataChangeReceivedEventArgs e)
    {
        _ = CallTwin(false);
    }

    OpcSubscribeDataChange[] nodes = new OpcSubscribeDataChange[]
    {
        new OpcSubscribeDataChange($"ns=2;s=Device {monitoredDevice}/DeviceError",DataChangeDeviceError),
        new OpcSubscribeDataChange($"ns=2;s=Device {monitoredDevice}/ProductionRate",DataChangeProductionRate),
    };
    OpcSubscription subscription = client.SubscribeNodes(nodes);
    subscription.PublishingInterval = 1000; 
    subscription.ApplyChanges(); 

    Console.ReadLine();
}

