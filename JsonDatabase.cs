using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotSatoszow
{
    public class JsonDatabase
    {
        public Dictionary<long, UserData> UserDataDictionary;

        private string FilePath = "./JsonDatabase.json";

        public JsonDatabase()
        {
            LoadCurrentDatabaseFromFile();
        }        


        private void LoadCurrentDatabaseFromFile()
        {
            var fileExists = File.Exists(FilePath);

            if(fileExists)
            {
                var fileContent = File.ReadAllText(FilePath);
                var userDataList = JsonConvert.DeserializeObject<List<UserData>>(fileContent);
                if(userDataList != null && userDataList.Count != 0)
                {
                    UserDataDictionary = userDataList.ToDictionary(x => x.Id, x => x);
                }
                else
                {
                    UserDataDictionary = new Dictionary<long, UserData>();
                }
            }
            else
            {
                var fileStream = File.Create(FilePath);
                fileStream.Dispose();
                File.WriteAllText(FilePath, JsonConvert.SerializeObject(new List<UserData>()));
            }
        }

    }
}
