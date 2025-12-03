using System;
using System.Collections.Generic;

namespace WarehouseManagementSystem.Models
{
    public class CustomerListViewModel
    {
        public IList<Customer> Items { get; set; }

        // 之後如果要做搜尋，可以用這個欄位，現在不用也沒關係
        public string Keyword { get; set; }

        public int PageIndex { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }

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
