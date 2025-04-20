using AutoMapper;
using DxLabCoworkingSpaceForService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DxLabCoworkingSpace
{
    public static class GenerateMapper
    {
        public static IMapper GenerateMapperForService()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new AutoMapperProfile());  
            });

            IMapper mapper = config.CreateMapper();
            return mapper;
        }
    }
}
