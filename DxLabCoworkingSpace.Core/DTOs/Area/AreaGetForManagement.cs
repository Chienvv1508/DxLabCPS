using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class AreaGetForManagement
    {
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public int AreaTypeId { get; set; }
        public string AreaTypeName { get; set; } = null!;
        public int CategoryId { get; set; }
        public string Title { get; set; }

        public int Status { get; set; }

        public int FaciAmount { get; set; }
        public int FaciAmountCh { get; set; }

        public int Size { get; set; }
    }
}
