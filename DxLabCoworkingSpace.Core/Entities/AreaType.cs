using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DxLabCoworkingSpace
{
    public partial class AreaType
    {
        public AreaType()
        {
            Areas = new HashSet<Area>();
            Images = new HashSet<Image>();
        }

        public int AreaTypeId { get; set; }
        public string AreaTypeName { get; set; } = null!;
        public int AreaCategory { get; set; }
        public string AreaDescription { get; set; } = null!;
        public int Size { get; set; }
        public decimal Price { get; set; }
        public bool IsDeleted { get; set; }
        [JsonIgnore]
        public virtual ICollection<Area> Areas { get; set; }

        public virtual AreaTypeCategory AreaTypeCategory { get; set; }
        public virtual ICollection<Image> Images { get; set; }

       
    }
}
