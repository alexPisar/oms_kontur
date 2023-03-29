using System;
using System.Collections.Generic;
using DataContextManagementUnit.DataAccess.Contexts.Edi;

namespace OrderManagementSystem.UserInterface.ViewModels.Implementations
{
    public class MapGoodForExport
    {
        private MapGoodByBuyer _mapGoodByBuyer;

        public MapGoodForExport()
        {
            _mapGoodByBuyer = new MapGoodByBuyer();

            var mapGood = new MapGood();
            mapGood.Id = Guid.NewGuid().ToString();

            mapGood.MapGoodByBuyers = new List<MapGoodByBuyer>();
            mapGood.MapGoodByBuyers.Add(_mapGoodByBuyer);
            _mapGoodByBuyer.MapGood = mapGood;
            _mapGoodByBuyer.IdMapGood = mapGood.Id;
        }

        public decimal? IdGood
        {
            get => _mapGoodByBuyer?.MapGood?.IdGood;
            set => _mapGoodByBuyer.MapGood.IdGood = value;
        }

        public string BarCode
        {
            get => _mapGoodByBuyer?.MapGood?.BarCode;
            set => _mapGoodByBuyer.MapGood.BarCode = value;
        }

        public string Name
        {
            get => _mapGoodByBuyer?.MapGood?.Name;
            set => _mapGoodByBuyer.MapGood.Name = value;
        }

        public string BuyerCode
        {
            get => _mapGoodByBuyer?.BuyerCode;
            set => _mapGoodByBuyer.BuyerCode = value;
        }

        public void SetBuyerParameters(string gln)
        {
            _mapGoodByBuyer.Gln = gln;
        }

        public MapGood GetDbMapGood()
        {
            return _mapGoodByBuyer?.MapGood;
        }
    }
}
