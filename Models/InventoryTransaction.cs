using System;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagementSystem.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? CustomerId { get; set; }

        [Required]
        [StringLength(3)]
        public string TxType { get; set; }   // "IN" or "OUT"

        [Required]
        public int Quantity { get; set; }

        [Required]
        public DateTime TxDate { get; set; }

        [StringLength(200)]
        public string Remark { get; set; }

        // 下面這幾個是為了畫面好看用，不直接對應資料表欄位
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
    }
}
