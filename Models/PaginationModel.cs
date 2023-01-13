using System;
namespace Models
{
    public class PaginationModel
    {
        public PaginationModel()
        {
        }
        public int Page { get; set; }
        public int Take { get; set; }
        public int ItemCount { get; set; }
        public int PageCount { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
        //{
        //    "page": 0,
        //    "take": 0,
        //    "itemCount": 0,
        //    "pageCount": 0,
        //    "hasPreviousPage": true,
        //    "hasNextPage": true
        //}
    }
}

