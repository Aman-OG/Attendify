using System;

namespace Attendify.Models
{
    public class PaginatedResponse<T>
    {
        public T[] Data { get; set; } = Array.Empty<T>();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
