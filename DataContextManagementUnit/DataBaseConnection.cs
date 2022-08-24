using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit
{
    public class DataBaseConnection
    {
        private string _login;
        private string _password;
        private string _host;
        private string _sid;

        public DataBaseConnection(string host, string sid, string login, string password)
        {
            _host = host;
            _sid = sid;
            _login = login;
            _password = password;
        }

        public string GetConnectionString()
        {
            return $"User Id={_login};Password={_password};Data Source={_host}/{_sid}";
        }
    }
}
