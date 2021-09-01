using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Instrumentation;
using System.Text;
using System.Threading.Tasks;

namespace Core.Hardware
{

    /// <summary>
    ///  Run powershell scripts
    /// </summary>
    public static class PowerShell
    {
        /// <summary>
        /// Runs a powershell script 
        /// </summary>
        /// <param name="psCommand">Powershell command</param>
        /// <returns>string</returns>
        public static string RunPowerShellScript(string psCommand)
        {
            try
            {
                Runspace runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                Pipeline pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(psCommand);
                pipeline.Commands.Add("Out-String");
                Collection<PSObject> reults = pipeline.Invoke();
                runspace.Close();
                StringBuilder stringBuilder = new StringBuilder();
                foreach (PSObject pSObject in reults)
                {
                    stringBuilder.AppendLine(pSObject.ToString());
                }

                return stringBuilder.ToString();
            }
            catch(Exception e)
            {
                return e.Message;
            }
        }
    }
}
