using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Serialization;

namespace EdiProcessingUnit.UnifiedTransferDocument
{
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	[XmlRoot( Namespace = "", IsNullable = false )]
	public partial class Файл
	{
		public СвУчДокОбор СвУчДокОбор { get; set; }

		public Документ Документ { get; set; }
		
		[XmlAttribute()]
		public string Ид { get; set; }

		[XmlAttribute()]
		public decimal ВерсФорм { get; set; }

		[XmlAttribute()]
		public string ВерсПрог { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвУчДокОбор
	{
		public СвОЭДОтпр СвОЭДОтпр { get; set; }

		[XmlAttribute()]
		public string ИдОтпр { get; set; }

		[XmlAttribute()]
		public string ИдПол { get; set; }

	}


	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class Документ
	{
		public СвСчФакт СвСчФакт { get; set; }

		public ТаблСчФакт ТаблСчФакт { get; set; }

		public Пер Пер { get; set; }

		[XmlElement( "Подписант" )]
		public Подписант[] Подписант { get; set; }

		[XmlAttribute()]
		public string КНД { get; set; }

		[XmlAttribute()]
		public string Функция { get; set; }

		[XmlAttribute()]
		public string ПоФактХЖ { get; set; }

		[XmlAttribute()]
		public string НаимДокОпр { get; set; }

		[XmlAttribute()]
		public string ДатаИнфПр { get; set; }

		[XmlAttribute()]
		public string ВремИнфПр { get; set; }

		[XmlAttribute()]
		public string НаимЭконСубСост { get; set; }

		[XmlAttribute()]
		public string ОснДоверОргСост { get; set; }

		[XmlAttribute()]
		public string СоглСтрДопИнф { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвСчФакт
	{
		public ИспрСчФ ИспрСчФ { get; set; }

		[XmlElement( "" )]
		public СвОрг[] СвПрод { get; set; }

		[XmlElement( "ГрузОт" )]
		public ГрузОт[] ГрузОт { get; set; }

		[XmlElement( "ГрузПолуч" )]
		public СвОрг[] ГрузПолуч { get; set; }

		[XmlElement( "СвПРД" )]
		public СвПРД[] СвПРД { get; set; }

		[XmlElement( "СвПокуп" )]
		public СвОрг[] СвПокуп { get; set; }

		public ДопСвФХЖ1 ДопСвФХЖ1 { get; set; }

		[XmlElement( "ДокПодтвОтгр" )]
		public ДокПодтвОтгр[] ДокПодтвОтгр { get; set; }

		public ФХЖ ИнфПолФХЖ1 { get; set; }

		[XmlAttribute()]
		public string НомерСчФ { get; set; }

		[XmlAttribute()]
		public string ДатаСчФ { get; set; }

		[XmlAttribute()]
		public string КодОКВ { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ИспрСчФ
	{
		[XmlAttribute()]
		public string НомИспрСчФ { get; set; }

		[XmlAttribute()]
		public string ДефНомИспрСчФ { get; set; }

		[XmlAttribute()]
		public string ДатаИспрСчФ { get; set; }

		[XmlAttribute()]
		public string ДефДатаИспрСчФ { get; set; }

	}


	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class Адрес
	{
		public АдрРФ АдрРФ { get; set; }
		public АдрИнф АдрИнф { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class АдрИнф
	{
		[XmlAttribute()]
		public string КодСтр { get; set; }

		[XmlAttribute()]
		public string АдрТекст { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class АдрРФ
	{
		[XmlAttribute()]
		public string Индекс { get; set; }

		[XmlAttribute()]
		public string КодРегион { get; set; }

		[XmlAttribute()]
		public string Район { get; set; }

		[XmlAttribute()]
		public string Город { get; set; }

		[XmlAttribute()]
		public string НаселПункт { get; set; }

		[XmlAttribute()]
		public string Улица { get; set; }

		[XmlAttribute()]
		public string Дом { get; set; }

		[XmlAttribute()]
		public string Корпус { get; set; }

		[XmlAttribute()]
		public string Кварт { get; set; }

	}
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ГрузОт
	{
		public СвОрг ГрузОтпр { get; set; }

	}
	
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвПРД
	{
		[XmlAttribute()]
		public string НомерПРД { get; set; }

		[XmlAttribute()]
		public string ДатаПРД { get; set; }

		[XmlAttribute()]
		public string СуммаПРД { get; set; }

	}
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ДопСвФХЖ1
	{
		public ИнфПродГосЗакКазн ИнфПродГосЗакКазн { get; set; }

		public СвОрг СвФактор { get; set; }

		public ОснУстДенТреб ОснУстДенТреб { get; set; }

		[XmlAttribute()]
		public string ИдГосКон { get; set; }

		[XmlAttribute()]
		public string НаимОКВ { get; set; }

		[XmlAttribute()]
		public string КурсВал { get; set; }

		[XmlAttribute()]
		public string ОбстФормСЧФ { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ИнфПродГосЗакКазн
	{
		[XmlAttribute()]
		public string ДатаГосКонт { get; set; }

		[XmlAttribute()]
		public string НомерГосКонт { get; set; }

		[XmlAttribute()]
		public string ЛицСчетПрод { get; set; }

		[XmlAttribute()]
		public string КодПродБюджКласс { get; set; }

		[XmlAttribute()]
		public string КодЦелиПрод { get; set; }

		[XmlAttribute()]
		public string КодКазначПрод { get; set; }

		[XmlAttribute()]
		public string НаимКазначПрод { get; set; }

	}
	

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class БанкРекв
	{
		public СвБанк СвБанк { get; set; }

		[XmlAttribute()]
		public string НомерСчета { get; set; }

	}
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ОснУстДенТреб
	{
		[XmlAttribute()]
		public string НаимОсн { get; set; }

		[XmlAttribute()]
		public string НомОсн { get; set; }

		[XmlAttribute()]
		public string ДатаОсн { get; set; }

		[XmlAttribute()]
		public string ДопСвОсн { get; set; }

		[XmlAttribute()]
		public string ИдентОсн { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ДокПодтвОтгр
	{
		[XmlAttribute()]
		public string НаимДокОтгр { get; set; }

		[XmlAttribute()]
		public string НомДокОтгр { get; set; }

		[XmlAttribute()]
		public string ДатаДокОтгр { get; set; }

	}
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ТаблСчФакт
	{
		[XmlElement( "СведТов" )]
		public СведТов[] СведТов { get; set; }

		public ВсегоОпл ВсегоОпл { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СведТов
	{
		public Акциз Акциз { get; set; }

		public СумНалич СумНал { get; set; }

		[XmlElement( "СвТД" )]
		public СвТД[] СвТД { get; set; }

		public ДопСведТов ДопСведТов { get; set; }

		[XmlElement( "ИнфПолФХЖ2" )]
		public ТекстИнф[] ИнфПолФХЖ2 { get; set; }

		[XmlAttribute()]
		public string НомСтр { get; set; }

		[XmlAttribute()]
		public string НаимТов { get; set; }

		[XmlAttribute()]
		public string ОКЕИ_Тов { get; set; }

		[XmlAttribute()]
		public string ДефОКЕИ_Тов { get; set; }

		[XmlAttribute()]
		public string КолТов { get; set; }

		[XmlAttribute()]
		public string ЦенаТов { get; set; }

		[XmlAttribute()]
		public string СтТовБезНДС { get; set; }

		[XmlAttribute()]
		public string НалСт { get; set; }

		[XmlAttribute()]
		public string СтТовУчНал { get; set; }

		[XmlAttribute()]
		public string ДефСтТовУчНал { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class Акциз
	{
		public string СумАкциз { get; set; }
		public string БезАкциз { get; set; }
	}
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвТД
	{
		[XmlAttribute()]
		public string КодПроисх { get; set; }

		[XmlAttribute()]
		public string ДефКодПроисх { get; set; }

		[XmlAttribute()]
		public string НомерТД { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ДопСведТов
	{
		[XmlElement( "СведПрослеж" )]
		public СведПрослеж[] СведПрослеж { get; set; }

		[XmlElement( "НомСредИдентТов" )]
		public НомСредИдентТов[] НомСредИдентТов { get; set; }

		[XmlAttribute()]
		public string ПрТовРаб { get; set; }

		[XmlAttribute()]
		public string ДопПризн { get; set; }

		[XmlAttribute()]
		public string НаимЕдИзм { get; set; }

		[XmlAttribute()]
		public string КрНаимСтрПр { get; set; }

		[XmlAttribute()]
		public string НадлОтп { get; set; }

		[XmlAttribute()]
		public string ХарактерТов { get; set; }

		[XmlAttribute()]
		public string СортТов { get; set; }

		[XmlAttribute()]
		public string АртикулТов { get; set; }

		[XmlAttribute()]
		public string КодТов { get; set; }

		[XmlAttribute()]
		public string КодКат { get; set; }

		[XmlAttribute()]
		public string КодВидТов { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СведПрослеж
	{
		[XmlAttribute()]
		public string НомТовПрослеж { get; set; }

		[XmlAttribute()]
		public string ЕдИзмПрослеж { get; set; }

		[XmlAttribute()]
		public string НаимЕдИзмПрослеж { get; set; }

		[XmlAttribute()]
		public decimal КолВЕдПрослеж { get; set; }

		[XmlAttribute()]
		public string ДопПрослеж { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class НомСредИдентТов
	{
		[XmlElement( "КИЗ" )]
		public string[] КИЗ { get; set; }

		[XmlAttribute()]
		public string ИдентТрансУпак { get; set; }

	}
	
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ВсегоОпл
	{
		public СумНалич СумНалВсего { get; set; }

		public string КолНеттоВс { get; set; }

		[XmlAttribute()]
		public string СтТовБезНДСВсего { get; set; }

		[XmlAttribute()]
		public string СтТовУчНалВсего { get; set; }

		[XmlAttribute()]
		public string ДефСтТовУчНалВсего { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СумНалич
	{
		public string СумНал { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class Пер
	{
		public ПерСвПер СвПер { get; set; }

		public ФХЖ ИнфПолФХЖ3 { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ПерСвПер
	{
		[XmlElement( "ОснПер" )]
		public ОснПер[] ОснПер { get; set; }

		public СвЛицПер СвЛицПер { get; set; }

		public ТранГруз ТранГруз { get; set; }

		public СвПерВещи СвПерВещи { get; set; }

		[XmlAttribute()]
		public string СодОпер { get; set; }

		[XmlAttribute()]
		public string ВидОпер { get; set; }

		[XmlAttribute()]
		public string ДатаПер { get; set; }

		[XmlAttribute()]
		public string ДатаНач { get; set; }

		[XmlAttribute()]
		public string ДатаОкон { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ОснПер
	{
		[XmlAttribute()]
		public string НаимОсн { get; set; }

		[XmlAttribute()]
		public string НомОсн { get; set; }

		[XmlAttribute()]
		public string ДатаОсн { get; set; }

		[XmlAttribute()]
		public string ДопСвОсн { get; set; }

		[XmlAttribute()]
		public string ИдентОсн { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвЛицПер
	{
		public РабОргПрод РабОргПрод { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class РабОргПрод
	{
		public ФИО ФИО { get; set; }

		[XmlAttribute()]
		public string Должность { get; set; }

		[XmlAttribute()]
		public string ИныеСвед { get; set; }

		[XmlAttribute()]
		public string ОснПолн { get; set; }

	}
	

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ТранГруз
	{
		[XmlElement( "ТранНакл" )]
		public ТранНакл[] ТранНакл { get; set; }

		public СвОрг Перевозчик { get; set; }

		[XmlAttribute()]
		public string СвТранГруз { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ТранНакл
	{
		[XmlAttribute()]
		public string НомТранНакл { get; set; }

		[XmlAttribute()]
		public string ДатаТранНакл { get; set; }

	}

	[System.SerializableAttribute()]
	[System.ComponentModel.DesignerCategoryAttribute( "code" )]
	[System.Xml.Serialization.XmlTypeAttribute( AnonymousType = true )]
	[System.Xml.Serialization.XmlRootAttribute( Namespace = "", IsNullable = false )]
	public partial class ЮЛ
	{
		public ФИО ФИО { get; set; }

		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string ИННЮЛ { get; set; }

		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string Должн { get; set; }

		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string НаимОрг { get; set; }
	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвПерВещи
	{
		[XmlAttribute()]
		public string ДатаПерВещ { get; set; }

		[XmlAttribute()]
		public string СвПерВещ { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ФХЖ
	{
		[XmlElement( "ТекстИнф" )]
		public ТекстИнф[] ТекстИнф { get; set; }

		[XmlAttribute()]
		public string ИдИнфПол { get; set; }

	}


	

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class Подписант
	{
		public ФЛ ФЛ { get; set; }

		public ЮЛ ЮЛ { get; set; }

		[XmlAttribute()]
		public string ОблПолн { get; set; }

		[XmlAttribute()]
		public string Статус { get; set; }

		[XmlAttribute()]
		public string ОснПолн { get; set; }

		[XmlAttribute()]
		public string ОснПолнОрг { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ФЛ
	{
		public ФИО ФИО { get; set; }

		[XmlAttribute()]
		public string ГосРегИПВыдДов { get; set; }

		[XmlAttribute()]
		public string ИННФЛ { get; set; }

		[XmlAttribute()]
		public string ИныеСвед { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class Контакт
	{
		[XmlAttribute()]
		public string Тлф { get; set; }

		[XmlAttribute()]
		public string ЭлПочта { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ТекстИнф
	{
		[XmlAttribute()]
		public string Идентиф { get; set; }

		[XmlAttribute()]
		public string Значен { get; set; }

	}
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвБанк
	{
		[XmlAttribute()]
		public string НаимБанк { get; set; }

		[XmlAttribute()]
		public string БИК { get; set; }

		[XmlAttribute()]
		public string КорСчет { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ФИО
	{
		[XmlAttribute()]
		public string Фамилия { get; set; }

		[XmlAttribute()]
		public string Имя { get; set; }

		[XmlAttribute()]
		public string Отчество { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвОЭДОтпр
	{
		[XmlAttribute()]
		public string НаимОрг { get; set; }

		[XmlAttribute()]
		public string ИННЮЛ { get; set; }

		[XmlAttribute()]
		public string ИдЭДО { get; set; }

	}
	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвОрг
	{
		public ИдСв ИдСв { get; set; }

		public Адрес Адрес { get; set; }

		public Контакт Контакт { get; set; }

		public БанкРекв БанкРекв { get; set; }

		[XmlAttribute()]
		public string ОКПО { get; set; }

		[XmlAttribute()]
		public string СтруктПодр { get; set; }

		[XmlAttribute()]
		public string ИнфДляУчаст { get; set; }

		[XmlAttribute()]
		public string КраткНазв { get; set; }

	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class ИдСв
	{
		public СвИП СвИП { get; set; }
		public СвСвЮЛУч СвЮЛУч { get; set; }
	}		

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвСвЮЛУч
	{
		public string НаимОрг { get; set; }
		public string ИННЮЛ { get; set; }
		public string КПП { get; set; }
	}

	[Serializable()]
	[DesignerCategory( "code" )]
	[XmlType( AnonymousType = true )]
	public partial class СвИП
	{
		public ФИО ФИО { get; set; }

		[XmlAttribute()]
		public string ИННФЛ { get; set; }

		[XmlAttribute()]
		public string ДефИННФЛ { get; set; }

		[XmlAttribute()]
		public string СвГосРегИП { get; set; }

		[XmlAttribute()]
		public string ИныеСвед { get; set; }

	}


}
