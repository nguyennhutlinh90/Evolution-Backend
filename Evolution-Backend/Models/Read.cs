using System.Collections.Generic;

namespace Evolution_Backend.Models
{
    public class FilterType
    {
        public const string Equal = "$eq";
        public const string NotEqual = "$neq";
        public const string Contain = "$in";
        public const string NotContain = "$nin";
        public const string LikeWith = "$lw";
        public const string StartWith = "$sw";
        public const string EndWith = "$ew";
        public const string Between = "$bt";
        public const string GreatThan = "$gt";
        public const string GreatThanOrEqual = "$gte";
        public const string LessThan = "$lt";
        public const string LessThanOrEqual = "$lte";
    }

    public class FilterRequest
    {
        public List<string> Names { get; set; } = new List<string>();

        public object Value { get; set; }

        public string Type { get; set; } = FilterType.Equal;
    }

    public class SortRequest
    {
        public string Name { get; set; }

        public string Type { get; set; } = "asc";
    }

    public class ReadRequest
    {
        public List<FilterRequest> Filters { get; set; } = new List<FilterRequest>();

        public List<SortRequest> Sorts { get; set; } = new List<SortRequest>();

        public int PageLimit { get; set; } = int.MaxValue;

        public int PageSkip { get; set; } = 0;
    }

    public class ReadResponse<T>
    {
        public List<T> Datas { get; set; }

        public long Total { get; set; }
    }
}
