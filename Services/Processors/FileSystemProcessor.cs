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
        private readonly static Dictionary<string, (string Path, long WaitIntervalInMilliSeconds, string Extenstions, string QueueManagers, string Publisher)> _fileSystemManager = new();
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
                var path = fileSystemManagerDic["Path"];
                var waitIntervalInMilliSeconds = Convert.ToInt64(fileSystemManagerDic["WaitIntervalInMilliSeconds"]);
                var extenstions = fileSystemManagerDic["Extenstions"];
                var queueManagers = fileSystemManagerDic["QueueManagers"];
                var publisher = fileSystemManagerDic["Publisher"];
                _fileSystemManager.Add(fileSystemManager.Key, (path, waitIntervalInMilliSeconds, extenstions, queueManagers, publisher));
            }
        }

        public static void ActivateMonitor()
        {
            foreach (var fileSystemManager in _fileSystemManager)
            {
                if (Directory.Exists(fileSystemManager.Value.Path))
                {
                    var timer = new System.Timers.Timer(fileSystemManager.Value.WaitIntervalInMilliSeconds);
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
        private static void Processor(object? obj, ElapsedEventArgs eventArgs, (string Path, long WaitIntervalInMilliSeconds, string Extenstions, string QueueManagers, string Publisher) fileSystem)
        {
            var files = Directory.EnumerateFiles(fileSystem.Path);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                if (string.IsNullOrEmpty(fileSystem.Extenstions) && !fileSystem.Extenstions.Split(";").Contains(fileInfo.Extension))
                {
                    continue;
                }
                var publisher = GetRMQPublisher(fileSystem.QueueManagers,fileSystem.Publisher);
                if(publisher!= null)
                {
                    publisher.BindQueue();
                    publisher.Publish(File.ReadAllBytes(file));
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
