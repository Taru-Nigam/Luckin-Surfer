using Microsoft.AspNetCore.Mvc.ViewFeatures; // For ITempDataDictionary
using Newtonsoft.Json; // Make sure Newtonsoft.Json NuGet package is installed

namespace GameCraft.Helpers 
{
    public static class TempDataExtensions
    {
        public static void Set<T>(this ITempDataDictionary tempData, string key, T value) where T : class
        {
            tempData[key] = JsonConvert.SerializeObject(value);
        }

        public static T? Get<T>(this ITempDataDictionary tempData, string key) where T : class
        {
            tempData.TryGetValue(key, out object? o);
            return o == null ? null : JsonConvert.DeserializeObject<T>((string)o);
        }
    }
}