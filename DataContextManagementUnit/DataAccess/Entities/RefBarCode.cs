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
    public partial class RefBarCode
    {

        public RefBarCode()
        {
            OnCreated();
        }

        #region Properties

        public virtual decimal? IdGood { get; set; }

        public virtual string BarCode { get; set; }

        public virtual bool? IsPrimary { get; set; }

        #endregion

        #region Navigation Properties

        public virtual RefGood Good { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
