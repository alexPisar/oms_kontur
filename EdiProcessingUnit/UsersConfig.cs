using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UtilitesLibrary.ConfigSet;

namespace EdiProcessingUnit
{
    public class UsersConfig
    {
        private const string usersConfigName = "users";

        public UsersConfig()
        {
            string path = GetFullPath( usersConfigName );

            if (!File.Exists( path ))
            {
                throw new Exception( "Не найден файл users.json." );
            }

            using (FileStream fs = new FileStream( path, FileMode.OpenOrCreate ))
            {
                using (StreamReader sr = new StreamReader( fs, Encoding.GetEncoding(1251) ))
                {
                    string str = sr.ReadToEnd();
                    var arrayUsers = JsonConvert.DeserializeObject( str );
                    Users = ((Newtonsoft.Json.Linq.JArray)arrayUsers).ToObject<List<User>>();
                }
            }
        }

        public List<User> Users { get; set; }
        public User SelectedUser { get; set; }

        public bool IsMainAccount => SelectedUser?.UserGLN == "4607971729990";

        private string GetFullPath(string path)
        {
            string currentDirectoryPath = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
            string fullPath = currentDirectoryPath + "\\" + path;

            return fullPath;
        }

        public string GetConnectionString()
        {
            var dataConnection = new DataContextManagementUnit.DataBaseConnection(SelectedUser.Host, SelectedUser.SID, Config.GetInstance().GetDataBaseUser(), Config.GetInstance().GetDataBasePassword());

            return dataConnection.GetConnectionString();
        }

        public string GetConnectionStringByUser(User user)
        {
            var dataConnection = new DataContextManagementUnit.DataBaseConnection( user.Host, user.SID, Config.GetInstance().GetDataBaseUser(), Config.GetInstance().GetDataBasePassword());

            return dataConnection.GetConnectionString();
        }

        public List<string> GetAllConnectionStrings()
        {
            var connStrings = new List<string>();

            foreach (var u in Users)
            {
                var dataConnection = new DataContextManagementUnit.DataBaseConnection( u.Host, u.SID, Config.GetInstance().GetDataBaseUser(), Config.GetInstance().GetDataBasePassword());

                string connString = dataConnection.GetConnectionString();

                connStrings.Add( connString );
            }

            return connStrings;
        }

        public void Save()
        {
            string path = GetFullPath( usersConfigName );
            var usersString = JsonConvert.SerializeObject( Users.ToArray() );

            using (FileStream fs = new FileStream( path, FileMode.Create ))
            {
                using (StreamWriter sw = new StreamWriter( fs, Encoding.GetEncoding( 1251 ) ))
                {
                    sw.Write( usersString );
                }
            }
        }
    }
}
