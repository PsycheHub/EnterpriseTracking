namespace EnterpriseTracking.Core.Dto.Response
{
    public class ResponseDto<T>
    {
        public int StatusCode { get; set; }
        public string DisplayMessage { get; set; } = string.Empty;
        public T? Result { get; set; }
        public List<string>? ErrorMessages { get; set; }
    }
}
