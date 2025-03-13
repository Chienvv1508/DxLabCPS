using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace.Core.DTOs
{
    public class BlogDTO
    {
            public enum BlogStatus
            {
                Cancel = 0,
                Pending = 1,
                Approve = 2
            }

            public int BlogId { get; set; }
            public string BlogTitle { get; set; } = null!;
            public string BlogContent { get; set; } = null!;
            public DateTime BlogCreatedDate { get; set; }
            public BlogStatus Status { get; set; }  
            public string? UserName { get; set; }
            public List<String>? Images { get; set; } // Danh sách URL hoặc tên file 
    }
}
