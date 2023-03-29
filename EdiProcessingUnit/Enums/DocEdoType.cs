using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit.Enums
{
    public enum DocEdoType
    {
        Upd = (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument,
        UpdRevision = (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocumentRevision,
        Ucd = (int)Diadoc.Api.Proto.DocumentType.UniversalCorrectionDocument
    }
}
