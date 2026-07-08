namespace RVSite.Models
{
    public class SitePhoto
    {
        public int SitePhotoID { get; set; }

        public int SiteID { get; set; }
        public Site Site { get; set; }

        public string FilePath { get; set; }
        public string Caption { get; set; }
        public int SortOrder { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }
}
