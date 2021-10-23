using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DCCHelper
{
    public static class SerializationHelper
    {
        public static T DeserializeJsonToObject<T>(string jsonString)
        {
            T obj = JsonConvert.DeserializeObject<T>(jsonString);
            return obj;
        }
    }
}
