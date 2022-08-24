using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;

namespace EdiProcessingUnit.Edi
{
	public static class EntityConverter
	{
		public static DocOrder XmlOrder_To_DbOrder(Order xmlOrder)
		{
			return new DocOrder();
		}
		
		public static RefCompany XmlCompany_To_DbCompany(Company xmlCompany)
		{
			var refcomp = new RefCompany() {
				Gln = xmlCompany?.gln,
				Name = xmlCompany?.organization?.name,
				Kpp = xmlCompany?.organization?.kpp,
				Inn = xmlCompany?.organization?.inn,
				City = xmlCompany?.russianAddress?.city,
				IdContractor = null,
				RegionCode = xmlCompany?.russianAddress?.regionISOCode,
				Street = xmlCompany?.russianAddress?.street,
				House = xmlCompany?.russianAddress?.house,
				Flat = xmlCompany?.russianAddress?.flat,
				PostalCode = xmlCompany?.russianAddress?.postalCode,
			};

			return refcomp;
		}

	
	}
}
