using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCClubBooking.Core.Models
{
    public enum UserRole { Client, Admin }

    public class User
    {
        public int Id { get; set; }
        public string Login { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string Name { get; set; } = "";
        public string Phone { get; set; } = "";
        public UserRole Role { get; set; }
        public bool IsBlocked { get; set; }
    }
}
