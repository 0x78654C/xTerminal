using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;

namespace Core.Hardware
{
    public static class WMIDetails
    {
        /// <summary>
        /// Outputs WMI class details 
        /// </summary>
        /// <param name="query">WMI querry</param>
        /// <param name="scope">Scope definition</param>
        /// <returns>string</returns>
        public static string GetWMIDetails(string query, string scope = null)
        {
            ManagementObjectSearcher moSearcher = scope == null ? new ManagementObjectSearcher(query) : new ManagementObjectSearcher(scope, query);
            StringBuilder sb = new StringBuilder();
            foreach (ManagementObject wmi_HD in moSearcher.Get())
            {
                var properties = wmi_HD.Properties;

                foreach (var item in properties)
                {
                    if (wmi_HD[item.Name] == null || string.IsNullOrWhiteSpace(wmi_HD[item.Name].ToString()))
                    {
                        continue;
                    }

                    sb.AppendLine($"{item.Name}: {FormatOutputValue(wmi_HD[item.Name]) }");

                }

                sb.AppendLine(Environment.NewLine + string.Join("", Enumerable.Range(1, 30).Select(t => '-')) + Environment.NewLine);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Outputs WMI class details 
        /// </summary>
        /// <param name="query">WMI querry</param>
        /// <param name="scope">Scope definition</param>
        /// <param name="itemName">Output item name</param>
        /// <returns></returns>
        public static string GetWMIDetails(string query, string[] itemName, string scope = null)
        {
            ManagementObjectSearcher moSearcher = scope == null ? new ManagementObjectSearcher(query) : new ManagementObjectSearcher(scope, query);
            StringBuilder sb = new StringBuilder();
            foreach (ManagementObject wmi_HD in moSearcher.Get())
            {
                foreach (var item in itemName)
                {
                    if (wmi_HD[item] == null || string.IsNullOrWhiteSpace(wmi_HD[item].ToString()))
                    {
                        continue;
                    }             
                        sb.AppendLine($"{item}: {FormatOutputValue(wmi_HD[item]) }");                           
                }

                sb.AppendLine(Environment.NewLine + string.Join("", Enumerable.Range(1, 30).Select(t => '-')) + Environment.NewLine);
            }
            return sb.ToString();
        }

        private static string FormatOutputValue(object item)
        {
            var value = item.ToString();
            if (item is System.Collections.IEnumerable && !(item is string))
            {
                value = "";
                foreach (var item2 in (System.Collections.IEnumerable)item)
                {
                    value += item2 + " ";
                }
            }
            return value;
        }
    }
}
