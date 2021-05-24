namespace DdlWarezNotifier
{

    public class DdlWarezNotifierSettings
    {
        public string Queries { get; set; }
        public int Interval { get; set; }
        public TelegramSettings Telegram { get; set; }
    }

    public class TelegramSettings
    {
        public string ApiToken { get; set; }
        public int ChatId { get; set; }
    }
}