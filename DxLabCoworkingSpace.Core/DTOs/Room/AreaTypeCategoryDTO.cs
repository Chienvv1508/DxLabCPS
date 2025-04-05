using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaTypeCategoryDTO
    {
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public string CategoryDescription { get; set; }
        public List<string> Images { get; set; }

        public int Status { get; set; }



    }
}
