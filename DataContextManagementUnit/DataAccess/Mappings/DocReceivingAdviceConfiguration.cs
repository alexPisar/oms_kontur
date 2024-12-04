using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi.Mapping
{
    public partial class DocReceivingAdviceConfiguration : EntityTypeConfiguration<DocReceivingAdvice>
    {
        public DocReceivingAdviceConfiguration()
        {
            this
                .HasKey(r => new { r.MessageId, r.RecadvNumber })
                .ToTable("DOC_RECEIVING_ADVICES", "EDI");

            this
                .Property(r => r.MessageId)
                .HasColumnName(@"MESSAGE_ID")
                .IsRequired()
                .HasMaxLength(128)
                .IsUnicode(false);

            this
                .Property(r => r.IdOrder)
                .HasColumnName(@"ID_ORDER")
                .HasMaxLength(128)
                .IsUnicode(false);

            this
                .Property(r => r.RecadvNumber)
                .HasColumnName(@"RECADV_NUMBER")
                .IsRequired()
                .HasMaxLength(512)
                .IsUnicode(false);

            this
                .Property(r => r.RecadvDate)
                .HasColumnName(@"RECADV_DATE")
                .HasPrecision(0);

            this
                .Property(r => r.IdDocJournal)
                .HasColumnName(@"ID_DOC_JOURNAL");

            this
                .Property(r => r.TotalAmount)
                .HasColumnName(@"TOTAL_AMOUNT")
                .HasMaxLength(20)
                .IsUnicode(false);

            this
                .Property(r => r.TotalVatAmount)
                .HasColumnName(@"TOTAL_VAT_AMOUNT")
                .HasMaxLength(20)
                .IsUnicode(false);

            this
                .Property(r => r.TotalSumExcludeTax)
                .HasColumnName(@"TOTAL_SUM_EXCLUDE_TAX")
                .HasMaxLength(20)
                .IsUnicode(false);

            this
                .Property(r => r.TotalAcceptedQuantity)
                .HasColumnName(@"TOTAL_ACCEPTED_QUANTITY");

            OnCreated();
        }

        partial void OnCreated();
    }
}
