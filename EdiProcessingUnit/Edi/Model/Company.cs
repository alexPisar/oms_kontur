namespace EdiProcessingUnit.Edi.Model
{
	public class Company
	{
		public string gln { get; set; }
		public Organization organization { get; set; }
		public RussianAddress russianAddress { get; set; }
		public string additionalIdentificator { get; set; }
		public string taxSystem { get; set; }
		public string estimatedDeliveryDateTime { get; set; }
		public string shippingDateTime { get; set; }
		public Identificator waybill { get; set; }
	}
	
	public class Organization
	{
		public string name { get; set; }
		public string inn { get; set; }
		public string kpp { get; set; }
	}

	public class RussianAddress
	{
		public string regionISOCode { get; set; }
		public string city { get; set; }
		public string street { get; set; }
		public string house { get; set; }
		public string flat { get; set; }
		public string postalCode { get; set; }
	}





	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute( "code" )]
	[System.Xml.Serialization.XmlTypeAttribute( AnonymousType = true )]
	[System.Xml.Serialization.XmlRootAttribute( Namespace = "", IsNullable = false )]
	public partial class invoicee
	{
		public string gln{get;set;}
		public invoiceeSelfEmployed selfEmployed{get;set;}
		public invoiceeRussianAddress russianAddress{get;set;}
		public invoiceeAdditionalInfo additionalInfo{get;set;}
	}

	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute( "code" )]
	[System.Xml.Serialization.XmlTypeAttribute( AnonymousType = true )]
	public partial class invoiceeSelfEmployed
	{
		public invoiceeSelfEmployedFullName fullName{get;set;}
		public string inn{get;set;}
		public string soleProprietorRegistrationCertificate{get;set;}
	}

	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute( "code" )]
	[System.Xml.Serialization.XmlTypeAttribute( AnonymousType = true )]
	public partial class invoiceeSelfEmployedFullName
	{
		public string lastName{get;set;}
		public string firstName{get;set;}
		public string middleName{get;set;}
	}

	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute( "code" )]
	[System.Xml.Serialization.XmlTypeAttribute( AnonymousType = true )]
	public partial class invoiceeRussianAddress
	{
		public string regionISOCode{get;set;}
		public string district{get;set;}
		public string city{get;set;}
		public string settlement{get;set;}
		public string street{get;set;}
		public string house{get;set;}
		public string flat{get;set;}
		public string postalCode{get;set;}
	}

	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute( "code" )]
	[System.Xml.Serialization.XmlTypeAttribute( AnonymousType = true )]
	public partial class invoiceeAdditionalInfo
	{
		public string phone{get;set;}
		public string fax{get;set;}
		public string bankAccountNumber{get;set;}
		public string correspondentAccountNumber{get;set;}
		public string bankName{get;set;}
		public string BIK{get;set;}
		public string nameOfAccountant{get;set;}
	}

}
