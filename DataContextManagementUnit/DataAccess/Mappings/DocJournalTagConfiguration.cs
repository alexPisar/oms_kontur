using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class DocJournalTagConfiguration : EntityTypeConfiguration<DocJournalTag>
    {
        public DocJournalTagConfiguration()
        {
            this
                .HasKey(p => new { p.IdDoc, p.IdTad })
                .ToTable("DOC_JOURNAL_TAGS", "ABT");

            this
                .Property(p => p.IdDoc)
                .HasColumnName(@"ID_DOC")
                .IsRequired();

            this
                .Property(p => p.IdTad)
                .HasColumnName(@"ID_TAG")
                .IsRequired();

            this
                .Property(p => p.TagValue)
                .HasColumnName(@"TAG_VALUE")
                .HasMaxLength(200);

            OnCreated();
        }

        partial void OnCreated();
    }
}
