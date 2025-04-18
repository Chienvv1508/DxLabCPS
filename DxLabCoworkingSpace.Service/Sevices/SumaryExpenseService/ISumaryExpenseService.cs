using DxLabCoworkingSpace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface ISumaryExpenseService : IGenericeService<SumaryExpense>
    {
        Task Add(List<SumaryExpense> sumaryExpenses);
    }
}
