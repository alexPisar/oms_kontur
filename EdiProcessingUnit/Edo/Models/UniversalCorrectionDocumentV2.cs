using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using System.ComponentModel.DataAnnotations.Schema;

namespace EdiProcessingUnit.Edo.Models
{
    public class UniversalCorrectionDocumentV2
    {
        [Column("ID_DOC")]
        public virtual decimal IdDoc { get; set; }

        [Column("ID_DOC_TYPE")]
        public virtual decimal IdDocType { get; set; }

        [Column("ID_DOC_MASTER")]
        public virtual decimal IdDocMaster { get; set; }

        [Column("INVOICE_NUMBER")]
        public virtual string InvoiceNumber { get; set; }

        [Column("DOC_NUMBER")]
        public virtual string DocNumber { get; set; }

        [Column("CORRECTION_NUMBER")]
        public virtual string CorrectionNumber { get; set; }

        [Column("ID_CHANNEL")]
        public virtual decimal? IdChannel { get; set; }

        [Column("DOC_DATE")]
        public virtual DateTime DocDate { get; set; }

        [Column("INVOICE_DELIVERY_DATE")]
        public virtual DateTime? InvoiceDeliveryDate { get; set; }

        [Column("SELLER_ORG_NAME")]
        public virtual string SellerOrgName { get; set; }

        [Column("SELLER_INN")]
        public virtual string SellerInn { get; set; }

        [Column("SELLER_KPP")]
        public virtual string SellerKpp { get; set; }

        [Column("SELLER_ADDRESS")]
        public virtual string SellerAddress { get; set; }

        [Column("BUYER_ORG_NAME")]
        public virtual string BuyerOrgName { get; set; }

        [Column("BUYER_INN")]
        public virtual string BuyerInn { get; set; }

        [Column("BUYER_KPP")]
        public virtual string BuyerKpp { get; set; }

        [Column("BUYER_ADDRESS")]
        public virtual string BuyerAddress { get; set; }

        [Column("IS_MARKED")]
        public virtual bool IsMarked { get; set; }

        [Column("EMPLOYEE")]
        public virtual string Employee { get; set; }

        [Column("ORDER_NUMBER")]
        public virtual string OrderNumber { get; set; }

        [Column("ORDER_DATE")]
        public virtual DateTime OrderDate { get; set; }

        [Column("GLN_SHIP_TO")]
        public virtual string GlnShipTo { get; set; }

        [Column("DOC_RETURN_NUMBER")]
        public virtual string DocReturnNumber { get; set; }

        [Column("DOC_RETURN_DATE")]
        public virtual string DocReturnDate { get; set; }

        [Column("CONTRACT_NUMBER")]
        public virtual string ContractNumber { get; set; }

        [Column("CONTRACT_DATE")]
        public virtual string ContractDate { get; set; }

        [NotMapped]
        public RefEdoGoodChannel RefEdoGoodChannel { get; set; }

        [NotMapped]
        public List<UniversalCorrectionDocumentDetail> Details { get; set; }

        [NotMapped]
        public DocEdoProcessing BaseProcessing { get; set; }

        public UniversalCorrectionDocumentV2 Init(AbtDbContext abt)
        {
            DocJournal invoiceDocJournal;

            if (IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
                invoiceDocJournal = abt.DocJournals.FirstOrDefault(inv => inv.IdDocMaster == this.IdDocMaster
                && inv.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && inv.Code == this.InvoiceNumber);
            else if (IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                invoiceDocJournal = abt.DocJournals.FirstOrDefault(inv => inv.Id == this.IdDocMaster
                && inv.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && inv.Code == this.InvoiceNumber);
            else
                throw new Exception($"Неизвестный формат корректировки {CorrectionNumber}");

            RefEdoGoodChannel refEdoGoodChannel = null;
            if (IdChannel != null && IdChannel != 99001)
                refEdoGoodChannel = (from r in abt.RefContractors
                                     where r.IdChannel == IdChannel
                                     join s in (from c in abt.RefContractors
                                                where c.DefaultCustomer != null
                                                join refEdo in abt.RefEdoGoodChannels on c.IdChannel equals (refEdo.IdChannel)
                                                select new { RefEdoGoodChannel = refEdo, RefContractor = c })
                                                on r.DefaultCustomer equals (s.RefContractor.DefaultCustomer)
                                     where s != null
                                     select s.RefEdoGoodChannel)?.FirstOrDefault();

            if(IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
                Details = (from docGoodDetail in abt.DocGoodsDetails
                           where docGoodDetail.IdDoc == this.IdDoc
                           select new UniversalCorrectionDocumentDetail
                           {
                               DocDetail = docGoodDetail,
                               IdGood = docGoodDetail.IdGood
                           }).ToList();
            else if(IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                Details = (from docGoodDetail in abt.DocGoodsDetailsIs
                           where docGoodDetail.IdDoc == this.IdDoc
                           select new UniversalCorrectionDocumentDetail
                           {
                               DocDetailsI = docGoodDetail,
                               IdGood = docGoodDetail.IdGood
                           }).ToList();

            Details = Details?.Select(u => u.Init(abt, invoiceDocJournal, this.IsMarked, refEdoGoodChannel)?.SetBarCodeFromDataBase(abt))?.Where(u => u != null)?.ToList();

            refEdoGoodChannel?.EdoUcdValuesPairs?.ToList();
            RefEdoGoodChannel = refEdoGoodChannel;

            DocEdoProcessing baseProcessing = null;
            if (IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
            {
                var baseProcessings = from pr in abt.DocEdoProcessings
                                      where pr.IdDoc == this.IdDocMaster && pr.DocType == (int)Enums.DocEdoType.Upd
                                      && pr.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing
                                      && pr.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.RevokedAndProcessed
                                      && pr.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.Revoked
                                      select pr;
                baseProcessing = baseProcessings.FirstOrDefault(d =>
                d.DocStatus == (int)Enums.DocEdoSendStatus.Signed || d.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned);

                if (baseProcessing == null)
                    baseProcessing = baseProcessings.FirstOrDefault();
            }
            else if (IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
            {
                var baseProcessings = from pr in abt.DocEdoProcessings
                                      where pr.IdDoc == invoiceDocJournal.IdDocMaster && pr.DocType == (int)Enums.DocEdoType.Upd
                                      && pr.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing
                                      && pr.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.RevokedAndProcessed
                                      && pr.AnnulmentStatus != (int)HonestMark.AnnulmentDocumentStatus.Revoked
                                      select pr;
                baseProcessing = baseProcessings.FirstOrDefault(d =>
                d.DocStatus == (int)Enums.DocEdoSendStatus.Signed || d.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned);

                if (baseProcessing == null)
                    baseProcessing = baseProcessings.FirstOrDefault();
            }

            BaseProcessing = baseProcessing;

            return this;
        }
    }
}
