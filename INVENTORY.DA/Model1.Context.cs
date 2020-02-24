﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class DEWSRMEntities : DbContext
    {
        public DEWSRMEntities()
            : base("name=DEWSRMEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Bank> Banks { get; set; }
        public virtual DbSet<BankTransaction> BankTransactions { get; set; }
        public virtual DbSet<Branch> Branches { get; set; }
        public virtual DbSet<CardType> CardTypes { get; set; }
        public virtual DbSet<CardTypeSetup> CardTypeSetups { get; set; }
        public virtual DbSet<CashCollection> CashCollections { get; set; }
        public virtual DbSet<Category> Categorys { get; set; }
        public virtual DbSet<Color> Colors { get; set; }
        public virtual DbSet<Company> Companies { get; set; }
        public virtual DbSet<CreditSaleProduct> CreditSaleProducts { get; set; }
        public virtual DbSet<CreditSale> CreditSales { get; set; }
        public virtual DbSet<Customer> Customers { get; set; }
        public virtual DbSet<DamageProduct> DamageProducts { get; set; }
        public virtual DbSet<Designation> Designations { get; set; }
        public virtual DbSet<Employee> Employees { get; set; }
        public virtual DbSet<EmpSalary> EmpSalaries { get; set; }
        public virtual DbSet<Expenditure> Expenditures { get; set; }
        public virtual DbSet<ExpenseItem> ExpenseItems { get; set; }
        public virtual DbSet<MenuPermission> MenuPermissions { get; set; }
        public virtual DbSet<Menu> Menus { get; set; }
        public virtual DbSet<Model> Models { get; set; }
        public virtual DbSet<MonthlyStock> MonthlyStocks { get; set; }
        public virtual DbSet<POProductDetail> POProductDetails { get; set; }
        public virtual DbSet<POrderDetail> POrderDetails { get; set; }
        public virtual DbSet<POrder> POrders { get; set; }
        public virtual DbSet<PrevBalance> PrevBalances { get; set; }
        public virtual DbSet<Product> Products { get; set; }
        public virtual DbSet<ReturnDetail> ReturnDetails { get; set; }
        public virtual DbSet<Return> Returns { get; set; }
        public virtual DbSet<Role> Roles { get; set; }
        public virtual DbSet<ShareInvestmentHead> ShareInvestmentHeads { get; set; }
        public virtual DbSet<ShareInvestment> ShareInvestments { get; set; }
        public virtual DbSet<SOrderDetail> SOrderDetails { get; set; }
        public virtual DbSet<SOrder> SOrders { get; set; }
        public virtual DbSet<StockDetail> StockDetails { get; set; }
        public virtual DbSet<Stock> Stocks { get; set; }
        public virtual DbSet<Supplier> Suppliers { get; set; }
        public virtual DbSet<SystemInformation> SystemInformations { get; set; }
        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<vw_CustomerLedger> vw_CustomerLedger { get; set; }
        public virtual DbSet<vw_StockCheck> vw_StockCheck { get; set; }
        public virtual DbSet<vw_SupplierLedger> vw_SupplierLedger { get; set; }
        public virtual DbSet<CreditSalesDetail> CreditSalesDetails { get; set; }
        public virtual DbSet<SMSFormate> SMSFormates { get; set; }
        public virtual DbSet<SMSStatus> SMSStatuses { get; set; }
        public virtual DbSet<Godown> Godowns { get; set; }
    }
}
