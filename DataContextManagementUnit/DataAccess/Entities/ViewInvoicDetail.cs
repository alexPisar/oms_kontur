﻿//------------------------------------------------------------------------------
// This is auto-generated code.
//------------------------------------------------------------------------------
// This code was generated by Devart Entity Developer tool using Entity Framework DbContext template.
// Code is generated on: 05.10.2020 13:35:37
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
//------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class ViewInvoicDetail
    {

        public ViewInvoicDetail()
        {
            OnCreated();
        }

        #region Properties

        public virtual decimal Id { get; set; }

        public virtual decimal? IdDocMaster { get; set; }

        public virtual string Code { get; set; }

        public virtual string IName { get; set; }

        public virtual int Inbox { get; set; }

        public virtual string GName { get; set; }

        public virtual string GoodSize { get; set; }

        public virtual int Quantity { get; set; }

        public virtual decimal? PriceWithoutTax { get; set; }

        public virtual decimal? SummWithoutTax { get; set; }

        public virtual string Acsiz { get; set; }

        public virtual int TaxRate { get; set; }

        public virtual decimal? TaxSumm { get; set; }

        public virtual decimal? Summ { get; set; }

        public virtual string CountryName { get; set; }

        public virtual string CountryNumCode { get; set; }

        public virtual string CustomsNum { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
