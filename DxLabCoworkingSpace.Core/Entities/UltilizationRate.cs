using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class UltilizationRate
    {
        public long UltilizationRateId { get; set; }
        public DateTime THDate { get; set; }
        public int RoomId { get; set; }
        public string RoomName { get; set; }
        public int AreaId { get; set; }
        public string AreaName { get; set; }
        public int AreatypeId { get; set; }
        public string AreaTypeName { get; set; }

        public int AreaTypeCategoryId { get; set; }
        public string AreaTypeCategoryTitle { get; set; }

        public decimal Rate { get; set; }


    }
}
