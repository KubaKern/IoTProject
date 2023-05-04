using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.UaFx;
using Opc.UaFx.Client;
namespace OpcAgent
{
    internal class DeviceModel
    {
        OpcReadNode productionStatus;
        OpcReadNode productionRate;
        OpcReadNode workOrderId;
        OpcReadNode temperature;
        OpcReadNode goodCount;
        OpcReadNode badCount;
        OpcReadNode deviceError;

        public DeviceModel()
        {
            
        }
    }
}
