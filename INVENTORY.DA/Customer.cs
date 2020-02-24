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
    
    public partial class Customer
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Customer()
        {
            this.CashCollections = new HashSet<CashCollection>();
            this.CreditSales = new HashSet<CreditSale>();
            this.Returns = new HashSet<Return>();
            this.SOrders = new HashSet<SOrder>();
        }
    
        public int CustomerID { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string FName { get; set; }
        public string CompanyName { get; set; }
        public string ContactNo { get; set; }
        public string EmailID { get; set; }
        public string NID { get; set; }
        public string Address { get; set; }
        public string PhotoPath { get; set; }
        public decimal TotalDue { get; set; }
        public string RefName { get; set; }
        public string RefContact { get; set; }
        public string RefFName { get; set; }
        public string RefAddress { get; set; }
        public int CustomerType { get; set; }
        public decimal OpeningDue { get; set; }
        public Nullable<System.DateTime> CreateDate { get; set; }
        public int CreatedBy { get; set; }
        public decimal CreditDue { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CashCollection> CashCollections { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<CreditSale> CreditSales { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Return> Returns { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<SOrder> SOrders { get; set; }
    }
}