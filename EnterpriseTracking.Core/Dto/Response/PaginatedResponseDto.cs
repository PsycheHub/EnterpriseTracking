namespace EnterpriseTracking.Core.Dto.Response
{
    public class PaginatedResponseDto<T>
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public IEnumerable<T> Data { get; set; }
        public int TotalCount { get; set; }
    }


}
