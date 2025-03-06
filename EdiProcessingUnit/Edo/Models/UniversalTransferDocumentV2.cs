﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.Edo.Models
{
    public partial class UniversalTransferDocumentV2
    {
        private object _status;
        public UniversalTransferDocumentV2()
        {
            OnCreated();
        }

        [Column("ID_DOC")]
        public virtual decimal IdDoc { get; set; }
        
        [Column("ID_DOC_MASTER")]
        public virtual decimal IdDocMaster { get; set; }

        [Column("INVOICE_NUMBER")]
        public virtual string InvoiceNumber { get; set; }

        [Column("INVOICE_DATE")]
        public virtual DateTime? InvoiceDate { get; set; }

        [Column("PERMISSION_STATUS")]
        public virtual int PermissionStatus { get; set; }

        [Column("ID_CHANNEL")]
        public virtual decimal? IdChannel { get; set; }

        [Column("ID_SUBDIVISION")]
        public virtual decimal? IdSubdivision { get; set; }

        [Column("INVOICE_TOTAL_SUMM")]
        public virtual decimal InvoiceTotalSumm { get; set; }

        [Column("INVOICE_TAX_SUMM")]
        public virtual decimal InvoiceTaxSumm { get; set; }

        [Column("IS_MARKED")]
        public virtual bool IsMarked { get; set; }

        [Column("CURRENCY")]
        public virtual string Currency { get; set; }

        [Column("EMPLOYEE")]
        public virtual string Employee { get; set; }

        [Column("EMPLOYEE_POSITION")]
        public virtual string EmployeePosition { get; set; }

        [Column("DOC_DATE")]
        public virtual DateTime DocDate { get; set; }

        [Column("DOC_FUNCTION")]
        public virtual string DocFunction { get; set; }

        [Column("DEFAULT_COUNTRY_CODE")]
        public virtual string DefaultCountryCode { get; set; }

        [Column("SELLER_INN")]
        public virtual string SellerInn { get; set; }

        [Column("SELLER_KPP")]
        public virtual string SellerKpp { get; set; }

        [Column("SELLER_NAME")]
        public virtual string SellerName { get; set; }

        [Column("SELLER_ADDRESS")]
        public virtual string SellerAddress { get; set; }

        [Column("SHIPPER_NAME")]
        public virtual string ShipperName { get; set; }

        [Column("SHIPPER_ADDRESS")]
        public virtual string ShipperAddress { get; set; }

        [Column("BUYER_INN")]
        public virtual string BuyerInn { get; set; }

        [Column("BUYER_KPP")]
        public virtual string BuyerKpp { get; set; }

        [Column("BUYER_NAME")]
        public virtual string BuyerName { get; set; }

        [Column("BUYER_ADDRESS")]
        public virtual string BuyerAddress { get; set; }

        [Column("CONSIGNEE_NAME")]
        public virtual string ConsigneeName { get; set; }

        [Column("CONSIGNEE_ADDRESS")]
        public virtual string ConsigneeAddress { get; set; }

        [Column("ORDER_NUMBER")]
        public virtual string OrderNumber { get; set; }

        [Column("ORDER_DATE")]
        public virtual DateTime OrderDate { get; set; }

        [Column("GLN_SHIP_TO")]
        public virtual string GlnShipTo { get; set; }

        [NotMapped]
        public List<UniversalTransferDocumentDetail> Details { get; set; }

        [NotMapped]
        public RefEdoGoodChannel RefEdoGoodChannel { get; set; }

        [NotMapped]
        public object ProcessingStatus
        {
            get {
                if (_status != null)
                {
                    if (_status as IQueryable<DocComissionEdoProcessing> != null)
                    {
                        var query = _status as IQueryable<DocComissionEdoProcessing>;

                        if (query.Any(s => s.DocStatus == 2))
                            _status = query.FirstOrDefault(s => s.DocStatus == 2);
                        else if (query.Any(s => s.DocStatus == 1))
                            _status = query.FirstOrDefault(s => s.DocStatus == 1);
                        else
                            _status = query.FirstOrDefault();
                    }
                    else if (_status as IEnumerable<DocComissionEdoProcessing> != null)
                    {
                        var collection = _status as IEnumerable<DocComissionEdoProcessing>;

                        if (collection.Any(s => s.DocStatus == 2))
                            _status = collection.FirstOrDefault(s => s.DocStatus == 2);
                        else if (collection.Any(s => s.DocStatus == 1))
                            _status = collection.FirstOrDefault(s => s.DocStatus == 1);
                        else
                            _status = collection.FirstOrDefault();
                    }
                    else if (_status as DocComissionEdoProcessing == null)
                        return null;
                }

                return _status;
            }
            set {
                _status = value;
            }
        }

        public UniversalTransferDocumentV2 Init(AbtDbContext abt)
        {
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

            RefEdoGoodChannel = refEdoGoodChannel;

            Details = (from docGoodDetailI in abt.DocGoodsDetailsIs
                       where docGoodDetailI.IdDoc == this.IdDoc
                       select new UniversalTransferDocumentDetail
                       {
                           DocDetailI = docGoodDetailI,
                           IdGood = docGoodDetailI.IdGood
                       }).ToList();

            Details = Details?.Select(u => u.Init(abt, refEdoGoodChannel))?.ToList();

            if(this.IsMarked)
                _status = (from docComissionEdoProcessing in abt.DocComissionEdoProcessings
                           where docComissionEdoProcessing.IdDoc == this.IdDocMaster
                           orderby docComissionEdoProcessing.DocDate descending
                           select docComissionEdoProcessing);

            return this;
        }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
