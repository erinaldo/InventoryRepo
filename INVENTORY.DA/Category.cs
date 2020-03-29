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
    
    public partial class Category
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Category()
        {
            this.Products = new HashSet<Product>();
        }
    
        public int CategoryID { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public string CategoryType { get; set; }
        public string Age { get; set; }
        public string BackColor { get; set; }
        public string ForeColor { get; set; }
        public bool Inactive { get; set; }
        public bool IsVat { get; set; }
        public decimal VAT { get; set; }
        public bool IsPayOut { get; set; }
        public bool IsSeperateSale { get; set; }
        public int OrderNo { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Product> Products { get; set; }
    }
}
