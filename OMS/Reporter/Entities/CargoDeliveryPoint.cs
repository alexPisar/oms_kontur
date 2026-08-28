using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class CargoDeliveryPoint : Base.IReportEntity<CargoDeliveryPoint>
    {
        public string LoadingPoint { get; set; }
        public string UnloadingPoint { get; set; }
        public string PlaceCount { get; set; }
    }
}
