using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IAreaTypeCategoryService : IGenericeService<AreaTypeCategory>
    {

        Task UpdateImage(AreaTypeCategory areaTypeCateFromDb, List<string> images);

    }
}
