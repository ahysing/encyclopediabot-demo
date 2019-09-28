using System;
using System.Collections.Generic;

namespace EncyclopediaBot.Logic.Snl
{
    public class Article
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string SubjectUrl { get; set; }
        public string SubjectTitle { get; set; }
        public string XhtmlBody { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ChangedAt { get; set; }
        public string LicenseName { get; set; }
        public string MetadataLicenseName { get; set; }
        public Metadata Metadata { get; set; }
        public List<Author> Authors { get; set; }
        public List<Image> Images { get; set; }
    }
}