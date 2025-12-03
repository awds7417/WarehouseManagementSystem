using System;
using System.Collections.Generic;

namespace WarehouseManagementSystem.Models
{
    public class InventoryStatusListViewModel
    {
        public IList<InventorySummary> Items { get; set; }

        public int PageIndex { get; set; }

        public int PageSize { get; set; }

        public int TotalCount { get; set; }

        public int TotalPages
        {
            get
            {
                if (PageSize <= 0)
                    return 0;

                return (int)Math.Ceiling((double)TotalCount / (double)PageSize);
            }
        }
    }
}
