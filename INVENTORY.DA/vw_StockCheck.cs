//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace INVENTORY.DA
{
    using System;
    using System.Collections.Generic;
    
    public partial class vw_StockCheck
    {
        public int StockID { get; set; }
        public int ProductID { get; set; }
        public int ColorID { get; set; }
        public string Code { get; set; }
        public string ProductName { get; set; }
        public string ColorCode { get; set; }
        public decimal Purchase { get; set; }
        public decimal TotalSales { get; set; }
        public decimal CreditSales { get; set; }
        public decimal Salereturns { get; set; }
        public decimal Purchasereturns { get; set; }
        public Nullable<decimal> SystemStock { get; set; }
        public Nullable<decimal> StockShouldBe { get; set; }
        public string Remarks { get; set; }
    }
}
