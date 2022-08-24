using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefUserByOrgEdoConfiguration : EntityTypeConfiguration<RefUserByOrgEdo>
    {
        public RefUserByOrgEdoConfiguration()
        {
            this
                .HasKey(r => new { r.IdCustomer, r.UserName })
                .ToTable("REF_USERS_BY_ORG_EDO", "EDI");

            this
                .Property(r => r.IdCustomer)
                .HasColumnName("ID_CUSTOMER");

            this
                .Property(r => r.UserName)
                .HasColumnName("USER_NAME")
                .HasMaxLength(100);

            OnCreated();
        }

        partial void OnCreated();
    }
}
