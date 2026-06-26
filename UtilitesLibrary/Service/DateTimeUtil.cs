using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilitesLibrary.Service
{
    public class DateTimeUtil
    {
        public DateTime GetDateTimeByTimestamp(int timestamp)
        {
            return new DateTime(1970, 1, 1).AddSeconds(timestamp);
        }

        public int GetTimestampByDateTime(DateTime dateTime)
        {
            var zeroDateTime = new DateTime(1970, 1, 1);

            return (int)((dateTime.Ticks - zeroDateTime.Ticks) / 10000000);
        }
    }
}
