using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefEdoUpdValuesConfiguration : EntityTypeConfiguration<RefEdoUpdValues>
    {
        public RefEdoUpdValuesConfiguration()
        {
            this
                .HasKey(r => new { r.IdEdoGoodChannel, r.Key })
                .ToTable("REF_EDO_UPD_VALUES", "EDI");

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

            OnCreated();
        }

        partial void OnCreated();
    }
}
