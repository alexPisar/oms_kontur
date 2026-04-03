using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter
{
    public class ReporterDll
    {
        public TReport ParseDocument<TReport>(string content) where TReport : IReport, new()
        {
            TReport report = new TReport();
            report.Parse(content);
            return report;
        }

        public TReport ParseDocument<TReport>(byte[] content) where TReport : IReport, new()
        {
            TReport report = new TReport();
            report.Parse(content);
            return report;
        }
    }
}
