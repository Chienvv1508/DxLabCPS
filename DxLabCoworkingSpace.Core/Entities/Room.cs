using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Room
    {
        public Room()
        {
            Areas = new HashSet<Area>();
            Images = new HashSet<Image>();
        }

        public int RoomId { get; set; }
        public string RoomName { get; set; } = null!;
        public string? RoomDescription { get; set; }
        public int Capacity { get; set; }
        // 0: Chưa ss, 1: SS, 2: Xóa
        public int Status { get; set; }

        public virtual ICollection<Area> Areas { get; set; }
        public virtual ICollection<Image> Images { get; set; }
    }
}
