using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class DocumentDetails : Base.IReportEntity<DocumentDetails>
    {
        /// <summary>
        /// Наименование документа
        /// </summary>
        public string DocumentName { get; set; }

        /// <summary>
        /// Номер документа
        /// </summary>
        public string DocumentNumber { get; set; }

        /// <summary>
        /// Дата документа
        /// </summary>
        public DateTime DocumentDate { get; set; }

        /// <summary>
        /// Идентификатор файла обмена документа, подписанного первой стороной
        /// </summary>
        public string FirstDocumentId { get; set; }

        /// <summary>
        /// Идентификатор документа
        /// </summary>
        public string DocumentId { get; set; }

        /// <summary>
        /// Идентифицирующая информация об информационной системе, в которой осуществляется хранение документа, необходимая для запроса информации из информационной системы
        /// </summary>
        public string DocumentSystemSavingId { get; set; }

        /// <summary>
        /// Сведения в формате URL об информационной системе, которая предоставляет техническую возможность получения информации о документе
        /// </summary>
        public string UrlAddress { get; set; }

        /// <summary>
        /// Дополнительные сведения
        /// </summary>
        public string DocumentOtherInfo { get; set; }

        /// <summary>
        /// Идентифицирующие реквизиты экономических субъектов, составивших (сформировавших) документ
        /// </summary>
        public FinSubjectCreator[] FinSubjectCreators { get; set; }
    }
}
