using Opc.UaFx;
using Opc.UaFx.Client;
using Microsoft.Azure.Devices.Client;
using Org.BouncyCastle.Security;
using System.Net.Sockets;
using System.IO;

string deviceConnectionString;
// default app values
int delayInMs = 5000;
int publishingInterval = 1000;
string serverAddressFile = "opcServerAddress.txt";
string opcServerAddress = "opc.tcp://localhost:4840/";
string devicesAddressFile = "devicesAddress.txt";

IDictionary<string, string> devicesNames = new Dictionary<string, string>();

if (File.Exists(serverAddressFile))
{
    using (StreamReader file = new StreamReader(serverAddressFile))
    {
        opcServerAddress = file.ReadLine();
        file.Close();
    }
}
else
{
    Console.WriteLine("Missing opcServerAddress.txt file. Create new file according to the documentation.");
    Environment.Exit(0);
}

if (File.Exists(devicesAddressFile))
{
    using (StreamReader file = new StreamReader(devicesAddressFile))
    {
        string? line;
        string[] deviceNameAndAddress;
        while ((line = file.ReadLine()) != null)
        {
            deviceNameAndAddress = line.Split(',');
            devicesNames.Add(deviceNameAndAddress[0], deviceNameAndAddress[1]);
        }
        file.Close(); 
    }
}
else
{
    Console.WriteLine("Missing devicesAddress.txt file. Create new file according to the documentation.");
    Environment.Exit(0);
}



using (OpcClient client = new OpcClient(opcServerAddress))
{
    try
    {
        client.Connect();
        Console.WriteLine("Connected to Opc server\n");

        var node = client.BrowseNode(OpcObjectTypes.ObjectsFolder);
        Console.WriteLine("\nAvailable devices:\n");
        List<string> listOfDevices = new List<string>();
        foreach (var deviceNode in node.Children())
        {
            if (deviceNode.DisplayName.Value == "Server")
            {
                continue;
            }
            listOfDevices.Add(deviceNode.DisplayName.Value);
            Console.WriteLine(deviceNode.DisplayName.Value);
        }
        Console.WriteLine("\nSelect device you want to monitor by typing it's full name\n");

        String monitoredDevice = "";
        while (!listOfDevices.Contains(monitoredDevice))
        {
            monitoredDevice = Console.ReadLine();
            if (!listOfDevices.Contains(monitoredDevice))
                Console.WriteLine("Wrong device name. Select device again...");
        }

        Console.WriteLine("\nEnter delay (in miliseconds) in which messages will be send to IoTHub:\n");
        delayInMs = Convert.ToInt32(Console.ReadLine());
        Console.WriteLine("\nEnter value for publishing subscriptions in OpcServer (in miliseconds):\n");
        publishingInterval = Convert.ToInt32(Console.ReadLine());

        OpcReadNode[] telemetryData = new OpcReadNode[]
        {
        new OpcReadNode($"ns=2;s={monitoredDevice}/WorkorderId"),
        new OpcReadNode($"ns=2;s={monitoredDevice}/ProductionStatus"),
        new OpcReadNode($"ns=2;s={monitoredDevice}/GoodCount"),
        new OpcReadNode($"ns=2;s={monitoredDevice}/BadCount"),
        new OpcReadNode($"ns=2;s={monitoredDevice}/Temperature"),
        };

        using var deviceClient = DeviceClient.CreateFromConnectionString(devicesNames[monitoredDevice], TransportType.Mqtt);
        await deviceClient.OpenAsync();
        var device = new Device(deviceClient, monitoredDevice, client);
        await device.InitializeHandlers();

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
        new OpcSubscribeDataChange($"ns=2;s={monitoredDevice}/DeviceError",DataChangeDeviceError),
        new OpcSubscribeDataChange($"ns=2;s={monitoredDevice}/ProductionRate",DataChangeProductionRate),
        };
        OpcSubscription subscription = client.SubscribeNodes(nodes);
        subscription.PublishingInterval = publishingInterval;
        subscription.ApplyChanges();
        await device.SendTelemetryMessage(telemetryData, delayInMs);
        Console.ReadLine();
    }
    catch (Exception ex)
    {
        Console.WriteLine("\nConnection to server failed. Make sure your server is online. More details below:\n");
        Console.WriteLine(ex.ToString());
    }
}

