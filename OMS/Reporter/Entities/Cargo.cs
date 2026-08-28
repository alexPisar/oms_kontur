using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class Cargo : Base.IReportEntity<Cargo>
    {
        public string Name { get; set; }
        public string PlaceCount { get; set; }
        public Enums.WeighingMethodEnum WeighingMethod { get; set; }
        public string Condition { get; set; }
        public decimal Volume { get; set; }
        public string ContainerType { get; set; }
        public Enums.PossibilityDistributionAlongPlatformEnum PossibilityDistributionAlongPlatform { get; set; }
        public Enums.CargoDivisibilityEnum CargoDivisibility { get; set; }
        public LogisticWeightType CargoPlacesWeight { get; set; }
        public CargoDimensions PlacesCargoDimensions { get; set; }
        public CargoDeliveryPoint[] DeliveryPoints { get; set; }
    }
}
