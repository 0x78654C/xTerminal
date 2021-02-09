using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Diagnostics;

namespace xEditor
{
    class Program
    {
        /*Text editor opener*/
        static void Main(string[] args)
        {
            string file=string.Empty;
            string dlocation = File.ReadAllText(@".\Data\curDir.ini");

            try
            {
                file = args[0];
                var process = new Process();
                process.StartInfo = new ProcessStartInfo("notepad")
                {
                    UseShellExecute = false,
                    Arguments = dlocation + @"\" + file

                };

                process.Start();
                process.WaitForExit();


            }
            catch
            {

            }

        }
    }
  
}
