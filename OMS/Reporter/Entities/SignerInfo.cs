using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class SignerInfo : Base.IReportEntity<SignerInfo>
    {
        private object _powerOfAttorney;

        /// <summary>
        /// Должность
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Тип подписи
        /// </summary>
        public Enums.SignTypeEnum SignType { get; set; }

        /// <summary>
        /// Дата подписания документа
        /// </summary>
        public DateTime SignDate { get; set; }

        /// <summary>
        /// Способ подтверждения полномочий представителя на подписание документа
        /// </summary>
        public Enums.MethodOfConfirmingAuthorityEnum MethodOfConfirmingAuthorityEnum { get; set; }

        /// <summary>
        /// Дополнительные сведения
        /// </summary>
        public string OtherInfo { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string Surname { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Отчество
        /// </summary>
        public string Patronymic { get; set; }

        public object PowerOfAttorney
        {
            get
            {
                return _powerOfAttorney;
            }
            set
            {
                _powerOfAttorney = value;

                if (value as ElectronicPowerOfAttorney != null)
                    ElectronicPowerOfAttorney = value as ElectronicPowerOfAttorney;
                else if (value as PaperPowerOfAttorney != null)
                    PaperPowerOfAttorney = value as PaperPowerOfAttorney;
            }
        }

        /// <summary>
        /// Сведения о доверенности в электронной форме в машиночитаемом виде, используемой для подтверждения полномочий представителя
        /// </summary>
        public ElectronicPowerOfAttorney ElectronicPowerOfAttorney { get; set; }

        /// <summary>
        /// Сведения о доверенности в форме документа на бумажном носителе, используемой для подтверждения полномочий представителя
        /// </summary>
        public PaperPowerOfAttorney PaperPowerOfAttorney { get; set; }
    }
}
