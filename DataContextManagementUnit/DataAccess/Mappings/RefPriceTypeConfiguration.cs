﻿////------------------------------------------------------------------------------
//// This is auto-generated code.
////------------------------------------------------------------------------------
//// This code was generated by Devart Entity Developer tool using Entity Framework DbContext template.
//// Code is generated on: 04.08.2020 12:25:26
////
//// Changes to this file may cause incorrect behavior and will be lost if
//// the code is regenerated.
////------------------------------------------------------------------------------

//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Collections.Specialized;
//using System.Data.Entity.ModelConfiguration;

//namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
//{

//    public partial class RefPriceTypeConfiguration : EntityTypeConfiguration<RefPriceType>
//    {

//        public RefPriceTypeConfiguration()
//        {
//            this
//                .HasKey(p => p.Id)
//                .ToTable("REF_PRICE_TYPES", "ABT");
//            // Properties:
//            this
//                .Property(p => p.Id)
//                    .HasColumnName(@"ID")
//                    .IsRequired()
//                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
//            this
//                .Property(p => p.IdParentPriceType)
//                    .HasColumnName(@"ID_PARENT_PRICE_TYPE");
//            this
//                .Property(p => p.Name)
//                    .HasColumnName(@"NAME")
//                    .IsRequired()
//                    .HasMaxLength(128)
//                    .IsUnicode(false);
//            this
//                .Property(p => p.Coef)
//                    .HasColumnName(@"COEF")
//                    .IsRequired();
//            this
//                .Property(p => p.IdDefaultCurrency)
//                    .HasColumnName(@"ID_DEFAULT_CURRENCY")
//                    .IsRequired();
//            this
//                .Property(p => p.Oldid)
//                    .HasColumnName(@"OLDID");
//            // Association:
//            OnCreated();
//        }

//        partial void OnCreated();

//    }
//}
