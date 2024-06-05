using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace WebApplication1.Models
{

    public class User
    {
        public User()
        {

        }

        [Key]
        public int? user_id { get; set; }
        public string? userName { get; set; }
        public string? password { get; set; }
        public string? email { get; set; }
        public string? role { get; set; }
    }


}
