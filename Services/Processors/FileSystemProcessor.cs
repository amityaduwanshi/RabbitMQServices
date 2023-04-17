using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQService;
using Services.Helper;
using System.Text;
using System.Timers;

namespace Services.Processors
{
    public static class FileSystemProcessor
    {
        private readonly static Dictionary<string, IDictionary<string,string>> _fileSystemManager = new();
        private readonly static List<System.Timers.Timer> _timers = new();
        private static IConfiguration _configuration;
        #region Public Method
        /// <summary>
        /// Configure Consumer and Publisher with it's respective configuration
        /// </summary>
        /// <param name="configuration"></param>
        public static void Bootstrap(IConfiguration configuration)
        {
            _configuration = configuration;
            var configuraitonSec = configuration.GetSection("Configuration:Monitor:FileSytem");
            foreach (var fileSystemManager in configuraitonSec.GetChildren())
            {
                var fileSystemManagerDic = ConfigHelper.GetDictionary(fileSystemManager);
                if (fileSystemManagerDic == null) continue;
               
                _fileSystemManager.Add(fileSystemManager.Key, fileSystemManagerDic);
            }
        }

        public static void ActivateMonitor()
        {
            foreach (var fileSystemManager in _fileSystemManager)
            {
                var fileSystemManagerDic = fileSystemManager.Value;

                var path = fileSystemManagerDic["Path"];
                var waitIntervalInMilliSeconds = Convert.ToInt64(fileSystemManagerDic["WaitIntervalInMilliSeconds"]);
                if (Directory.Exists(path))
                {
                    var timer = new System.Timers.Timer(waitIntervalInMilliSeconds);
                    timer.Elapsed += (obj, eventArgs) =>
                    {
                        Processor(obj, eventArgs, fileSystemManager.Value);
                    };
                    timer.Enabled = true;
                    timer.Start();
                    _timers.Add(timer);
                }
                else
                {
                    //TODO: Write Log
                }
            }
        }
        #endregion

        #region Private Method
        private static void Processor(object? obj, ElapsedEventArgs eventArgs, IDictionary<string,string> fileSystemManagerDic)
        {
            var path = fileSystemManagerDic["Path"];
            var extenstions = fileSystemManagerDic["Extenstions"];
            var queueManagers = fileSystemManagerDic["QueueManagers"];
            var publisherName = fileSystemManagerDic["Publisher"];
            var completedProcessPath = fileSystemManagerDic["CompletedProcessPath"];
            var files = Directory.EnumerateFiles(path);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (string.IsNullOrEmpty(extenstions) && !extenstions.Split(";").Contains(fileInfo.Extension))
                {
                    continue;
                }
                var publisher = GetRMQPublisher(queueManagers,publisherName);
                if(publisher!= null)
                {
                    publisher.BindQueue();
                    publisher.Publish(File.ReadAllBytes(file));
                    if (Directory.Exists(completedProcessPath))
                    {
                        Directory.CreateDirectory(completedProcessPath);
                    }

                    File.Move(file, $"{completedProcessPath}\\{fileInfo.Name}",true);
                }

            }
        }

        private static Publisher? GetRMQPublisher(string queueManagers,string publisherPath)
        {
                IConfigurationSection configuration = _configuration.GetSection($"Configuration:{publisherPath}");
            Publisher? publisher = null;
                var publisherDic = ConfigHelper.GetDictionary(configuration);
                if (publisherDic != null)
                {
                    publisher = RabbitMQProcessor.GetPublisher(publisherDic, _configuration.GetSection($"Configuration:{queueManagers}"));
                }
            return publisher;
        }
        #endregion
    }
}
