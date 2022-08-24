using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class DocEdoProcessingConfiguration : EntityTypeConfiguration<DocEdoProcessing>
    {
        public DocEdoProcessingConfiguration()
        {
            this
                .HasKey(d => d.Id)
                .ToTable("DOC_EDO_PROCESSING", "EDI");

            this
                .Property(d => d.Id)
                .HasColumnName("ID")
                .IsRequired()
                .HasMaxLength(36);

            this
                .Property(d => d.MessageId)
                .HasColumnName("MESSAGE_ID")
                .HasMaxLength(36);

            this
                .Property(d => d.EntityId)
                .HasColumnName("ENTITY_ID")
                .HasMaxLength(36);

            this
                .Property(d => d.FileName)
                .HasColumnName("FILE_NAME")
                .HasMaxLength(500);

            this
                .Property(d => d.IsReprocessingStatus)
                .HasColumnName("IS_REPROCESSING_STATUS");

            this
                .Property(d => d.IdComissionDocument)
                .HasColumnName("ID_DOC_COMISSION")
                .HasMaxLength(36);

            this
                .Property(d => d.AnnulmentStatus)
                .HasColumnName("ANNULMENT_STATUS");

            this
                .Property(d => d.AnnulmentFileName)
                .HasColumnName("ANNULMENT_FILENAME")
                .HasMaxLength(500);

            this
                .Property(d => d.IdDoc)
                .HasColumnName("ID_DOC");

            this
                .Property(d => d.DocDate)
                .HasColumnName("DOC_DATE")
                .IsRequired();

            this
                .Property(d => d.UserName)
                .HasColumnName("USER_NAME")
                .HasMaxLength(100);

            this
                .Property(d => d.ReceiverName)
                .HasColumnName("RECEIVER_NAME")
                .HasMaxLength(200);

            this
                .Property(d => d.ReceiverInn)
                .HasColumnName("RECEIVER_INN")
                .HasMaxLength(20);

            this
                .Property(d => d.DocStatus)
                .HasColumnName("DOC_STATUS");

            OnCreated();
        }

        partial void OnCreated();
    }
}
