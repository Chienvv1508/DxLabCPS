using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DxLabCoworkingSpace
{
    public class AreaTypeCategoryForAddDTO
    {
       
        public string Title { get; set; }
        public string CategoryDescription { get; set; }
        public List<IFormFile> Images { get; set; }

        public int Status { get; set; }
    }
}
