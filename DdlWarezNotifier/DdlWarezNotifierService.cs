using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Telegram.Bot.Types;

namespace DdlWarezNotifier
{

    public class DdlWarezNotifierService : IHostedService, IDisposable
    {
        private readonly Telegram.Bot.TelegramBotClient bot;
        private readonly ChatId chat;
        private readonly List<string> _processedDownloads = new List<string>();
        private readonly ILogger<DdlWarezNotifierService> _logger;
        private readonly DdlWarezNotifierSettings settings;
        private const string DDL_WAREZ_URL = "https://ddl-warez.to/";
        private Timer _timer;

        public DdlWarezNotifierService(ILogger<DdlWarezNotifierService> logger, IOptions<DdlWarezNotifierSettings> settings)
        {
            _logger = logger;
            this.settings = settings.Value;
            bot = new Telegram.Bot.TelegramBotClient(settings.Value.Telegram.ApiToken);
            chat = new ChatId(settings.Value.Telegram.ChatId);
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            var intervalText = settings.Interval == 1 ? "minute" : settings.Interval + " minutes";
            _logger.LogInformation($"DDL Warez notifier service is running and will look for new downloads every {intervalText}");
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromMinutes(settings.Interval));
            _processedDownloads.AddRange(GetProcessedDownloadsFromFile());
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var queries = settings.Queries.Split(',');
            foreach (var query in queries)
            {
                if (!string.IsNullOrEmpty(query) && query.Length > 3)
                {
                    var htmlQuery = HttpUtility.UrlEncode(query);
                    var url = $"{DDL_WAREZ_URL}?search={htmlQuery}";
                    var web = new HtmlWeb();
                    var doc = web.Load(url);

                    var value = doc.DocumentNode
                        .SelectNodes("//td/a[1]");

                    if (value?.Count > 0)
                    {
                        var message = $"<b>New downloads are available for your query \"{query}\":</b>";
                        message += Environment.NewLine;

                        var newElements = false;
                        foreach (var node in value)
                        {
                            var elementTitle = node.InnerText.Trim();
                            var elementLink = node.Attributes["href"].Value.Trim();
                            if (!_processedDownloads.Contains(elementTitle) && !string.IsNullOrEmpty(elementTitle) && !string.IsNullOrEmpty(elementLink))
                            {
                                message += $"<a href=\"{DDL_WAREZ_URL}{elementLink}\">{elementTitle}</a>";
                                message += Environment.NewLine;
                                _processedDownloads.Add(elementTitle);
                                newElements = true;
                            }
                        }

                        if (newElements)
                        {
                            _logger.LogInformation($"There is at least one new download found for query \"{query}\"");
                            _logger.LogInformation("Try sending Telegram message...");
                            try
                            {
                                bot.SendTextMessageAsync(chat, message, Telegram.Bot.Types.Enums.ParseMode.Html).Wait();
                                WriteProcessedDownloadsToFile();
                                _logger.LogInformation("Successfully sent message");
                                _logger.LogTrace(message);
                            }
                            catch (Exception e)
                            {
                                _logger.LogInformation("Error sending message");
                                _logger.LogInformation(e.Message);
                            }

                        }
                        else
                        {
                            _logger.LogInformation($"There were no new downloads found for query \"{query}\". No need to send message.");
                        }
                    }

                }
            }
        }

        private const string PROCESSED_DOWNLOADS_FILE_NAME = "ProcessedDownloads.txt";
        private string[] GetProcessedDownloadsFromFile()
        {
            var existingProcessedDownloads = new string[0];
            if (System.IO.File.Exists(PROCESSED_DOWNLOADS_FILE_NAME))
            {
                existingProcessedDownloads = System.IO.File.ReadAllLines(PROCESSED_DOWNLOADS_FILE_NAME);
            }
            return existingProcessedDownloads;
        }
        private void WriteProcessedDownloadsToFile()
        {
            var existingProcessedDownloads = GetProcessedDownloadsFromFile();
            using (StreamWriter writer = System.IO.File.AppendText(PROCESSED_DOWNLOADS_FILE_NAME))
            {
                foreach (var element in _processedDownloads)
                {
                    if (!existingProcessedDownloads.Contains(element))
                    {
                        writer.WriteLine(element);
                    }
                }
            }
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("DDL Warez notifier service is stopping");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}