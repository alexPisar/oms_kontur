using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public class RefAuthoritySignDocumentsConfiguration : EntityTypeConfiguration<RefAuthoritySignDocuments>
    {
        public RefAuthoritySignDocumentsConfiguration()
        {
            this
                .HasKey(r => new { r.IdCustomer, r.Inn })
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

            this
                .Property(r => r.DataBaseUserName)
                .HasColumnName(@"DATABASE_USER_NAME")
                .HasMaxLength(40);

            this
                .Property(r => r.Comment)
                .HasColumnName(@"COMENT")
                .HasMaxLength(200);

            this
                .Property(r => r.EmchdId)
                .HasColumnName(@"EMCHD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.EmchdBeginDate)
                .HasColumnName(@"EMCHD_BEGIN_DATE");

            this
                .Property(r => r.EmchdEndDate)
                .HasColumnName(@"EMCHD_END_DATE");

            this
                .Property(r => r.IsMainDefault)
                .HasColumnName(@"IS_MAIN_DEFAULT");
        }
    }
}
