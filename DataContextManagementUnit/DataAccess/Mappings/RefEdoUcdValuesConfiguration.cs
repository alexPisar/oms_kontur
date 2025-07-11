using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefEdoUcdValuesConfiguration : EntityTypeConfiguration<RefEdoUcdValues>
    {
        public RefEdoUcdValuesConfiguration()
        {
            this
                .HasKey(r => new { r.IdEdoGoodChannel, r.Key })
                .ToTable("REF_EDO_UCD_VALUES", "EDI");

            this
                .Property(r => r.IdEdoGoodChannel)
                .HasColumnName(@"ID_EDO_GOOD_CHANNEL")
                .IsRequired()
                .HasMaxLength(36);

            this
                .Property(r => r.Key)
                .HasColumnName(@"KEY")
                .IsRequired()
                .HasMaxLength(50);

            this
                .Property(r => r.Value)
                .HasColumnName(@"VALUE")
                .HasMaxLength(2000);

            this
                .Property(r => r.IdDocType)
                .HasColumnName(@"ID_DOC_TYPE")
                .IsRequired();

            OnCreated();
        }

        partial void OnCreated();
    }
}
