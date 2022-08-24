using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;
using SkbKontur.EdiApi.Client.Types.Organization;

namespace EdiProcessingUnit.WorkingUnits
{
	public class RelationsProcessor : EdiProcessor
	{

		public override void Run()
		{
			ProcessorName = "RelationsProcessor";
			
			ProcessParties();
		}

		private void ProcessParties()
		{
			var parties = _edi.GetParties();

            if (parties == null)
                return;

			foreach (var party in parties)
			{
				OrganizationCatalogueInfo OrganizationCatalogueInfo = _edi.GetOrganizationCatalogueInfo( party.Partner.PartnerId );

				SkbKontur.EdiApi.Client.Types.Organization.Organization[] DeliveryPoints = OrganizationCatalogueInfo.DeliveryPoints;
				
				foreach (var dpoint in DeliveryPoints)
				{
					// если в базе не нашлось совпадений по GLN для обрабатываемой точки доставки,
					// то пытаемся засунуть её в базу
					if (!_ediDbContext.RefCompanies.Any( point => point.Gln == dpoint.OrganizationInfo.Gln /*&& point.IsDeliveryPoint == "1"*/ ))
					{
						var newDeliveryPoint = ConvertCompany( dpoint/*, true */);
						_ediDbContext.RefCompanies.Add( newDeliveryPoint );
						_ediDbContext.SaveChanges();
					}
				}

				SkbKontur.EdiApi.Client.Types.Organization.Organization[] Organizations = OrganizationCatalogueInfo.Organizations;

				foreach (var organization in Organizations)
				{
					// если в базе не нашлось совпадений по GLN для обрабатываемой организации,
					// то пытаемся засунуть её в базу
					if (!_ediDbContext.RefCompanies.Any( org => org.Gln == organization.OrganizationInfo.Gln /*&& org.IsDeliveryPoint == "0"*/ ))
					{
						var newOrganization = ConvertCompany( organization);
						_ediDbContext.RefCompanies.Add( newOrganization );
						_ediDbContext.SaveChanges();
					}
				}
			}
		}


		/// <summary>
		/// Конвертирует сущность организации из модели контура в модель нашей базы данных
		/// </summary>
		/// <param name="organization">Организация</param>
		/// <returns></returns>
		private RefCompany ConvertCompany(SkbKontur.EdiApi.Client.Types.Organization.Organization organization/*, bool IsDeliveryPoint = false*/)
		{
			string name = "",
				kpp = "",
				inn = "",
				gln = "",
				city = "",
				region = "",
				street = "",
				house = "",
				flat = "",
				postal = "";

			if (organization.OrganizationInfo.RussianPartyInfo.ULInfo != null)
			{
				var partyInf = organization.OrganizationInfo.RussianPartyInfo.ULInfo;

				name = partyInf.Name;
				kpp = partyInf.Kpp;
				inn = partyInf.Inn;
			}
			else
			{
				var partyInf = organization.OrganizationInfo.RussianPartyInfo.IPInfo;

				name = $"ИП {partyInf.FirstName} {partyInf.MiddleName} {partyInf.LastName}";
				inn = partyInf.Inn;
			}

			if (organization.OrganizationInfo != null)
			{
				var orgInf = organization.OrganizationInfo;
				gln = orgInf.Gln;

				if (orgInf.PartyAddress.RussianAddressInfo != null)
				{
					var addrInf = orgInf.PartyAddress.RussianAddressInfo;

					city = addrInf.City;
					region = addrInf.RegionCode;
					street = addrInf.Street;
					house = addrInf.House;
					flat = addrInf.Flat;
					postal = addrInf.PostalCode;
				}
			}

			var refcomp = new RefCompany() {
				Gln = gln,
				Name = name,
				Kpp = kpp,
				Inn = inn,
				City = city,
				RegionCode = region,
				Street = street,
				House = house,
				Flat = flat,
				PostalCode = postal,
				LastSync = DateTime.Now,
				//IsDeliveryPoint = IsDeliveryPoint ? "1" : "0",
			};

			return refcomp;
		}

		public void AddNewCompany(RefCompany refComp)
		{
			if(!_ediDbContext.RefCompanies.Any(comp => comp.Gln == refComp.Gln ))
			{
				_ediDbContext.RefCompanies.Add( refComp );
				_ediDbContext.SaveChanges();
			}
		}

		public void AddNewCompanies(List<RefCompany> refCompList)
		{
			foreach (RefCompany refComp in refCompList)
				AddNewCompany( refComp );
		}

	}
}
