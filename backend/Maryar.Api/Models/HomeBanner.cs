using System;

namespace Maryar.Api.Models
{
    public class HomeBanner
    {
        public Guid Id { get; set; }

        public string BackgroundImageUrl { get; set; }
        public string ImageLink { get; set; }

        public string Eyebrow { get; set; }
        public string Title { get; set; }
        public string TitleEmphasis { get; set; }
        public string Description { get; set; }

        public string PrimaryButtonText { get; set; }
        public string PrimaryButtonUrl { get; set; }
        public string SecondaryButtonText { get; set; }
        public string SecondaryButtonUrl { get; set; }

        public int SortOrder { get; set; }
        public bool Active { get; set; }
        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
