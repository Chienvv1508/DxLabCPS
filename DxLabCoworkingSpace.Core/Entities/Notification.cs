﻿using System.ComponentModel.DataAnnotations;

namespace DxLabCoworkingSpace
{
    public partial class Notification
    {
        public int NotificationId { get; set; }
        [Required(ErrorMessage = "Message không được để trống.")]
        [StringLength(500, ErrorMessage = "Message không được vượt quá 500 ký tự.")]
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool Status { get; set; }    
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
