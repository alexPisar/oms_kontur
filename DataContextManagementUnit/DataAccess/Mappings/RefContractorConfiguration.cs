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

    public partial class RefContractorConfiguration : EntityTypeConfiguration<RefContractor>
    {

        public RefContractorConfiguration()
        {
            this
                .HasKey(p => p.Id)
                .ToTable("REF_CONTRACTORS", "ABT");
            // Properties:
            this
                .Property(p => p.Id)
                    .HasColumnName(@"ID")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.Name)
                    .HasColumnName(@"NAME")
                    .HasMaxLength(128)
                    .IsUnicode(false);
            this
                .Property(p => p.IdDistrict)
                    .HasColumnName(@"ID_DISTRICT");
            this
                .Property(p => p.IdCity)
                    .HasColumnName(@"ID_CITY");
            this
                .Property(p => p.Comment)
                    .HasColumnName(@"COMMENTS")
                    .HasMaxLength(128)
                    .IsUnicode(false);
            this
                .Property(p => p.Address)
                    .HasColumnName(@"ADDRESS")
                    .HasMaxLength(128)
                    .IsUnicode(false);
            this
                .Property(p => p.Phone)
                    .HasColumnName(@"PHONES")
                    .HasMaxLength(64)
                    .IsUnicode(false);
            this
                .Property(p => p.Contact)
                    .HasColumnName(@"CONTACT")
                    .HasMaxLength(64)
                    .IsUnicode(false);
            this
                .Property(p => p.DefaultCustomer)
                    .HasColumnName(@"DEFAULT_CUSTOMER");
            this
                .Property(p => p.IdChannel)
                    .HasColumnName(@"ID_CHANNEL");
            // Associations:
            OnCreated();
        }

        partial void OnCreated();

    }
}
