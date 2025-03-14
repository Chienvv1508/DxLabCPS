using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class SlotGenerationRequest
    {
        public string? StartTime { get; set; }
        public string? EndTime { get; set; }
        public int? BreakTime { get; set; }
    }
}
