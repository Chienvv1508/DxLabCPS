using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaTypeCategory
    {
        public AreaTypeCategory() {

            AreaTypes = new HashSet<AreaType>();
            Images = new HashSet<Image>();

        }
        [Key]
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string CategoryDescription { get; set; }
        public int Status { get; set; }
        public virtual ICollection<Image> Images { get; set; }
        public virtual ICollection<AreaType> AreaTypes { get; set; }


    }
}
