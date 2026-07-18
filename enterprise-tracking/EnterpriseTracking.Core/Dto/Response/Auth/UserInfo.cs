namespace EnterpriseTracking.Core.Dto.Response.Auth
{
    public class UserInfo
    {
        public string Id { get; set; }
      
      
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
      
        public string UserName { get; set; }
     
     
        public bool IsSuspendUser { get; set; }
    
   
        public DateTime? LastLoginTime { get; set; } 
     
       
        public IList<string>? UserRole { get; set; }
     

        public DateTime Created { get; set; }

    }
}
