using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace Core.SystemTools
{
    public static class JsonManage
    {
        /// <summary>
        /// Read data from JSON file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static T ReadJsonFromFile<T>(string filePath)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            return serializer.Deserialize<T>(File.ReadAllText(filePath));
        }

        /// <summary>
        /// Create JSON File with data.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ob"></param>
        public static void CreateJsonFile(string filePath, object ob)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            File.WriteAllText(filePath, serializer.Serialize(ob));
        }

        /// <summary>
        /// Update JSON file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="ToAdd"></param>
        public static void UpdateJsonFile<T>(string filePath, T ToAdd)
        {
            if (!File.Exists(filePath))
            {
                CreateJsonFile(filePath, new[] { ToAdd });
                return;
            }
            var json = ReadJsonFromFile<T[]>(filePath).ToList();
            json.Add(ToAdd);
            CreateJsonFile(filePath, json.ToArray());
        }

        /// <summary>
        /// Delete data from a JSON file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filePath"></param>
        /// <param name="toRemove"></param>
        public static void DeleteJsonData<T>(string filePath, Func<IEnumerable<T>, IEnumerable<T>> toRemove)
        {
            if (!File.Exists(filePath)) { return; }
            var json = ReadJsonFromFile<T[]>(filePath).ToList();
            json = json.Except(toRemove(json)).ToList();
            CreateJsonFile(filePath, json.ToArray());
        }
    }
}
