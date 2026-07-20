using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefUserByEdoConsignorConfiguration : EntityTypeConfiguration<RefUserByEdoConsignor>
    {
        public RefUserByEdoConsignorConfiguration()
        {
            this
                .HasKey(r => new { r.UserName, r.IdCustomerConsignor })
                .ToTable("REF_USERS_BY_EDO_CONSIGNORS", "EDI");

            this
                .Property(r => r.IdCustomerConsignor)
                .HasColumnName("ID_CUSTOMER_CONSIGNOR");

            this
                .Property(r => r.UserName)
                .HasColumnName("USER_NAME")
                .HasMaxLength(100);

            OnCreated();
        }

        partial void OnCreated();
    }
}
