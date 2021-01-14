using System.Collections.Generic;

namespace Evolution_Backend.Models
{
    public class CountInfo
    {
        public string Name { get; set; }

        public List<FilterRequest> Filters { get; set; } = new List<FilterRequest>();
    }

    public class CountRequest
    {
        public List<FilterRequest> Filters { get; set; } = new List<FilterRequest>();

        public List<CountInfo> CountInfos { get; set; } = new List<CountInfo>();
    }
}
