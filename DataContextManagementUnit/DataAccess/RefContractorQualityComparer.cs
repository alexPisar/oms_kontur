using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts
{
    public class RefContractorQualityComparer : IEqualityComparer<Abt.RefContractor>
    {
        public bool Equals(Abt.RefContractor r1, Abt.RefContractor r2)
        {
            if (r1 == null && r2 == null)
                return true;

            if (r1 == null || r2 == null)
                return false;

            return r1.Id == r2.Id;
        }

        public int GetHashCode(Abt.RefContractor contractor)
        {
            return contractor.GetHashCode();
        }
    }
}
