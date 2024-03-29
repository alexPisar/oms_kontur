﻿//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
// This code was generated by Devart Entity Developer tool using Entity Framework DbContext template.
// Code is generated on: 01.10.2020 15:53:46
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    /// <summary>
    /// Справочник товаров
    /// </summary>
    public partial class RefGood
    {

        public RefGood()
        {
            this.IdCountry = 116301m;
            this.IdSubdivision = 83901m;
            this.HasRemain = true;
            this.Item = new List<RefItem>();
            OnCreated();
        }

        #region Properties

        public virtual decimal Id { get; set; }

        public virtual string SertNum { get; set; }

        public virtual string RegNum { get; set; }

        public virtual global::System.DateTime? ExpiringDate { get; set; }

        public virtual string Code { get; set; }

        public virtual string Name { get; set; }

        public virtual int Tax { get; set; }

        public virtual decimal IdBaseItem { get; set; }

        public virtual decimal IdDefaultItem { get; set; }

        public virtual decimal IdAccountCurrency { get; set; }

        public virtual decimal IdManufacturer { get; set; }

        public virtual decimal? IdOrgan { get; set; }

        public virtual decimal IdCountry { get; set; }

        public virtual string CustomsNo { get; set; }

        public virtual decimal IdSubdivision { get; set; }

        public virtual bool HasRemain { get; set; }

        public virtual decimal? Oldid { get; set; }

        public virtual string GoodSize { get; set; }

        public virtual decimal? BarCode { get; set; }

        #endregion

        #region Navigation Properties

        public virtual List<RefItem> Item { get; set; }
        public virtual List<RefBarCode> BarCodes { get; set; }
        public virtual RefItem DefaultItem { get; set; }
        public virtual RefItem BaseItem { get; set; }
        public virtual RefContractor Manufacturer { get; set; }
        public virtual RefContractor Contractor { get; set; }
		public virtual RefCountry Country { get; set; }

		#endregion

		#region Extensibility Method Definitions

		partial void OnCreated();

        #endregion
    }

}
