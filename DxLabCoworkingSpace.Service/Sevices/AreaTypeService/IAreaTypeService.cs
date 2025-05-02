using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public interface IAreaTypeService : IGenericeService<AreaType>
    {
        Task<ResponseDTO<AreaType>> AddImages(int id, List<IFormFile> images);
        Task<ResponseDTO<AreaType>> CreateNewAreaType(AreaTypeForAddDTO areTypeDto);
        Task<ResponseDTO<List<AreaType>>> GetAllByFilPara(string fil);
        public Task<object> GetAreaTypeForAddRoom();
        Task<ResponseDTO<AreaType>> PatchAreaType(int id, JsonPatchDocument<AreaType> patchDoc);
        Task<ResponseDTO<AreaType>> RemoveImages(int id, List<string> images);
        Task UpdateImage(AreaType areaTypeFromDb, List<string> images);
    }
}
