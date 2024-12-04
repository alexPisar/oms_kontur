using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class DocGoodsDetailsLabelsConfiguration : EntityTypeConfiguration<DocGoodsDetailsLabels>
    {
        public DocGoodsDetailsLabelsConfiguration()
        {
            this
                .HasKey(l => new { l.IdDoc, l.IdGood, l.DmLabel})
                .ToTable("DOC_GOODS_DETAILS_LABELS", "ABT");

            this
                .Property(l => l.IdDoc)
                .HasColumnName(@"ID_DOC")
                .IsRequired()
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);
            this
                .Property(l => l.IdGood)
                .HasColumnName(@"ID_GOOD")
                .IsRequired()
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            this
                .Property(l => l.DmLabel)
                .HasColumnName(@"DM_LABEL")
                .HasMaxLength(100);

            this
                .Property(l => l.InsertDateTime)
                .HasColumnName(@"INSERT_DATETIME");

            this
                .Property(l => l.IdDocSale)
                .HasColumnName(@"ID_DOC_SALE")
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            this
                .Property(l => l.IdDocReturn)
                .HasColumnName(@"ID_DOC_RETURN")
                .HasDatabaseGeneratedOption(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None);

            this
                .Property(l => l.SaleDmLabel)
                .HasColumnName(@"SALE_DM_LABEL")
                .HasMaxLength(100);

            this
                .Property(l => l.SaleDateTime)
                .HasColumnName(@"SALE_DATETIME");

            this
                .Property(l => l.ReturnDateTime)
                .HasColumnName(@"RETURN_DATETIME");

            OnCreated();
        }

        partial void OnCreated();
    }
}
