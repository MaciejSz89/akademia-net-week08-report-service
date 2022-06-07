using Cipher;
using EmailSender.Models;
using ReportService.Core;
using ReportService.Core.Models.Domains;
using ReportService.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ReportService.ConsoleApp
{
    public class Program
    {
        static void Main(string[] args)
        {

            StringCipher stringCipher = new StringCipher("BA7D49D2-3769-4A59-9C93-08389D129CEF");


 

                var encryptedPassword = ConfigurationManager.AppSettings["SenderEmailPassword"];

                if (encryptedPassword.StartsWith("encrypt:"))
                {
                    encryptedPassword = stringCipher.Encrypt(encryptedPassword.Replace("encrypt:", ""));

                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    configFile.AppSettings.Settings["SenderEmailPassword"].Value = encryptedPassword;
                    configFile.Save();
                }
                var decryptedPassword = stringCipher.Decrypt(encryptedPassword);
            
            
            Console.WriteLine(decryptedPassword);



        }





    }
}

