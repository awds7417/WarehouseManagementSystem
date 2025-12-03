using System;
using System.Collections.Generic;

namespace WarehouseManagementSystem.Models
{
    public class ProductListViewModel
    {
        public IList<Product> Items { get; set; }

        public string Keyword { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        // 方便 View 用
        public int TotalPages
        {
            get
            {
                if (PageSize <= 0) return 0;
                return (int)Math.Ceiling((double)TotalCount / (double)PageSize);
            }
        }
    }
}
