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
    
    public partial class SystemInformation
    {
        public int SystemInfoID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string TelephoneNo { get; set; }
        public string EmailAddress { get; set; }
        public string WebAddress { get; set; }
        public Nullable<System.DateTime> SystemStartDate { get; set; }
        public string ProductPhotoPath { get; set; }
        public string SupplierPhotoPath { get; set; }
        public string CustomerPhotoPath { get; set; }
        public string CustomerNIDPatht { get; set; }
        public string SupplierDocPath { get; set; }
        public string EmployeePhotoPath { get; set; }
        public int SMSServiceEnable { get; set; }
        public string InsuranceContactNo { get; set; }
    }
}