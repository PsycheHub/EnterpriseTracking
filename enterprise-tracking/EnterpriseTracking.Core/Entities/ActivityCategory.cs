namespace EnterpriseTracking.Core.Entities
{
    public class ActivityCategory : BaseEntity
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public IEnumerable<MapAppCatories> MappAppCategories { get; set; }
    }
}
