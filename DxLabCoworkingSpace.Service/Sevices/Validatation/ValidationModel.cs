using Nethereum.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public static class ValidationModel<T> where T : class
    {
        public static Tuple<bool, string> ValidateModel(T model)
        {
            var context = new ValidationContext(model, null, null);
            var results = new List<ValidationResult>();
            bool isValid = Validator.TryValidateObject(model, context, results, true);

            if (!isValid)
            {
                string errors = string.Join(" | ", results.Select(r => r.ErrorMessage));
                return new Tuple<bool, string>(false, errors);
            }
            return new Tuple<bool, string>(true, "");
        }
    }
}
