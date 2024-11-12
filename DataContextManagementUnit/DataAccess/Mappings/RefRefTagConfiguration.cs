using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefRefTagConfiguration : EntityTypeConfiguration<RefRefTag>
    {
        public RefRefTagConfiguration()
        {
            this
                .HasKey(r => new { r.IdTag, r.IdObject })
                .ToTable("REF_REF_TAGS", "ABT");

            this
                .Property(r => r.IdTag)
                .HasColumnName(@"ID_TAG")
                .IsRequired();

            this
                .Property(r => r.IdObject)
                .HasColumnName(@"ID_OBJECT")
                .IsRequired();

            this
                .Property(r => r.TagValue)
                .HasColumnName(@"TAG_VALUE")
                .HasMaxLength(500);

            OnCreated();
        }

        partial void OnCreated();
    }
}
