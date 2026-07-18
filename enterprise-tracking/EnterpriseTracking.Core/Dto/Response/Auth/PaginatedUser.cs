namespace EnterpriseTracking.Core.Dto.Response.Auth
{
    public class PaginatedUser
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<DisplayFindUserDTO> Data { get; set; }
        public int TotalUserCount { get; set; }
    }
}