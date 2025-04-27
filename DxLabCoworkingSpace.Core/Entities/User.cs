using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class User
    {
        public User()
        {
            Blogs = new HashSet<Blog>();
            Bookings = new HashSet<Booking>();
            Notifications = new HashSet<Notification>();
            Reports = new HashSet<Report>();
        }

        public int UserId { get; set; }
        public int? RoleId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? WalletAddress { get; set; }
        public bool Status { get; set; }

        public virtual Role? Role { get; set; }
        public virtual ICollection<Blog> Blogs { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<Report> Reports { get; set; }
    }
}
