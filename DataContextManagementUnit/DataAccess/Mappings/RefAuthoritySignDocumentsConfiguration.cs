using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public class RefAuthoritySignDocumentsConfiguration : EntityTypeConfiguration<RefAuthoritySignDocuments>
    {
        public RefAuthoritySignDocumentsConfiguration()
        {
            this
                .HasKey(r => r.IdCustomer)
                .ToTable("REF_AUTHORITY_SIGN_DOCUMENTS", "EDI");

            this
                .Property(r => r.IdCustomer)
                .HasColumnName(@"ID_CUSTOMER")
                .IsRequired();

            this
                .Property(r => r.Surname)
                .HasColumnName(@"SURNAME")
                .HasMaxLength(60);

            this
                .Property(r => r.Name)
                .HasColumnName(@"NAME")
                .HasMaxLength(60);

            this
                .Property(r => r.PatronymicSurname)
                .HasColumnName(@"PATRONIMYC_SURNAME")
                .HasMaxLength(60);

            this
                .Property(r => r.Position)
                .HasColumnName(@"POSITION")
                .HasMaxLength(128);

            this
                .Property(r => r.Inn)
                .HasColumnName(@"INN")
                .HasMaxLength(15);
        }
    }
}
