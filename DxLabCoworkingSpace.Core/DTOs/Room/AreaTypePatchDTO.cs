using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaTypePatchDTO
    {
        public string AreaTypeName { get; set; }
        public string AreaDescription { get; set; }
        public decimal Price { get; set; }
        public List<string> Images { get; set; }
    }
}
