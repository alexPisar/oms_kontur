using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public class RefCustomerConfiguration : EntityTypeConfiguration<RefCustomer>
    {
        public RefCustomerConfiguration()
        {
            this
                .HasKey(p => p.Id)
                .ToTable("REF_CUSTOMERS", "ABT");

            this
                .Property(p => p.Id)
                    .HasColumnName(@"ID")
                    .IsRequired();

            this
                .Property(p => p.Phones)
                    .HasColumnName(@"PHONES")
                    .HasMaxLength(50)
                    .IsUnicode(false);

            this
                .Property(p => p.Name)
                    .HasColumnName(@"NAME")
                    .HasMaxLength(128)
                    .IsUnicode(false);

            this
                .Property(p => p.Address)
                    .HasColumnName(@"JURIDICAL_ADDRESS")
                    .HasMaxLength(128)
                    .IsUnicode(false);

            this
                .Property(p => p.PostalAddress)
                    .HasColumnName(@"POSTAL_ADDRESS")
                    .HasMaxLength(128)
                    .IsUnicode(false);

            this
                .Property(p => p.Inn)
                    .HasColumnName(@"INN")
                    .HasMaxLength(16)
                    .IsUnicode(false);

            this
                .Property(p => p.Kpp)
                    .HasColumnName(@"KPP")
                    .HasMaxLength(16)
                    .IsUnicode(false);

            this
                .Property(p => p.Okpo)
                    .HasColumnName(@"OKPO")
                    .HasMaxLength(16)
                    .IsUnicode(false);

            this
                .Property(p => p.Okonh)
                    .HasColumnName(@"OKONH")
                    .HasMaxLength(64)
                    .IsUnicode(false);

            this
                .Property(p => p.IdContractor)
                    .HasColumnName(@"ID_CONTRACTOR");

            this
                .Property(p => p.IdCity)
                    .HasColumnName(@"ID_CITY");

            this
                .Property(p => p.Director)
                    .HasColumnName(@"DIRECTOR")
                    .HasMaxLength(64)
                    .IsUnicode(false);

            this
                .Property(p => p.AccountAnt)
                    .HasColumnName(@"ACCOUNTANT")
                    .HasMaxLength(64)
                    .IsUnicode(false);

            this
                .Property(p => p.Contact)
                    .HasColumnName(@"CONTACT")
                    .HasMaxLength(128)
                    .IsUnicode(false);

            this
                .HasOptional(p => p.Contractor)
                .WithMany()
                .HasForeignKey(p => p.IdContractor);
        }
    }
}
