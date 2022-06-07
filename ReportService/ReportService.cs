using Cipher;
using EmailSender.Models;
using ReportService.Core;
using ReportService.Core.Repositories;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace ReportService
{
    public partial class ReportService : ServiceBase
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly int SendHour = 8;
        private readonly int IntervalInMinutes;
        private readonly bool SendingReportsEnable;
        private Timer _timer;
        private ErrorRepository _errorRepository = new ErrorRepository();
        private ReportRepository _reportRepository = new ReportRepository();
        private Email _email;
        private GenerateHtmlEmail _htmlEmail = new GenerateHtmlEmail();
        private string _emailReceiver;
        private StringCipher _stringCipher = new StringCipher("BA7D49D2-3769-4A59-9C93-08389D129CEF");
        private const string NotEncryptedPasswordPrefix = "encrypt:";


        public ReportService()
        {
            InitializeComponent();
            

            try
            {
                SendHour = int.Parse(ConfigurationManager.AppSettings["SendHour"]);
                IntervalInMinutes = int.Parse(ConfigurationManager.AppSettings["IntervalInMinutes"]);
                SendingReportsEnable = bool.Parse(ConfigurationManager.AppSettings["SendingReportsEnable"]);

                _timer = new Timer(IntervalInMinutes * 60000);


                _emailReceiver = ConfigurationManager.AppSettings["ReceiverEmail"];

                _email = new Email(new EmailParams
                {
                    HostSmtp = ConfigurationManager.AppSettings["HostSmtp"],
                    Port = int.Parse(ConfigurationManager.AppSettings["Port"]),
                    EnableSsl = bool.Parse(ConfigurationManager.AppSettings["EnableSsl"]),
                    SenderName = ConfigurationManager.AppSettings["SenderName"],
                    SenderEmail = ConfigurationManager.AppSettings["SenderEmail"],
                    SenderEmailPassword = DecryptSenderEmailPassword()
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private string DecryptSenderEmailPassword()
        {          

            var encryptedPassword = ConfigurationManager.AppSettings["SenderEmailPassword"];

            if (encryptedPassword.StartsWith(NotEncryptedPasswordPrefix))
            {
                encryptedPassword = _stringCipher.Encrypt(encryptedPassword.Replace(NotEncryptedPasswordPrefix, ""));

                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configFile.AppSettings.Settings["SenderEmailPassword"].Value = encryptedPassword;
                configFile.Save();
            }
            return _stringCipher.Decrypt(encryptedPassword);
        }

        protected override void OnStart(string[] args)
        {
            _timer.Elapsed += DoWork;
            _timer.Start();
            Logger.Info("Service started.");
        }
        //private void DoWork1(object sender, ElapsedEventArgs e)
        //{
        //    Logger.Info("Timer");
        //}
        private async void DoWork(object sender, ElapsedEventArgs e)
        {
            try
            {
                await SendError();
                if(SendingReportsEnable) await SendReport();
            }
            catch (Exception ex)
            {

                Logger.Error(ex, ex.Message);
                throw new Exception(ex.Message);
            }
        }

        private async Task SendError()
        {
            var errors = _errorRepository.GetLastErrors(IntervalInMinutes);

            if (errors == null || !errors.Any())
                return;

            await _email.Send("Błędy w aplikacji", _htmlEmail.GenerateErrors(errors, IntervalInMinutes), _emailReceiver);

            Logger.Info("Error sent.");
        }

        private async Task SendReport()
        {
            var actualHour = DateTime.Now.Hour;

            if (actualHour < SendHour)
                return;

            var report = _reportRepository.GetLastNotSentReport();

            if (report == null)
                return;

            await _email.Send("Raport dobowy", _htmlEmail.GenerateReport(report), _emailReceiver);


            _reportRepository.ReportSent(report);


            Logger.Info("Report sent.");
        }

        protected override void OnStop()
        {
            Logger.Info("Service stopped.");
        }
    }
}
