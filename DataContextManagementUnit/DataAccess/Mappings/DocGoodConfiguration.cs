using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{

    public partial class DocGoodConfiguration : EntityTypeConfiguration<DocGood>
    {

        public DocGoodConfiguration()
        {
            this
                .HasKey(p =>  p.IdDoc)
                .ToTable("DOC_GOODS", "ABT");
            // Properties:
            this
                .Property(p => p.IdDoc)
                    .HasColumnName(@"ID_DOC")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.IdAgent)
                    .HasColumnName(@"ID_AGENT");
            this
                .Property(p => p.IdCurrency)
                    .HasColumnName(@"ID_CURRENCY")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.CurrencyRate)
                    .HasColumnName(@"CURRENCY_RATE")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.IdPriceType)
                    .HasColumnName(@"ID_PRICE_TYPE")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.DiscountRate)
                    .HasColumnName(@"DISCOUNT_RATE");
            this
                .Property(p => p.DiscountSumm)
                    .HasColumnName(@"DISCOUNT_SUMM");
            this
                .Property(p => p.TotalSumm)
                    .HasColumnName(@"TOTAL_SUMM");
            this
                .Property(p => p.IdSeller)
                    .HasColumnName(@"ID_SELLER")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.IdCustomer)
                    .HasColumnName(@"ID_CUSTOMER")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.IdStoreSender)
                    .HasColumnName(@"ID_STORE_SENDER");
            this
                .Property(p => p.IdStoreReciepient)
                    .HasColumnName(@"ID_STORE_RECIEPIENT");
            this
                .Property(p => p.IdSubdivision)
                    .HasColumnName(@"ID_SUBDIVISION")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.IdDocReturn)
                    .HasColumnName(@"ID_DOC_RETURN");
            this
                .Property(p => p.ChargeRate)
                    .HasColumnName(@"CHARGE_RATE");
            this
                .Property(p => p.ChargeSumm)
                    .HasColumnName(@"CHARGE_SUMM");
            this
                .Property(p => p.IsReturn)
                    .HasColumnName(@"IS_RETURN")
                    .IsRequired()
                    .HasMaxLength(1)
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)
                    .IsFixedLength()
                    .IsUnicode(false);
            this
                .Property(p => p.LockStatus)
                    .HasColumnName(@"LOCK_STATUS")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.DocPrecision)
                    .HasColumnName(@"DOC_PRECISION")
                    .IsRequired()
                    .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(p => p.TotalPrime)
                    .HasColumnName(@"TOTAL_PRIME");
            // Associations:
            this
                .HasRequired(p => p.Seller)
                    .WithMany()
                .HasForeignKey(p => p.IdSeller)
                    .WillCascadeOnDelete(false);
            this
                .HasRequired(p => p.Customer)
                    .WithMany()
                .HasForeignKey(p => p.IdCustomer)
                    .WillCascadeOnDelete(false);
            OnCreated();
        }

        partial void OnCreated();

    }
}
