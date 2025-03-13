using System;
using System.Collections.Generic;

namespace  DxLabCoworkingSpace

{
    public partial class Blog
    {
        public Blog()
        {
            Images = new HashSet<Image>();
        }

        public int BlogId { get; set; }
        public int? UserId { get; set; }
        public string BlogTitle { get; set; } = null!;
        public string BlogContent { get; set; } = null!;
        public DateTime BlogCreatedDate { get; set; }
        public int Status { get; set; }
        public virtual User? User { get; set; }
        public virtual ICollection<Image> Images { get; set; }
    }
}
