using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public class SumaryExpense
    {
        [Key]
        public long SumaryExpenseId { get; set; }
        public DateTime SumaryDate { get; set; }
        public decimal Amout { get; set; }

        public int FaciCategory { get; set; }   

    }
}
