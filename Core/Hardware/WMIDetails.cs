using System;
using System.Linq;
using System.Management;
using System.Text;
using System.Runtime.Versioning;

namespace Core.Hardware
{
    [SupportedOSPlatform("windows")]
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
        /// <returns>string</returns>
        public static string GetWMIDetails(string query, string[] itemName, string scope = null)
        {
            ManagementObjectSearcher moSearcher = scope == null ? new ManagementObjectSearcher(query) : new ManagementObjectSearcher(scope, query);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join("", Enumerable.Range(1, 30).Select(t => '-')) + Environment.NewLine);
            foreach (ManagementObject wmi_HD in moSearcher.Get())
            {
                foreach (var item in itemName)
                {
                    if (wmi_HD[item] == null || string.IsNullOrWhiteSpace(wmi_HD[item].ToString()))
                    {
                        continue;
                    }
                    if (item.Contains("Size"))
                    {
                        sb.AppendLine($"{item}: {FormatOutputValue(wmi_HD[item])} bytes");
                    }
                    else
                    {
                        sb.AppendLine($"{item}: {FormatOutputValue(wmi_HD[item]) }");
                    }

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

        /// <summary>
        /// Convert bytes to GB directly on WMI output
        /// </summary>
        /// <param name="data"> Input WMI data with Size(capacaty) parameter.</param>
        /// <returns>string</returns>
        public static string SizeConvert(string data, bool onlySize)
        {
            string wmiOut = string.Empty;
            foreach (var wmiData in data.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (wmiData.Contains("Size"))
                {
                    double sizeDeviced = Convert.ToDouble(wmiData.Split(' ')[1]);
                    for (int i = 0; i < 3; i++)
                    {
                        sizeDeviced /= 1024;
                    }
                    sizeDeviced = Math.Round(sizeDeviced, 2);
                    if (onlySize)
                    {
                        wmiOut += $"{sizeDeviced} GB" ;
                    }
                    else
                    {
                        wmiOut += $"Size: {sizeDeviced} GB" + Environment.NewLine;
                    }
                }

                if (!onlySize)
                    wmiOut += wmiData + Environment.NewLine;
            }
            return wmiOut;
        }
    }
}
