using Microsoft.Azure.Devices.Client;

namespace Device
{
    public class Device
    {
        public async Task SendProductionStatus(OpcReadNode prodStatus)
        {
            Console.WriteLine($"Device production status: {prodStatus}");
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
}