﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess
{
    public enum DocJournalType
    {
        Receipt = 1,
        Requirement,
        Translocation,
        ReturnFromBuyer = 7,
        Invoice,
        Correction = 18
    }
}
