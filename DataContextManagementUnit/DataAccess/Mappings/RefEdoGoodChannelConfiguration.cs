using System.Data.Entity.ModelConfiguration;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt.Mapping
{
    public partial class RefEdoGoodChannelConfiguration : EntityTypeConfiguration<RefEdoGoodChannel>
    {
        public RefEdoGoodChannelConfiguration()
        {
            this
                .HasKey(r => r.Id)
                .ToTable("REF_EDO_GOODS_CHANNELS", "EDI");

            this
                .Property(r => r.Id)
                .HasColumnName(@"ID")
                .IsRequired()
                .HasMaxLength(36);

            this
                .Property(r => r.IdChannel)
                .HasColumnName(@"ID_CHANNEL");

            this
                .Property(r => r.Name)
                .HasColumnName(@"NAME")
                .HasMaxLength(100);

            this
                .Property(r => r.CreateDateTime)
                .HasColumnName(@"CREATE_DATETIME")
                .IsRequired();

            this
                .Property(r => r.UserName)
                .HasColumnName(@"USER_NAME")
                .HasMaxLength(100);

            this
                .Property(r => r.EdiGln)
                .HasColumnName(@"EDI_GLN")
                .HasMaxLength(16);

            this
                .Property(r => r.PermittedForOtherFilials)
                .HasColumnName(@"PERMITTED_FOR_OTHER_FILIALS");

            this
                .Property(r => r.IdFilial)
                .HasColumnName(@"ID_FILIAL");

            this
                .Property(r => r.NumberUpdId)
                .HasColumnName(@"NUMBER_UPD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.OrderNumberUpdId)
                .HasColumnName(@"ORDER_NUMBER_UPD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.OrderDateUpdId)
                .HasColumnName(@"ORDER_DATE_UPD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.DetailBuyerCodeUpdId)
                .HasColumnName(@"DETAIL_BUYER_CODE_UPD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.DetailBarCodeUpdId)
                .HasColumnName(@"DETAIL_BAR_CODE_UPD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.DocReturnNumberUcdId)
                .HasColumnName(@"DOC_RETURN_NUMBER_UCD_ID")
                .HasMaxLength(50);

            this
                .Property(r => r.DocReturnDateUcdId)
                .HasColumnName(@"DOC_RETURN_DATE_UCD_ID")
                .HasMaxLength(50);

            this
                .HasMany(r => r.EdoValuesPairs)
                .WithRequired(p => p.EdoGoodChannel)
                .HasForeignKey(p => p.IdEdoGoodChannel)
                .WillCascadeOnDelete(false);

            this
                .HasMany(r => r.EdoUcdValuesPairs)
                .WithRequired(p => p.EdoGoodChannel)
                .HasForeignKey(p => p.IdEdoGoodChannel)
                .WillCascadeOnDelete(false);

            OnCreated();
        }

        partial void OnCreated();
    }
}
