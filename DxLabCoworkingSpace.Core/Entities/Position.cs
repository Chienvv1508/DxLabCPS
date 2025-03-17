using System;
using System.Collections.Generic;

namespace DxLabCoworkingSpace
{
    public partial class Position
    {
        public int PositionId { get; set; }
        public int? AreaId { get; set; }

        public int PositionNumber { get; set; }
        public int Status { get; set; }


        public virtual Area? Area { get; set; }
    }
}
