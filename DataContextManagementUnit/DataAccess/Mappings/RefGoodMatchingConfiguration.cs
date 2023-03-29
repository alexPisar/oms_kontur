using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefGoodMatchingConfiguration : EntityTypeConfiguration<RefGoodMatching>
    {
        public RefGoodMatchingConfiguration()
        {
            this
                .HasKey(r => r.Id)
                .ToTable("REF_GOODS_MATCHING", "ABT");

            this
                .Property(r => r.Id)
                .HasColumnName(@"ID")
                .IsRequired();

            this
                .Property(r => r.IdChannel)
                .HasColumnName(@"ID_CHANNEL");

            this
                .Property(r => r.CustomerArticle)
                .HasColumnName(@"CUSTOMER_ARTICLE")
                .HasMaxLength(50);

            this
                .Property(r => r.IdGood)
                .HasColumnName(@"ID_GOOD");

            this
                .Property(r => r.Disabled)
                .HasColumnName(@"DISABLED")
                .IsRequired();

            this
                .Property(r => r.DisabledDatetime)
                .HasColumnName(@"DISABLED_DATETIME");

            this
                .Property(r => r.InsertUser)
                .HasColumnName(@"INSERT_USER");

            this
                .Property(r => r.InsertDatetime)
                .HasColumnName(@"INSERT_DATETIME")
                .IsRequired();

            OnCreated();
        }

        partial void OnCreated();
    }
}
