using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class DocEdoPurchasingDetailConfiguration : EntityTypeConfiguration<DocEdoPurchasingDetail>
    {
        public DocEdoPurchasingDetailConfiguration()
        {
            this
                .HasKey(d => new { d.IdDocEdoPurchasing, d.DetailNumber })
                .ToTable("DOC_EDO_PURCHASING_DETAILS", "EDI");

            this
                .Property(d => d.IdDocEdoPurchasing)
                .HasColumnName("ID_DOC_EDO_PURCHASING")
                .HasMaxLength(36);

            this
                .Property(d => d.BarCode)
                .HasColumnName("BAR_CODE")
                .HasMaxLength(20);

            this
                .Property(d => d.Description)
                .HasColumnName("DESCRIPTION")
                .HasMaxLength(2000);

            this
                .Property(d => d.Quantity)
                .HasColumnName("QUANTITY");

            this
                .Property(d => d.Price)
                .HasColumnName("PRICE");

            this
                .Property(d => d.Subtotal)
                .HasColumnName("SUBTOTAL");

            this
                .Property(d => d.TaxAmount)
                .HasColumnName("TAX_AMOUNT");

            this
                .Property(d => d.IdGood)
                .HasColumnName("ID_GOOD");

            this
                .Property(d => d.DetailNumber)
                .HasColumnName("DETAIL_NUMBER");

            this
                .Property(d => d.Gtin)
                .HasColumnName("GTIN")
                .HasMaxLength(20);

            this
                .Property(d => d.QuantityMark)
                .HasColumnName("QUANTITY_MARK");

            OnCreated();
        }

        partial void OnCreated();
    }
}
