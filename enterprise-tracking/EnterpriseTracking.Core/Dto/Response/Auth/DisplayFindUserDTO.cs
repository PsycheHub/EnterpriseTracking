namespace EnterpriseTracking.Core.Dto.Response.Auth
{
    public class DisplayFindUserDTO
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
      
     
        public string Email { get; set; }
     
        public string UserName { get; set; }
        public string Status { get; set; }
     
      


        public DateTime? LastLoginTime { get; set; } 
        public bool IsSuspend { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeleteDate { get; set; }
   
        public DateTime Created { get; set; }
    }
}