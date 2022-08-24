using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit.Edi.Model
{
	public class DeliveryInfoTransportation
	{
		public string transportMode { get; set; }
		public string typeOfTransportCode { get; set; }
		public string typeOfTransport { get; set; }
		public string vehicleNumber { get; set; }
		public string vehicleBrand { get; set; }
		public string nameOfCarrier { get; set; }
		public string vehicleArrivalDateTime { get; set; }
		public string startOfLoadingOnVehicleFromSupplier { get; set; }
		public string endOfLoadingOnVehicleFromSupplier { get; set; }
	}
}
