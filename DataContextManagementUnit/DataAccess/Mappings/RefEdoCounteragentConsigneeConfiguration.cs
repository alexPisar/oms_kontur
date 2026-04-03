using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefEdoCounteragentConsigneeConfiguration : EntityTypeConfiguration<RefEdoCounteragentConsignee>
    {
        public RefEdoCounteragentConsigneeConfiguration()
        {
            this
                .HasKey(r => new { r.IdCustomerSeller, r.IdCustomerBuyer, r.IdFnsBuyer, r.IdContractorConsignee })
                .ToTable("REF_EDO_COUNTERAGENT_CONSIGNEE", "EDI");

            this
                .Property(r => r.IdCustomerSeller)
                .HasColumnName(@"ID_CUSTOMER_SELLER")
                .IsRequired();

            this
                .Property(r => r.IdCustomerBuyer)
                .HasColumnName(@"ID_CUSTOMER_BUYER")
                .IsRequired();

            this
                .Property(r => r.IdFnsBuyer)
                .HasColumnName(@"ID_FNS_BUYER")
                .HasMaxLength(100);

            this
                .Property(r => r.IdContractorConsignee)
                .HasColumnName(@"ID_CONTRACTOR_CONSIGNEE")
                .IsRequired();

            this
                .Property(r => r.InsertDatetime)
                .HasColumnName(@"INSERT_DATETIME");

            this
                .Property(r => r.InsertUser)
                .HasColumnName(@"INSERT_USER")
                .HasMaxLength(100);

            OnCreated();
        }

        partial void OnCreated();
    }
}
