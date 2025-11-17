namespace The_CMCS.Models
{
    public class Report
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; }
        public string Type { get; set; } // "Invoice", "Summary", "Department"
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public string GeneratedBy { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public byte[]? PdfData { get; set; }
    }
}