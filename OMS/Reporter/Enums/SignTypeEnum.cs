using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace Reporter.Enums
{
    public enum SignTypeEnum
    {
        /// <summary>
        /// усиленная квалифицированная электронная подпись
        /// </summary>
        [Display(Description = "1 - усиленная квалифицированная электронная подпись")]
        QualifiedElectronicDigitalSignature = 1,
        /// <summary>
        /// простая электронная подпись
        /// </summary>
        [Display(Description = "2 - простая электронная подпись")]
        SimpleElectronicDigitalSignature,
        /// <summary>
        /// усиленная неквалифицированная электронная подпись
        /// </summary>
        [Display(Description = "3 - усиленная неквалифицированная электронная подпись")]
        NonQualifiedElectronicDigitalSignature
    }
}
