using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiTest
{
    public class DocGoodLabelForImportClass
    {
        public string Code { get; set; }

        public string Filial { get; set; }

        public DataContextManagementUnit.DataAccess.Contexts.Abt.DocGoodsDetailsLabels GetLabel(DataContextManagementUnit.DataAccess.Contexts.Abt.AbtDbContext abt)
        {
            var label = new DataContextManagementUnit.DataAccess.Contexts.Abt.DocGoodsDetailsLabels
            {
                /*LabelStatus = 2, */IdDoc = 0, DmLabel = Code, InsertDateTime = DateTime.Now
            };

            var barCode = Code.Substring(0, 16).TrimStart('0', '1').TrimStart('0');
            decimal? idGood = abt?.Database?.SqlQuery<decimal?>($"select id_good from ref_bar_codes where bar_code = '{barCode}'")?.FirstOrDefault();

            if (idGood != null)
            {
                label.IdGood = idGood.Value;
            }
            else
                label = null;

            return label;
        }
    }
}
