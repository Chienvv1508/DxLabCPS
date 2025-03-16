using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DxLabCoworkingSpace
{
    public class AreaType
    {
        public AreaType()
        {
            Area = new HashSet<Area>();
            Images = new HashSet<Image>();

        }
        public int AreaTypeId { get; set; }
        public string AreaName { get; set; } = null!;
        public int AreaCategory { get; set; }
        public string AreaDescription { get; set; }
        public int Size { get; set; }
        public decimal Price { get; set; }
        public bool IsDeleted { get; set; }

        public virtual ICollection<Area> Area { get; set; }
        public virtual ICollection<Image> Images { get; set; }
    }
}
