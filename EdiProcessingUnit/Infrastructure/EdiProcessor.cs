using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using UtilitesLibrary.ConfigSet;
using UtilitesLibrary.Logger;

namespace EdiProcessingUnit.Infrastructure
{
	abstract public class EdiProcessor
	{
		internal Edi.Edi _edi;
		internal EdiDbContext _ediDbContext;
		internal UtilityLog _log = UtilityLog.GetInstance();
		internal Config _conf = Config.GetInstance();
		internal string ProcessorName { get; set; }

		public EdiProcessor Init(Edi.Edi Edi, EdiDbContext EdiDbContext)
		{
			_edi = Edi;
			_ediDbContext = EdiDbContext;
			return this;
		}

		public EdiProcessor Init()
		{
			_edi = Edi.Edi.GetInstance();
			_ediDbContext = new EdiDbContext();
			return this;
		}

		abstract public void Run();


		/// <summary>
		/// Возвращает строку в формате даты по спецификации ISO 8601
		/// </summary>
		/// <param name="Date">input date</param>
		/// <returns>string 00.00.0000T00:00:00.000Z from date</returns>
		internal string GetDate(DateTime Date)
		{
			return Date.ToString( "yyyy-MM-ddTHH:mm:ssZ" );
		}

        /// <summary>
        /// Определяет, нужен ли данный документ в документообороте
        /// </summary>
        /// <param name="gln">ГЛН организации</param>
        protected virtual bool IsNeedProcessor(string gln)
        {
            return true;
        }

        // <summary>
        // Изменяет основной шлюз для подключения.
        // </summary>
        // <param name="newIpAddress">Новый IP адрес</param>
        //internal void ChangeGateway(string newIpAddress)
        //{
        //    var managementClass = new ManagementClass( "Win32_NetworkAdapterConfiguration" );
        //    var objMOC = managementClass.GetInstances();

        //    var hostname = Dns.GetHostName();
        //    var localIpAddress = Dns.GetHostAddresses( hostname )?.Where( i => !i.IsIPv6LinkLocal )?.FirstOrDefault()?.ToString();

        //    if (!string.IsNullOrEmpty( localIpAddress ))
        //        foreach (var objMO in objMOC)
        //        {
        //            var currentIpAddress = (string[])((ManagementObject)objMO)["IPAddress"];
        //            var gateWay = (string[])((ManagementObject)objMO)["DefaultIPGateway"];

        //            if ((currentIpAddress?.Any( c => c == localIpAddress ) ?? false))
        //            {
        //                var objNewGate = ((ManagementObject)objMO).GetMethodParameters( "SetGateways" );
        //                objNewGate["DefaultIPGateway"] = new string[] { newIpAddress };

        //                var objSetIP = ((ManagementObject)objMO).InvokeMethod( "SetGateways", objNewGate, null );
        //            }
        //        }
        //}
    }
}
