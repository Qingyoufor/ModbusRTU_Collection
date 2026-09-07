namespace Server.Models.Dtos
{
    public class SystemLogsDto
    {
        public string FileName { get; set; } = string.Empty;
        public List<string> Lines { get; set; } = new();
    }
}