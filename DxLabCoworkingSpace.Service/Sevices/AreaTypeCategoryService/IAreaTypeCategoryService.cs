using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
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
        Task<ResponseDTO<AreaTypeCategoryForAddDTO>> CreateNewAreaTypeCategoory(AreaTypeCategoryForAddDTO areaTypeCategoryDTO);
        Task<ResponseDTO<AreaTypeCategory>> PatchAreaTypeCategory(int id, JsonPatchDocument<AreaTypeCategory> patchDoc);
    }
}
