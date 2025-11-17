using System;

namespace The_CMCS.Models
{
    public class SupportingDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ClaimId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; } = DateTime.Now;
        public string FilePath { get; set; }
        public byte[] FileData { get; set; }
        public DateTime UploadedDate { get; internal set; }
    }
}