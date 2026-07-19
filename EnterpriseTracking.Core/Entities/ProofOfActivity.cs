using Microsoft.EntityFrameworkCore;

namespace EnterpriseTracking.Core.Entities
{
    [Index(nameof(DocIdentifier))]
    public class ProofOfActivity: BaseEntity
    {

        public string DocIdentifier { get; set; }
        public string? FileName { get; set; }
        public string? Url { get; set; }
     
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }
        public string MapCategoryId { get; set; }
        public MapAppCatories MapCategory { get; set; }
        public string AppName { get; set; }

        public string? ContentType { get; set; }
        public byte[]? ImageData { get; set; }
    }
}
