using System.IO;
using Newtonsoft.Json;

namespace UtilitesLibrary.Configuration
{
	public class Configuration<TConfig>
	{
		protected const string ConfigFolder = "conf";

		protected virtual string Path(string FileName) => ConfigFolder + "\\" + FileName;

		public TConfig Load(string FileName) 
		{
			var path = Path( FileName );

            path = GetFullPath( path );

            if (!File.Exists( path ))
			{
				var a = default( TConfig );
				return a;
			}

			using (FileStream fs = new FileStream( path, FileMode.OpenOrCreate ))
			{
				using (StreamReader sr = new StreamReader( fs ))
				{					
					return JsonConvert.DeserializeObject<TConfig>( sr.ReadToEnd() );
				}
			}
		}

        private string GetFullPath(string path)
        {
            string currentDirectoryPath = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
            string fullPath = currentDirectoryPath + "\\" + path;

            return fullPath;
        }

        public void Save(TConfig config, string FileName)
		{

			JsonSerializer serializer = new JsonSerializer();
			serializer.NullValueHandling = NullValueHandling.Ignore;

            var path = GetFullPath( Path( FileName ) );
			using (StreamWriter sw = new StreamWriter(path))
			using (JsonWriter writer = new JsonTextWriter( sw ))
			{
				serializer.Serialize( writer, config );
			}

		}
	}
}
