using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

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
            return JsonSerializer.Deserialize<T>(File.ReadAllText(filePath));
        }

        /// <summary>
        /// Create JSON File with data.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="ob"></param>
        public static void CreateJsonFile(string filePath, object ob)
        {
            File.WriteAllText(filePath, JsonSerializer.Serialize(ob));
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

        /// <summary>
        /// Json prettifier.
        /// </summary>
        /// <param name="jsonData"></param>
        /// <returns></returns>
        public static string JsonPrettifier(string jsonData)
        {
            var options = new JsonSerializerOptions()
            {
                WriteIndented = true
            };
            var jsonElem = JsonSerializer.Deserialize<JsonElement>(jsonData);
            return JsonSerializer.Serialize(jsonElem, options);
        }
    }
}
