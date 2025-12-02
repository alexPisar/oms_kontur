using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefEdoCounteragentConfiguration : EntityTypeConfiguration<RefEdoCounteragent>
    {
        public RefEdoCounteragentConfiguration()
        {
            this
                .HasKey(c => new { c.IdCustomerSeller, c.IdCustomerBuyer })
                .ToTable("REF_EDO_COUNTERAGENTS", "EDI");

            this
                .Property(c => c.IdCustomerSeller)
                .HasColumnName(@"ID_CUSTOMER_SELLER")
                .IsRequired();

            this
                .Property(c => c.IdCustomerBuyer)
                .HasColumnName(@"ID_CUSTOMER_BUYER")
                .IsRequired();

            this
                .Property(c => c.IdFnsBuyer)
                .HasColumnName(@"ID_FNS_BUYER")
                .HasMaxLength(100);

            this
                .Property(c => c.ConnectStatus)
                .HasColumnName(@"CONNECT_STATUS");

            this
                .Property(c => c.InsertDatetime)
                .HasColumnName(@"INSERT_DATETIME");

            this
                .Property(c => c.InsertUser)
                .HasColumnName(@"INSERT_USER")
                .HasMaxLength(100);

            this
                .Property(c => c.IdFilial)
                .HasColumnName(@"ID_FILIAL");

            this
                .Property(c => c.IsConnected)
                .HasColumnName(@"IS_CONNECTED");

            OnCreated();
        }

        partial void OnCreated();
    }
}
