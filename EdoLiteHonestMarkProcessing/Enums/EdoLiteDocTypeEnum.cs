using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdoLiteHonestMarkProcessing.Enums
{
    public enum EdoLiteDocTypeEnum
    {
        None = 0,
        DpUvutoch = 110,
        DpPdpol,
        DpPdotpr,
        DpIzvpolSeller,
        DpPrannul,
        DpIzvpolBuyer,
        UcdDis = 200,
        UcdDisBuyer,
        UcdCorrectionInvoice,
        UcdCorrectionInvoiceDis = 204,
        UcdCorrectionInvoiceDisBuyer,
        UcdiDis = 400,
        UpdDop = 500,
        UpdDopBuyer,
        UpdInvoice,
        UpdInvoiceDop = 504,
        UpdInvoiceDopBuyer,
        UpdDop970 = 520,
        UpdDopBuyer970,
        UpdInvoice970,
        UpdInvoiceDop970 = 524,
        UpdInvoiceDopBuyer970,
        UpdiDop = 800,
        UpdiDopBuyer,
        UpdiInvoice,
        UpdiInvoiceDop = 804,
        UpdiInvoiceDopBuyer,
        UpdiDop970 = 820,
        UpdiDopBuyer970,
        UpdiInvoice970,
        UpdiInvoiceDop970 = 824,
        UpdiInvoiceDopBuyer970
    }
}
