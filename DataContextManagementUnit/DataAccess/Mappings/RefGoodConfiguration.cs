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
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{

    public partial class RefGoodConfiguration : EntityTypeConfiguration<RefGood>
    {

        public RefGoodConfiguration()
        {
            this
                .HasKey(p => p.Id)
                .ToTable("REF_GOODS", "ABT");
            // Properties:
            this
                .Property(p => p.Id)
                    .HasColumnName(@"ID")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.SertNum)
                    .HasColumnName(@"SERT_NUM")
                    .HasMaxLength(100)
                    .IsUnicode(false);
            this
                .Property(p => p.RegNum)
                    .HasColumnName(@"REG_NUM")
                    .HasMaxLength(64)
                    .IsUnicode(false);
            this
                .Property(p => p.ExpiringDate)
                    .HasColumnName(@"EXPIRING_DATE")
                    .HasPrecision(0);
            this
                .Property(p => p.Code)
                    .HasColumnName(@"CODE")
                    .HasMaxLength(32)
                    .IsUnicode(false);
            this
                .Property(p => p.Name)
                    .HasColumnName(@"NAME")
                    .IsRequired()
                    .HasMaxLength(500)
                    .IsUnicode(false);
            this
                .Property(p => p.Tax)
                    .HasColumnName(@"TAX")
                    .IsRequired();
            this
                .Property(p => p.IdBaseItem)
                    .HasColumnName(@"ID_BASE_ITEM")
                    .IsRequired();
            this
                .Property(p => p.IdDefaultItem)
                    .HasColumnName(@"ID_DEFAULT_ITEM")
                    .IsRequired();
            this
                .Property(p => p.IdAccountCurrency)
                    .HasColumnName(@"ID_ACCOUNT_CURRENCY")
                    .IsRequired();
            this
                .Property(p => p.IdManufacturer)
                    .HasColumnName(@"ID_MANUFACTURER")
                    .IsRequired();
            this
                .Property(p => p.IdOrgan)
                    .HasColumnName(@"ID_ORGAN");
            this
                .Property(p => p.IdCountry)
                    .HasColumnName(@"ID_COUNTRY")
                    .IsRequired();
            this
                .Property(p => p.CustomsNo)
                    .HasColumnName(@"CUSTOMS_NO")
                    .HasMaxLength(32)
                    .IsUnicode(false);
            this
                .Property(p => p.IdSubdivision)
                    .HasColumnName(@"ID_SUBDIVISION")
                    .IsRequired();
            this
                .Property(p => p.HasRemain)
                    .HasColumnName(@"HAS_REMAIN")
                    .IsRequired();
            this
                .Property(p => p.Oldid)
                    .HasColumnName(@"OLDID");
            this
                .Property(p => p.GoodSize)
                    .HasColumnName(@"GOOD_SIZE")
                    .HasMaxLength(8)
                    .IsUnicode(false);
            this
                .Property(p => p.BarCode)
                    .HasColumnName(@"BAR_CODE");
            // Associations:
            this
                .HasMany(p => p.Item)
                    .WithOptional()
                .HasForeignKey(p => p.IdGood)
                    .WillCascadeOnDelete(false);
            this
                .HasRequired(p => p.DefaultItem)
                    .WithMany()
                .HasForeignKey(p => p.IdDefaultItem)
                    .WillCascadeOnDelete(false);
            this
                .HasRequired(p => p.BaseItem)
                    .WithMany()
                .HasForeignKey(p => p.IdBaseItem)
                    .WillCascadeOnDelete(false);
            this
                .HasRequired(p => p.Manufacturer)
                    .WithMany()
                .HasForeignKey(p => p.IdManufacturer)
                    .WillCascadeOnDelete(false);
            this
                .HasOptional(p => p.Contractor)
                    .WithMany()
                .HasForeignKey(p => p.IdOrgan)
                    .WillCascadeOnDelete(false);
			this
				.HasRequired( p => p.Country )
					.WithMany()
				.HasForeignKey( p => p.IdCountry )
					.WillCascadeOnDelete( false );
			OnCreated();
        }

        partial void OnCreated();

    }
}
