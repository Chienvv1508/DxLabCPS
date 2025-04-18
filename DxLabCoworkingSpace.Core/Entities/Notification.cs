namespace DxLabCoworkingSpace
{
    public partial class Notification
    {
        public int NotificationId { get; set; }
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int? UserId { get; set; }
        public virtual User? User { get; set; }
    }
}
