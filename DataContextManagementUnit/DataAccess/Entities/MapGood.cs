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
    public partial class MapGood
    {

        public MapGood()
        {
            OnCreated();
        }

        #region Properties

        public virtual string Id { get; set; }

        public virtual decimal? IdGood { get; set; }

        public virtual string BarCode { get; set; }

        public virtual string Name { get; set; }

        #endregion

        #region Navigation Properties
        public virtual List<MapGoodByBuyer> MapGoodByBuyers { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }

}
