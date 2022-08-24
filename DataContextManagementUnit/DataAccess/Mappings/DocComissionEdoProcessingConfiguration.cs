using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class DocComissionEdoProcessingConfiguration : EntityTypeConfiguration<DocComissionEdoProcessing>
    {
        public DocComissionEdoProcessingConfiguration()
        {
            this
                .HasKey(d => d.Id)
                .ToTable("DOC_COMISSION_EDO_PROCESSING", "EDI");

            this
                .Property(d => d.Id)
                .HasColumnName("ID")
                .IsRequired()
                .HasMaxLength(36);

            this
                .Property(d => d.IdDoc)
                .HasColumnName("ID_DOC");

            this
                .Property(d => d.SenderInn)
                .HasColumnName("SENDER_INN")
                .HasMaxLength(20);

            this
                .Property(d => d.ReceiverInn)
                .HasColumnName("RECEIVER_INN")
                .HasMaxLength(20);

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
                .Property(d => d.DocStatus)
                .HasColumnName("DOC_STATUS");

            this
                .Property(d => d.ErrorMessage)
                .HasColumnName("ERROR_MESSAGE")
                .HasMaxLength(1000);

            this
                .Property(d => d.UserName)
                .HasColumnName("USER_NAME")
                .HasMaxLength(100);

            this
                .Property(d => d.DocDate)
                .HasColumnName("DOC_DATE")
                .IsRequired();

            this
                .Property(d => d.DeliveryDate)
                .HasColumnName("DELIVERY_DATE");

            this
                .Property(d => d.NumberOfReturnDocuments)
                .HasColumnName("NUMBER_OF_RETURN_DOCUMENTS");

            this
                .Property(d => d.AnnulmentStatus)
                .HasColumnName("ANNULMENT_STATUS");

            this
                .Property(d => d.AnnulmentFileName)
                .HasColumnName("ANNULMENT_FILENAME")
                .HasMaxLength(500);

            this
                .Property(d => d.IsMainDocumentError)
                .HasColumnName("IS_MAIN_DOCUMENT_ERROR");

            this
                .HasMany(d => d.MainDocuments)
                .WithOptional(p => p.ComissionDocument)
                .HasForeignKey(p => p.IdComissionDocument)
                .WillCascadeOnDelete(false);

            OnCreated();
        }

        partial void OnCreated();
    }
}
