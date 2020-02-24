﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using INVENTORY.DA;
using Microsoft.Reporting.WinForms;
using System.Security.Cryptography;


namespace INVENTORY.UI
{
    public partial class FCreditSale : Form
    {

        Product _FromControlProduct = null;
        Customer _FromControlCustomer = null;
        CreditSale _CreditSale = null;
        CreditSale _CreditSaleUnExpected = null;
        CreditSaleProduct _creditSaleProduct = null;
        List<CreditSaleProduct> _creditSaleProducts = new List<CreditSaleProduct>();
        CreditSale _CreditSaleSave = new CreditSale();
        decimal _prodPrice = 0;
        decimal TotalOrderQTY = 0;
        Stock _stock = null;
        Customer _Customer = null;
        CreditSalesDetail _oCSDetails = null;
        StockDetail _StockDetail = null;
        Product _oProduct = null;
        decimal _sum_remaning = 0;
        decimal _MergeTotalAmount = 0;
        decimal _sum_HireValue = 0;
        double _ratio = 1;

        List<DeleteCrediSalesDetails> DeleteCreditsalesDetailsList = new List<DeleteCrediSalesDetails>();


        List<CreditSale> _CreditSaleList = null;

        private bool _bCanceld = true;
        private bool _IsSaved = false;

        List<Customer> _CustomerList = null;
        List<Product> _ProductList = null;
        DEWSRMEntities db = null;
        List<CreditSalesDetail> _CreditSalesDetailforDelete = null;
        string sAgreement = string.Empty;
        private decimal _prevOrderdue = 0;
        bool _isPaidUnexpected = false;
        bool ForNewCustomer = false;



        public FCreditSale()
        {
            InitializeComponent();
        }

        public bool ShowDlg(CreditSale creditSale, bool IsSave, string OnlyPaid)
        {
            sAgreement = OnlyPaid;
            db = new DEWSRMEntities();
            if (creditSale.CreditSalesID > 0)
            {
                _CreditSale = db.CreditSales.FirstOrDefault(o => o.CreditSalesID == creditSale.CreditSalesID);
                _prevOrderdue = (decimal)_CreditSale.Remaining;

            }
            else
            {
                _CreditSale = new CreditSale();
            }


            if (creditSale.ISWeeekly != 0)
            {
                chkWeekly.Checked = true;

            }
            else
            {
                chkWeekly.Checked = false;

            }

            _IsSaved = IsSave;
            PopulateBankCombo();
            if (IsSave)
            {
                this.Text = "New Credit Sales.";
                RefresValue();
                btnCalculate.Enabled = true;
                btnSave.Enabled = false;
            }
            else
            {
                _CustomerList = db.Customers.ToList();
                _ProductList = db.Products.ToList();

                if (OnlyPaid == "OnlyPaid")
                {
                    this.Text = "Only Paid Credit Sales.";
                    btnSave.Enabled = false;
                    btnCalculate.Enabled = false;
                    btnPaid.Enabled = true;
                    groupBox1.Enabled = false;
                    //groupBox2.Enabled = false;
                    //groupBox3.Enabled = false;
                    ManageControlOnlyPaid();
                    groupBox7.Enabled = false;
                    //groupBox10.Enabled = true;
                    btnPaid.Focus();
                    chkIsAllPaid.Enabled = true;
                    CreditSalesDetail dtail = _CreditSale.CreditSalesDetails.FirstOrDefault(o => o.PaymentStatus == "Due");
                    numPayment.Value = dtail != null ? (decimal)dtail.InstallmentAmt : 0;
                    numPaymentNew.Value = numPayment.Value;
                    RefresValueAfterEdit();

                }
                else if (OnlyPaid == "Agreement")
                {
                    this.Text = "Agreement Invoice.";
                    chkUnExpected.Text = "Is Agreement";
                    btnPaid.Enabled = false;
                    btnCalculate.Enabled = true;
                    btnSave.Enabled = false;
                    RefresValueAfterEdit();
                }
                else
                {
                    this.Text = "Edit Credit Sales.";
                    btnSave.Text = "Update";
                    btnPaid.Enabled = false;
                    btnCalculate.Enabled = true;
                    btnSave.Enabled = false;
                    _CreditSale.ISUnexpected = true;
                    RefresValueAfterEdit();
                }
            }

            this.ShowDialog();
            return !_bCanceld;
        }
        private void ManageControlOnlyPaid()
        {
            txtGrandTotalAmt.Enabled = false;
            numDownPayment.Enabled = false;
            txtDiscount.Enabled = false;
            txtRemaining.Enabled = false;
            txtNetAmount.Enabled = false;
            dtpSalesDate.Enabled = false;
            chkIsAllPaid.Enabled = false;
            numEXTInterestAmt.Enabled = true;
            numExtTimeInterestRate.Enabled = true;
            btnIncreaseInstall.Visible = true;
            numCashDownPayment.Enabled = false;
        }

        private void PopulateBankCombo()
        {
            using (DEWSRMEntities db = new DEWSRMEntities())
            {
                DataTable dt = new DataTable();
                dt.Columns.Add("BankID", typeof(int));
                dt.Columns.Add("BankName", typeof(string));
                DataRow row = null;
                var banks = db.Banks.ToList();

                row = dt.NewRow();
                row["BankID"] = 0;
                row["BankName"] = "--Select Bank--";
                dt.Rows.Add(row);

                foreach (var item in banks)
                {
                    row = dt.NewRow();
                    row["BankID"] = item.BankID;
                    row["BankName"] = item.BankName + " (" + item.AccountNo + ")";
                    dt.Rows.Add(row);

                }
                cmbBank.DisplayMember = "BankName";
                cmbBank.ValueMember = "BankID";
                cmbBank.DataSource = dt;
            }
        }
        private void cmbBank_SelectedIndexChanged(object sender, EventArgs e)
        {
            PopulateCardTypeCombo();
        }

        private void PopulateCardTypeCombo()
        {
            int BankID = Convert.ToInt32(cmbBank.SelectedValue);
            if (BankID > 0)
            {
                using (DEWSRMEntities db = new DEWSRMEntities())
                {
                    DataTable dt = new DataTable();
                    dt.Columns.Add("CardTypeSetupID", typeof(int));
                    dt.Columns.Add("CardName", typeof(string));
                    DataRow row = null;
                    var cards = (from cts in db.CardTypeSetups.Where(x => x.BankID == (int)cmbBank.SelectedValue)
                                 join ct in db.CardTypes on cts.CardTypeID equals ct.CardTypeID
                                 where cts.BankID == BankID && ct.Status == 1
                                 select new
                                 {
                                     cts.CardTypeSetupID,
                                     ct.Description,
                                     ct.Sequence
                                 }).OrderBy(i => i.Sequence).ToList();
                    foreach (var item in cards)
                    {
                        row = dt.NewRow();
                        row["CardTypeSetupID"] = item.CardTypeSetupID;
                        row["CardName"] = item.Description;
                        dt.Rows.Add(row);
                    }
                    cmbCardType.DisplayMember = "CardName";
                    cmbCardType.ValueMember = "CardTypeSetupID";
                    cmbCardType.DataSource = dt;
                }
            }
        }
        private void RefresValue()
        {
            // txtQuantity.Text = _CreditSale.Quantity.ToString();
            txtGrandTotalAmt.Text = _CreditSale.TSalesAmt.ToString();
            txtInterestRate.Text = _CreditSale.InterestRate.ToString();
            txtNoOfInstallment.Text = _CreditSale.NoOfInstallment.ToString();
            numDownPayment.Value = _CreditSale.DownPayment;
            txtDiscount.Text = _CreditSale.Discount.ToString();
            chkUnExpected.Checked = _CreditSale.ISUnexpected != null ? (bool)_CreditSale.ISUnexpected : false;
            txtNetAmount.Text = _CreditSale.NetAmount.ToString();
            dtpIssueDate.Text = _CreditSale.IssueDate != DateTime.MinValue ? _CreditSale.IssueDate.ToString() : DateTime.Now.ToString();
            dtpSalesDate.Text = _CreditSale.SalesDate != DateTime.MinValue ? _CreditSale.SalesDate.ToString() : DateTime.Now.ToString();
            txtRemaining.Text = _CreditSale.Remaining.ToString();
            txtFixedAmt.Text = _CreditSale.FixedAmt.ToString();
            txtVoucherNo.Text = _CreditSale.InvoiceNo;
            txtUnExpectedIns.Text = _CreditSale.UnExInstallment.ToString();
            if (_CreditSale.ISWeeekly != 0)
            {
                chkWeekly.Checked = true;
            }
            else
            {
                chkWeekly.Checked = false;
            }
        }

        private void RefresValueAfterEdit()
        {

            Customer oCus = _CustomerList.FirstOrDefault(d => d.CustomerID == _CreditSale.CustomerID);
            _FromControlCustomer = oCus;

            if (oCus != null)
            {
                ctlCustomer.SelectedID = oCus.CustomerID;
                ctlCustomer.Enabled = false;
            }

            //chkStatus.Checked = !_CreditSale.Status;
            txtGrandTotalAmt.Text = _CreditSale.TSalesAmt.ToString();
            txtInterestRate.Text = _CreditSale.InterestRate.ToString();
            txtNoOfInstallment.Text = _CreditSale.NoOfInstallment.ToString();
            numDownPayment.Value = _CreditSale.DownPayment;
            txtFixedAmt.Text = _CreditSale.FixedAmt.ToString();
            numEXTInterestAmt.Value = _CreditSale.FixedAmt;
            txtDiscount.Text = _CreditSale.Discount.ToString();
            txtNetAmount.Text = _CreditSale.NetAmount.ToString();
            txtRemaining.Text = _CreditSale.Remaining.ToString();
            dtpIssueDate.Text = _CreditSale.IssueDate.ToString();
            dtpSalesDate.Text = _CreditSale.SalesDate.ToString();
            txtVoucherNo.Text = _CreditSale.InvoiceNo;
            txtUnExpectedIns.Text = _CreditSale.UnExInstallment.ToString();
            chkUnExpected.Checked = _CreditSale.ISUnexpected != null ? (bool)_CreditSale.ISUnexpected : false;
            txtRemarks.Text = _CreditSale.Remarks != null ? _CreditSale.Remarks : "";

            if (_CreditSale.Status == 1)
            {
                rbIsDownPayment.Checked = true;
            }
            else if (_CreditSale.Status == 2)
            {
                rbFlateAmount.Checked = true;
            }
            RefreshGrid();
            RefreshScheduleGrid();
            //RefreshList();

        }

        private void RefreshGrid()
        {
            try
            {
                int count = 0;
                int nSLNo = 1;
                dgProducts.Rows.Clear();

                _creditSaleProducts = _CreditSale.CreditSaleProducts.ToList();
                List<CreditSaleProduct> CreditSaleNobarcodeProductList = new List<CreditSaleProduct>();
                if (_creditSaleProducts.Count > 0)
                {
                    Product oProduct = null;
                    INVENTORY.DA.Category oCategory = null;
                    INVENTORY.DA.Color oColInfo = null;
                    foreach (CreditSaleProduct oPODItem in _creditSaleProducts)
                    {
                        dgProducts.Rows.Add();
                        oProduct = db.Products.FirstOrDefault(o => o.ProductID == oPODItem.ProductID);
                        oColInfo = db.Colors.FirstOrDefault(c => c.ColorID == oPODItem.ColorInfoID);
                        oCategory = db.Categorys.FirstOrDefault(c => c.CategoryID == oProduct.CategoryID);


                        if (oProduct.ProductType != (int)EnumProductType.NoBarCode)
                        {
                            dgProducts.Rows[count].Cells[0].Value = nSLNo.ToString();

                            if (oProduct != null)
                            {
                                dgProducts.Rows[count].Cells[1].Value = oProduct.ProductName;
                                dgProducts.Rows[count].Cells[2].Value = oCategory.Description;

                            }

                            dgProducts.Rows[count].Cells[3].Value = oPODItem.Quantity.ToString();
                            dgProducts.Rows[count].Cells[4].Value = ((decimal)oPODItem.UnitPrice).ToString("F");
                            dgProducts.Rows[count].Cells[5].Value = ((decimal)oPODItem.TotalInterest).ToString("F");
                            dgProducts.Rows[count].Cells[6].Value = ((decimal)oPODItem.UTAmount).ToString("F");
                            dgProducts.Rows[count].Tag = oPODItem;
                            count++;
                            nSLNo++;
                        }
                        else
                        {
                            CreditSaleNobarcodeProductList.Add(oPODItem);
                        }
                    }

                    if (CreditSaleNobarcodeProductList.Count != 0)
                    {
                        var nobarcodesd = from sod in CreditSaleNobarcodeProductList
                                          join sd in db.StockDetails on sod.StockDetailID equals sd.SDetailID
                                          group sod by new { sod.ProductID, sd.ColorID } into g
                                          select new CreditSaleProduct
                                          {
                                              ProductID = g.Key.ProductID,
                                              ColorInfoID = g.Key.ColorID,
                                              CompressorWarrenty = g.FirstOrDefault().CompressorWarrenty,
                                              MotorWarrenty = g.FirstOrDefault().MotorWarrenty,
                                              PanelWarrenty = g.FirstOrDefault().PanelWarrenty,
                                              SparePartsWarrenty = g.FirstOrDefault().SparePartsWarrenty,
                                              ServiceWarrenty = g.FirstOrDefault().ServiceWarrenty,
                                              Quantity = g.Sum(i => i.Quantity),
                                              UnitPrice = g.FirstOrDefault().UnitPrice,
                                              UTAmount = g.Sum(i => i.UTAmount),
                                              TotalInterest = g.Sum(i => i.TotalInterest),
                                              StockDetailID = g.FirstOrDefault().StockDetailID
                                          };

                        foreach (var oPODItem in nobarcodesd)
                        {
                            dgProducts.Rows.Add();
                            oProduct = db.Products.FirstOrDefault(o => o.ProductID == oPODItem.ProductID);
                            _StockDetail = db.StockDetails.FirstOrDefault(x => x.SDetailID == oPODItem.StockDetailID);
                            oColInfo = db.Colors.FirstOrDefault(c => c.ColorID == oPODItem.ColorInfoID);
                            oCategory = db.Categorys.FirstOrDefault(c => c.CategoryID == oProduct.CategoryID);


                            dgProducts.Rows[count].Cells[0].Value = nSLNo.ToString();

                            if (oProduct != null)
                            {
                                dgProducts.Rows[count].Cells[1].Value = oProduct.ProductName;
                                dgProducts.Rows[count].Cells[2].Value = oCategory.Description;

                            }

                            dgProducts.Rows[count].Cells[3].Value = oPODItem.Quantity.ToString();
                            dgProducts.Rows[count].Cells[4].Value = ((decimal)oPODItem.UnitPrice).ToString("F");
                            dgProducts.Rows[count].Cells[5].Value = oPODItem.TotalInterest.ToString("F");
                            dgProducts.Rows[count].Cells[6].Value = ((decimal)oPODItem.UTAmount).ToString("F");
                            dgProducts.Rows[count].Tag = oPODItem;
                            count++;
                            nSLNo++;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void RefreshScheduleGrid()
        {
            int count = 0;
            dgvCreditSales.Rows.Clear();
            List<CreditSalesDetail> CSD = new List<CreditSalesDetail>();
            if (_CreditSale.CreditSalesDetails.Count > 0)
                CSD = _CreditSale.CreditSalesDetails.OrderBy(o => o.ScheduleNo).ToList();
            bool flag = false;
            foreach (CreditSalesDetail oCSD in CSD)
            {
                dgvCreditSales.Rows.Add();

                dgvCreditSales.Rows[count].Cells[0].Value = ((DateTime)oCSD.MonthDate).ToString("dd MMM yyyy");
                dgvCreditSales.Rows[count].Cells[1].Value = oCSD.PaymentStatus == "Paid" ? ((DateTime)oCSD.PaymentDate).ToString("dd MMM yyyy") : ((DateTime)oCSD.MonthDate).ToString("dd MMM yyyy");
                dgvCreditSales.Rows[count].Cells[2].Value = Math.Round((decimal)oCSD.Balance).ToString();
                dgvCreditSales.Rows[count].Cells[3].Value = Math.Round((decimal)oCSD.NetValue, 2).ToString();
                dgvCreditSales.Rows[count].Cells[4].Value = Math.Round((decimal)oCSD.HireValue, 2).ToString();
                dgvCreditSales.Rows[count].Cells[5].Value = Math.Round((decimal)oCSD.InstallmentAmt).ToString();

                dgvCreditSales.Rows[count].Cells[6].Value = Math.Round((decimal)oCSD.ClosingBalance).ToString();
                dgvCreditSales.Rows[count].Cells[7].Value = oCSD.PaymentStatus;
                dgvCreditSales.Rows[count].Cells[8].Value = oCSD.Remarks;




                dgvCreditSales.Rows[count].Tag = oCSD;
                if (oCSD.PaymentStatus == "Paid")
                {
                    dgvCreditSales.Rows[count].ReadOnly = true;
                }
                else if (!flag)
                {
                    dgvCreditSales.Rows[count].Selected = true;
                    flag = true;

                }
                count++;
            }
        }

        private bool IsValid()
        {
            if (ctlCustomer.SelectedID <= 0)
            {
                MessageBox.Show("Select a Customer", "Calculate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ctlCustomer.Focus();
                return false;
            }
            if (Convert.ToDecimal(txtRemaining.Text) < 0)
            {
                MessageBox.Show("Paid amount can't be more than due amount", "Calculate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                numCashDownPayment.Focus();
                return false;
            }

            if (txtNoOfInstallment.Text == "0")
            {
                MessageBox.Show("Enter no of Installment", "Calculate", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtNoOfInstallment.Focus();
                return false;
            }

            return true;
        }

        public static string GetUniqueKey(int maxSize)
        {
            char[] chars = new char[62];
            chars = "123456789".ToCharArray();
            byte[] data = new byte[1];
            RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider();
            crypto.GetNonZeroBytes(data);
            data = new byte[maxSize];
            crypto.GetNonZeroBytes(data);

            StringBuilder result = new StringBuilder(maxSize);
            foreach (byte b in data)
            {
                result.Append(chars[b % (chars.Length)]);
            }
            return result.ToString();
        }

        private void CalculateInstallmentAmt()
        {
            if (_CreditSaleUnExpected == null)
            {
                _CreditSaleUnExpected = new CreditSale();
            }

            if (_CreditSale == null)
            {
                _CreditSale = new CreditSale();

            }
            //_CreditSale.Status = !chkStatus.Checked;
            _CreditSale.CustomerID = _FromControlCustomer.CustomerID;

            _CreditSale.Quantity = Convert.ToInt32(_CreditSale.CreditSaleProducts.Sum(x => x.Quantity));
            _CreditSale.Discount = Convert.ToDecimal(txtDiscount.Text);
            _CreditSale.IssueDate = dtpIssueDate.Value;
            _CreditSale.SalesDate = dtpSalesDate.Value;
            _CreditSale.NetAmount = Convert.ToDecimal(txtNetAmount.Text);
            _CreditSale.TSalesAmt = Convert.ToDecimal(txtGrandTotalAmt.Text);
            _CreditSale.NoOfInstallment = Convert.ToInt32(txtNoOfInstallment.Text);
            _CreditSale.InterestRate = Convert.ToInt16(txtInterestRate.Text);
            _CreditSale.InvoiceNo = txtVoucherNo.Text;//"INV-" + GetUniqueKey(4);
            _CreditSale.FixedAmt = Convert.ToDecimal(txtFixedAmt.Text != "" ? txtFixedAmt.Text : "0");
            //_CreditSale.WInterestAmt = Convert.ToDecimal(txtWRateTAmt.Text);
            _CreditSale.Remaining = Convert.ToDecimal(txtRemaining.Text) + _sum_remaning;
            _CreditSale.UnExInstallment = Convert.ToInt32(txtUnExpectedIns.Text);
            _CreditSale.ISUnexpected = chkUnExpected.Checked;
            _CreditSale.Remarks = txtRemarks.Text;
            _CreditSale.MergeTotalSales = _MergeTotalAmount + (decimal)_CreditSale.TSalesAmt;

            if (rbIsDownPayment.Checked)
            {
                _CreditSale.DownPayment = numDownPayment.Value;
                //_CreditSale.IsStatus = 1;

                if (chkUnExpected.Checked)
                {
                    ReSchedule(false);
                }
                else
                {
                    CreateInstallments(dtpIssueDate.Value);
                }
            }
            else if (rbFlateAmount.Checked)
            {
                //_CreditSale.IsStatus = 2;
                if (chkUnExpected.Checked)
                {
                    ReSchedule(false);
                }
                else
                {
                    FixedAmountCalculation(dtpIssueDate.Value);
                }
            }
            else if (rbIsPercentage.Checked)
            {
                _CreditSale.DownPayment = numDownPayment.Value;
                //_CreditSale.IsStatus = 3;
                if (chkUnExpected.Checked)
                {
                    ReSchedule(false);
                }
                else
                {
                    CreateInstallments(dtpIssueDate.Value);
                }

            }

            RefreshScheduleGrid();
        }

        private void CreateInstallments(DateTime dMonth)
        {

            _CreditSale.CreditSalesDetails = new List<CreditSalesDetail>();
            CreditSalesDetail oCSalesDetail = null;

            decimal nTotalBalance = 0;
            decimal nInstallmentAmt = 0;
            decimal nTotalHireValue = 0;
            decimal nTotalNetValue = 0;
            decimal nHireValue = 0;
            decimal nNetValue = 0;

            decimal TotalDiscount = 0m;
            decimal DiscountForHire = 0m;
            decimal DiscountForProduct = 0m;


            if (_CreditSale.FirstTotalInterest == 0)
            {
                DiscountForProduct = _CreditSale.Discount;
            }
            if (_CreditSale.FirstTotalInterest >= _CreditSale.Discount)
            {
                DiscountForHire = _CreditSale.Discount;

            }
            else if (_CreditSale.FirstTotalInterest < _CreditSale.Discount)
            {
                DiscountForHire = _CreditSale.FirstTotalInterest;
                DiscountForProduct = _CreditSale.Discount - _CreditSale.FirstTotalInterest;
            }

            //bd
            nTotalBalance = (decimal)_CreditSale.Remaining;
            nTotalHireValue = _CreditSale.FirstTotalInterest - DiscountForHire + _sum_HireValue;
            nTotalNetValue = (decimal)_CreditSale.Remaining - _CreditSale.FirstTotalInterest + DiscountForHire - _sum_HireValue;

            nInstallmentAmt = nTotalBalance / Convert.ToDecimal(txtNoOfInstallment.Text);
            nHireValue = nTotalHireValue / Convert.ToDecimal(txtNoOfInstallment.Text);
            nNetValue = nTotalNetValue / Convert.ToDecimal(txtNoOfInstallment.Text);


            if (chkWeekly.Checked)
            {
                _CreditSale.ISWeeekly = 1;
                dMonth = dMonth.AddDays(7);

                for (int i = 0; i < Convert.ToInt16(this.txtNoOfInstallment.Text); i++)
                {
                    oCSalesDetail = new CreditSalesDetail();
                    oCSalesDetail.MonthDate = dMonth;
                    oCSalesDetail.ScheduleNo = i + 1;

                    oCSalesDetail.PaymentStatus = "Due";
                    oCSalesDetail.InstallmentAmt = nInstallmentAmt;
                    oCSalesDetail.HireValue = nHireValue;
                    oCSalesDetail.NetValue = nNetValue;



                    oCSalesDetail.PaymentDate = dMonth;
                    oCSalesDetail.Balance = nTotalBalance;

                    nTotalBalance -= (decimal)oCSalesDetail.InstallmentAmt;


                    oCSalesDetail.ClosingBalance = nTotalBalance;


                    oCSalesDetail.IsUnExpected = 0;
                    _CreditSale.CreditSalesDetails.Add(oCSalesDetail);
                    dMonth = dMonth.AddDays(7);
                }
            }
            else
            {
                dMonth = dMonth.AddMonths(1);
                for (int i = 0; i < Convert.ToInt16(this.txtNoOfInstallment.Text); i++)
                {
                    oCSalesDetail = new CreditSalesDetail();
                    oCSalesDetail.MonthDate = dMonth;
                    oCSalesDetail.ScheduleNo = i + 1;

                    oCSalesDetail.PaymentStatus = "Due";
                    oCSalesDetail.InstallmentAmt = nInstallmentAmt;
                    oCSalesDetail.HireValue = nHireValue;
                    oCSalesDetail.NetValue = nNetValue;



                    oCSalesDetail.PaymentDate = dMonth;
                    oCSalesDetail.Balance = nTotalBalance;

                    nTotalBalance -= (decimal)oCSalesDetail.InstallmentAmt;


                    oCSalesDetail.ClosingBalance = nTotalBalance;


                    oCSalesDetail.IsUnExpected = 0;
                    _CreditSale.CreditSalesDetails.Add(oCSalesDetail);
                    dMonth = dMonth.AddMonths(1);
                }
            }

          
        }

        private void FixedAmountCalculation(DateTime dMonth)
        {
        }

        private void InterestRateCalculation(DateTime dMonth)
        {

        }

        private void btnCalculate_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsValid()) return;


                CalculateInstallmentAmt();
                btnCalculate.Enabled = false;

                if (sAgreement == "Agreement")
                    btnSave.Enabled = false;
                else
                    btnSave.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        bool IsSaveValid()
        {
            if (numCardPaidAmt.Value > 0 && (int)cmbBank.SelectedValue == 0)
            {
                MessageBox.Show("Please select card.", "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (Convert.ToDecimal(txtRemaining.Text) < 0)
            {
                MessageBox.Show("Paid amount can't be more than due amount.", "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (_CreditSale != null)
                {
                    if (_IsSaved)
                    {
                        if (!IsSaveValid()) return;
                        _CreditSaleSave.CreditSalesDetails = new List<CreditSalesDetail>();

                        if (_CreditSale.CreditSalesDetails.Count > 0)
                        {
                            int detailid = db.CreditSalesDetails.Count() > 0 ? db.CreditSalesDetails.Max(obj => obj.CSDetailsID) + 1 : 1;
                            foreach (CreditSalesDetail oCDItem in _CreditSale.CreditSalesDetails)
                            {
                                oCDItem.CSDetailsID = detailid;
                                detailid++;
                            }
                        }
                        if (_CreditSale.CreditSaleProducts.Count > 0)
                        {
                            int cProdid = db.CreditSaleProducts.Count() > 0 ? db.CreditSaleProducts.Max(obj => obj.CreditSaleProductsID) + 1 : 1;
                            foreach (CreditSaleProduct oCPItem in _CreditSale.CreditSaleProducts)
                            {
                                oCPItem.CreditSaleProductsID = cProdid;
                                cProdid++;
                            }
                        }
                        _CreditSale.CreditSalesID = _CreditSaleList.Count() > 0 ? db.CreditSales.Max(obj => obj.CreditSalesID) + 1 : 1;//Update Code From Motiur.
                        _CreditSale.InvoiceNo = txtVoucherNo.Text;
                        _CreditSale.CreatedBy = Global.CurrentUser.UserID;
                        _CreditSale.CreateDate = DateTime.Now;
                        _CreditSale.Status = (int)EnumSalesType.Sales;



                        using (var Transaction = db.Database.BeginTransaction())
                        {
                            try
                            {
                                if (cmbCardType.SelectedValue != null && numCardPaidAmt.Value > 0)
                                {
                                    int CardTypeSetupID = (int)cmbCardType.SelectedValue;
                                    int BankTranID = 0;
                                    decimal percentage = 0;
                                    decimal CardPaidAmt = numCardPaidAmt.Value;
                                    _CreditSale.CardPaidAmount = CardPaidAmt;
                                    _CreditSale.CardTypeSetupID = CardTypeSetupID;
                                    BankTranID = BankDeposit(CardTypeSetupID, CardPaidAmt, _CreditSale.InvoiceNo,dtpSalesDate.Value, out percentage);
                                    _CreditSale.DepositChargePercent = percentage;
                                    _CreditSale.BankTranID = BankTranID;
                                }

                                db.CreditSales.Add(_CreditSale);
                                _Customer = (Customer)(db.Customers.FirstOrDefault(o => o.CustomerID == _CreditSale.CustomerID));
                                _Customer.CreditDue = _Customer.CreditDue + ((decimal)_CreditSale.Remaining - _prevOrderdue - _sum_remaning);
                                db.SaveChanges();
                                Transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                Transaction.Rollback();
                                MessageBox.Show("Transaction Failed." + Environment.NewLine + ex.Message, "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }
                        }


                        using (DEWSRMEntities db2 = new DEWSRMEntities())
                        {
                            using (var connection = db2.Database.Connection)
                            {
                                connection.Open();

                                if (DeleteCreditsalesDetailsList.ToList().Count != 0)
                                    foreach (DeleteCrediSalesDetails oDCSD in DeleteCreditsalesDetailsList)
                                    {
                                        var command = connection.CreateCommand();
                                        command.CommandText = "EXEC sp_DeletePreviousCreditSalesDetails " + oDCSD.CustomerID.ToString() + "," + oDCSD.CreditSalesID.ToString();
                                        var reader = command.ExecuteReader();
                                    }
                            }
                        }

                        MessageBox.Show("Data saved successfully.", "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _bCanceld = false;
                        btnSave.Enabled = false;

                        #region For Credit Invoice
                        var ProductList = db.Products;
                        DisplayInvoice(_CreditSale, ProductList);
                        MoneyReceipt();
                        #endregion


                    }
                    else
                    {
                        db.CreditSalesDetails.RemoveRange(_CreditSalesDetailforDelete);

                        int detailid = db.CreditSalesDetails.Count() > 0 ? db.CreditSalesDetails.Max(obj => obj.CSDetailsID) + 1 : 1;
                        foreach (CreditSalesDetail oCDItem in _CreditSale.CreditSalesDetails)
                        {
                            if (!(oCDItem.CSDetailsID > 0))
                            {
                                oCDItem.CSDetailsID = detailid;
                                detailid++;
                            }
                        }
                        if (_CreditSale.CreditSaleProducts.Count > 0)
                        {
                            int cProdid = db.CreditSaleProducts.Count() > 0 ? db.CreditSaleProducts.Max(obj => obj.CreditSaleProductsID) + 1 : 1;
                            foreach (CreditSaleProduct oCPItem in _CreditSale.CreditSaleProducts)
                            {
                                if (oCPItem.CreditSaleProductsID <= 0)
                                {
                                    oCPItem.CreditSaleProductsID = cProdid;
                                    cProdid++;
                                }
                            }
                        }

                        _CreditSale.ModifiedDate = (DateTime)DateTime.Today;
                        _CreditSale.ModifiedBy = (int)Global.CurrentUser.UserID;

                        _Customer = (Customer)(db.Customers.FirstOrDefault(o => o.CustomerID == _CreditSale.CustomerID));
                        _Customer.CreditDue = _Customer.CreditDue + ((decimal)_CreditSale.Remaining - _prevOrderdue);


                        db.SaveChanges();
                        MessageBox.Show("Data update successfully.", "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        _bCanceld = false;
                        btnSave.Enabled = false;

                        #region For Credit Invoice
                        var ProductList = db.Products;
                        DisplayInvoice(_CreditSale, ProductList);
                        MoneyReceipt();
                        #endregion


                    }
                    _isPaidUnexpected = false;
                    btnPaid.Enabled = false;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public int BankDeposit(int CardTypeSetupID, decimal CardPaidAmt, string TransactionNo,DateTime TranDate, out decimal Percentage)
        {
            BankTransaction bankTrans = new BankTransaction();
            int TransID = 0;
            Percentage = 0;
            using (var dbContext = new DEWSRMEntities())
            {
                var cardTypeSetup = dbContext.CardTypeSetups.FirstOrDefault(i => i.CardTypeSetupID == CardTypeSetupID);
                if (cardTypeSetup != null)
                {
                    Percentage = cardTypeSetup.Percentage;
                    decimal amountDecrease = (CardPaidAmt * cardTypeSetup.Percentage) / 100m;
                    bankTrans.BankID = cardTypeSetup.BankID;
                    bankTrans.Amount = CardPaidAmt - amountDecrease;
                    bankTrans.TransactionType = (int)EnumBankTransType.Deposit;
                    bankTrans.Remarks = "Customer Card payments deposit.";
                    bankTrans.TranDate = TranDate;
                    bankTrans.TransactionNo = TransactionNo;
                    var Bank = dbContext.Banks.FirstOrDefault(i => i.BankID == cardTypeSetup.BankID);
                    Bank.TotalAmount += bankTrans.Amount;
                    dbContext.BankTransactions.Add(bankTrans);
                    dbContext.SaveChanges();
                    TransID = bankTrans.BankTranID;
                }
            }
            return TransID;
        }

        private void RefreshControl()
        {

            numQTY.Value = 0;
            numUTotal.Value = 0;
            numUnitPrice.Value = 0;
            numStock.Value = 0;
            txtMRP.Text = "0";
            ctlProduct.SelectedID = 0;
            numSGrand.Value = 0;
            numTotalInterest.Value = 0;
            numInterestRate.Value = 0;

            txtBarcode.Text = "";
            txtCompressor.Text = string.Empty;
            txtMotor.Text = string.Empty;
            txtSpareparts.Text = string.Empty;
            txtPanel.Text = string.Empty;
            txtService.Text = string.Empty;
        }

        private void RecalculateNet()
        {
            try
            {
                decimal paid = 0;
                if (_CreditSale != null && _CreditSale.CreditSalesDetails != null && _CreditSale.CreditSalesDetails.Count > 0)
                {
                    List<CreditSalesDetail> paidlist = _CreditSale.CreditSalesDetails.Where(o => o.PaymentStatus == "Paid").ToList();
                    if (paidlist.Count > 0)
                    {
                        paid = (decimal)paidlist.Sum(o => o.InstallmentAmt);
                    }
                }

                double val1 = 0;
                double val2 = 0;
                double val3 = 0;
                double val4 = 0;
                double val5 = 0;
                double val6 = 0;
                double.TryParse(txtFixedAmt.Text, out val4);
                double.TryParse(txtGrandTotalAmt.Text, out val1);
                double.TryParse(txtDiscount.Text, out val3);
                double.TryParse(txtInterestRate.Text, out val5);
                double.TryParse(txtNoOfInstallment.Text, out val6);

                if (val1 == 0)
                    txtGrandTotalAmt.Text = "0";
                if (val3 == 0)
                    txtDiscount.Text = "0";

                if (val4 == 0)
                    txtFixedAmt.Text = "0";
                if (val5 == 0)
                    txtInterestRate.Text = "0";
                if (val6 == 0)
                    txtNoOfInstallment.Text = "0";

                txtNetAmount.Text = (Convert.ToDecimal(txtGrandTotalAmt.Text) + Convert.ToDecimal(txtFixedAmt.Text) - Convert.ToDecimal(txtDiscount.Text != "" ? txtDiscount.Text : "0")).ToString();
                txtRemaining.Text = (Convert.ToDecimal(txtNetAmount.Text) - numDownPayment.Value - paid).ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        bool IsAddToOrderValid()
        {

            if (numStock.Value < 0)
            {
                MessageBox.Show("Stock not available.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (numQTY.Value <= 0)
            {
                MessageBox.Show("Please enter product quantity.", "Quantity", MessageBoxButtons.OK, MessageBoxIcon.Information);
                numQTY.Focus();
                return false;
            }
            if (_CreditSale.CreditSaleProducts.Any(i => i.ProductID == _stock.ProductID && i.ColorInfoID == _stock.ColorID))
            {
                MessageBox.Show("This product is already added.", "Add to order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshControl();
                return false;
            }
            return true;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IsAddToOrderValid()) return;

                if (_CreditSale == null)
                {
                    _CreditSale = new CreditSale();

                    _CreditSale.CustomerID = ctlCustomer.SelectedID;
                    _CreditSale.CreditSaleProducts = new List<CreditSaleProduct>();
                }
                if (txtGrandTotalAmt.Text == "")
                    txtGrandTotalAmt.Text = "0";
                //   _CreditSale.TSalesAmt = (decimal)(Convert.ToDecimal(txtTotalAmt.Text) + numUTotal.Value);
                _CreditSale.TSalesAmt = (decimal)(Convert.ToDecimal(txtGrandTotalAmt.Text) + numSGrand.Value);
                _CreditSale.FirstTotalInterest = _CreditSale.FirstTotalInterest + numTotalInterest.Value;
                TotalOrderQTY = TotalOrderQTY + (decimal)numQTY.Value;

                #region Order Details
                if (_oProduct.ProductType == (int)EnumProductType.BarCode || _oProduct.ProductType == (int)EnumProductType.SerialNo)
                {
                    _creditSaleProduct = new CreditSaleProduct();
                    _creditSaleProduct.ProductID = _stock.ProductID;//ctlProduct.SelectedID;
                    _creditSaleProduct.UnitPrice = (decimal)numUnitPrice.Value;

                    _creditSaleProduct.Quantity = (decimal)numQTY.Value;
                    // _creditSaleProduct.UTAmount = (decimal)numUTotal.Value;
                    _creditSaleProduct.UTAmount = (decimal)numSGrand.Value;

                    _creditSaleProduct.MPRateTotal = (decimal)(Convert.ToDecimal(_stock.PMPrice) * numQTY.Value);     //(decimal)(Convert.ToDecimal(txtMRP.Text) * numQTY.Value);
                    _creditSaleProduct.MPRate = _stock.PMPrice; //Convert.ToDecimal(txtMRP.Text);

                    _creditSaleProduct.ColorInfoID = _stock.ColorID;
                    _creditSaleProduct.StockDetailID = ctlProduct.SelectedID;
                    _creditSaleProduct.InterestRate = numInterestRate.Value;
                    _creditSaleProduct.TotalInterest = numTotalInterest.Value;



                    _creditSaleProduct.PRate = numPRate.Value;
                    _creditSaleProduct.SRate = (decimal)numUnitPrice.Value;
                    _creditSaleProduct.CompressorWarrenty = txtCompressor.Text;
                    _creditSaleProduct.MotorWarrenty = txtMotor.Text;
                    _creditSaleProduct.PanelWarrenty = txtPanel.Text;
                    _creditSaleProduct.ServiceWarrenty = txtService.Text;
                    _creditSaleProduct.SparePartsWarrenty = txtSpareparts.Text;

                    _creditSaleProduct.GSTPerc = numGSTPerc.Value;
                    _creditSaleProduct.CGSTPerc = numCGSTPerc.Value;
                    _creditSaleProduct.SGSTPerc = numSGSTPerc.Value;
                    _creditSaleProduct.IGSTPerc = numIGSTPerc.Value;
                    _creditSaleProduct.GSTAmt = numGSTAmt.Value;
                    _creditSaleProduct.CGSTAmt = numCGSTAmt.Value;
                    _creditSaleProduct.SGSTAmt = numSGSTAmt.Value;
                    _creditSaleProduct.IGSTAmt = numIGSTAmt.Value;


                    _CreditSale.CreditSaleProducts.Add(_creditSaleProduct);
                }


                #endregion

                #region Stock
                if (_stock != null)
                {
                    _stock.Quantity = numStock.Value;
                    decimal SoldQuanity = numQTY.Value;
                    if (_oProduct.ProductType == (int)EnumProductType.NoBarCode)
                    {
                        var StockDetails = db.StockDetails.Where(i => i.ProductID == _oProduct.ProductID && i.ColorID == _StockDetail.ColorID && i.Status == (int)EnumStockDetailStatus.Stock);
                        foreach (var item in StockDetails)
                        {
                            _creditSaleProduct = new CreditSaleProduct();
                            _creditSaleProduct.ProductID = _stock.ProductID;//ctlProduct.SelectedID;
                            _creditSaleProduct.UnitPrice = (decimal)numUnitPrice.Value;
                            _creditSaleProduct.MPRate = (decimal)item.PRate; //Convert.ToDecimal(txtMRP.Text);
                            _creditSaleProduct.ColorInfoID = _stock.ColorID;
                            _creditSaleProduct.StockDetailID = item.SDetailID;
                            _creditSaleProduct.InterestRate = numInterestRate.Value;
                            _creditSaleProduct.PRate = (decimal)item.PRate;
                            _creditSaleProduct.SRate = (decimal)numUnitPrice.Value;
                            _creditSaleProduct.CompressorWarrenty = txtCompressor.Text;
                            _creditSaleProduct.MotorWarrenty = txtMotor.Text;
                            _creditSaleProduct.PanelWarrenty = txtPanel.Text;
                            _creditSaleProduct.ServiceWarrenty = txtService.Text;
                            _creditSaleProduct.SparePartsWarrenty = txtSpareparts.Text;

                            if (item.Quantity == SoldQuanity)
                            {
                                _creditSaleProduct.Quantity = SoldQuanity;
                                _creditSaleProduct.UTAmount = _creditSaleProduct.SRate * SoldQuanity;
                                _creditSaleProduct.TotalInterest = (decimal)((_creditSaleProduct.UTAmount * _creditSaleProduct.InterestRate) / 100m);
                                _creditSaleProduct.UTAmount += _creditSaleProduct.TotalInterest;
                                _creditSaleProduct.MPRateTotal = (decimal)item.PRate * SoldQuanity;
                                item.Quantity -= SoldQuanity;
                                item.Status = (int)EnumStockDetailStatus.Sold;
                                _CreditSale.CreditSaleProducts.Add(_creditSaleProduct);
                                break;
                            }
                            else if (item.Quantity > SoldQuanity)
                            {
                                _creditSaleProduct.Quantity = SoldQuanity;
                                _creditSaleProduct.UTAmount = _creditSaleProduct.SRate * SoldQuanity;
                                _creditSaleProduct.TotalInterest = (decimal)((_creditSaleProduct.UTAmount * _creditSaleProduct.InterestRate) / 100m);
                                _creditSaleProduct.UTAmount += _creditSaleProduct.TotalInterest;
                                _creditSaleProduct.MPRateTotal = (decimal)item.PRate * SoldQuanity;
                                item.Quantity -= SoldQuanity;
                                _CreditSale.CreditSaleProducts.Add(_creditSaleProduct);
                                break;
                            }
                            else if (item.Quantity < SoldQuanity)
                            {
                                _creditSaleProduct.Quantity = item.Quantity;
                                _creditSaleProduct.UTAmount = _creditSaleProduct.SRate * item.Quantity;
                                _creditSaleProduct.TotalInterest = (decimal)((_creditSaleProduct.UTAmount * _creditSaleProduct.InterestRate) / 100m);
                                _creditSaleProduct.UTAmount += _creditSaleProduct.TotalInterest;
                                _creditSaleProduct.MPRateTotal = (decimal)item.PRate * item.Quantity;
                                SoldQuanity = SoldQuanity - item.Quantity;
                                item.Quantity = 0m;
                                item.Status = (int)EnumStockDetailStatus.Sold;
                                _CreditSale.CreditSaleProducts.Add(_creditSaleProduct);
                            }

                        }
                    }
                    else
                    {
                        _StockDetail.Status = (int)EnumStockDetailStatus.Sold;
                    }

                }
                #endregion

                txtGrandTotalAmt.Text = ((decimal)(Convert.ToDecimal(txtGrandTotalAmt.Text) + (decimal)numSGrand.Value)).ToString();

                RefreshGrid();
                RefreshControl();
            }
            catch (Exception EX)
            {
                MessageBox.Show(EX.Message);
            }
        }
        private void btnRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgProducts.SelectedRows.Count > 0)
                {
                    TotalOrderQTY = 0;
                    CreditSaleProduct oCreditSaleProduct = dgProducts.SelectedRows[0].Tag as CreditSaleProduct;
                    txtGrandTotalAmt.Text = (Convert.ToDecimal(txtGrandTotalAmt.Text) - (decimal)oCreditSaleProduct.UTAmount).ToString();
                    _StockDetail = db.StockDetails.FirstOrDefault(o => o.SDetailID == oCreditSaleProduct.StockDetailID);
                    _stock = _StockDetail.Stock;
                    var product = db.Products.FirstOrDefault(i => i.ProductID == oCreditSaleProduct.ProductID);
                    if (_StockDetail != null)
                    {
                        _stock.Quantity = _stock.Quantity + (decimal)oCreditSaleProduct.Quantity;
                        _stock.ModifiedDate = DateTime.Today;
                        _stock.ModifiedBy = Global.CurrentUser.UserID;
                        if (product.ProductType == (int)EnumProductType.NoBarCode)
                        {
                            var nobarcodestockdetails = _CreditSale.CreditSaleProducts.Where(i => i.ProductID == product.ProductID).ToList();
                            foreach (var item in nobarcodestockdetails)
                            {
                                var stockdetails = db.StockDetails.FirstOrDefault(i => i.SDetailID == item.StockDetailID);
                                stockdetails.Quantity += (decimal)item.Quantity;
                                stockdetails.Status = (int)EnumStockDetailStatus.Stock;
                                if (_CreditSale.CreditSalesID > 0)
                                {
                                    db.CreditSaleProducts.Remove(item);
                                }
                                else
                                {
                                    _CreditSale.CreditSaleProducts.Remove(item);
                                }
                            }
                        }
                    }
                    if (product.ProductType != (int)EnumProductType.NoBarCode)
                    {
                        if (_CreditSale.CreditSalesID > 0)
                        {
                            db.CreditSaleProducts.Remove(oCreditSaleProduct);
                        }
                        else
                        {
                            _CreditSale.CreditSaleProducts.Remove(oCreditSaleProduct);
                        }
                    }
                    RefreshGrid();
                }
                else
                {
                    MessageBox.Show("select an item to remove", "Item not yet selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception EX)
            {
                MessageBox.Show(EX.Message);
            }

        }
        private void btnCancle_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnPrint_Click(object sender, EventArgs e)
        {
            var ProductList = db.Products;
            DisplayInvoice(_CreditSale, ProductList);
        }

        public void DisplayInvoice(CreditSale oCreditSale, IEnumerable<Product> ProductList)
        {
            try
            {
                fReportViewer fRptViewer = new fReportViewer();
                rptDataSet.CreditSalesInfoDataTable CreditSalesDT = new rptDataSet.CreditSalesInfoDataTable();
                DataRow oSDRow = null;

                if (db == null)
                    db = new DEWSRMEntities();

                DataSet ds = new DataSet();

                List<CreditSalesDetail> CreditSDetails = oCreditSale.CreditSalesDetails.OrderBy(o => o.ScheduleNo).ToList();
                List<CreditSaleProduct> CreditSProducts = oCreditSale.CreditSaleProducts.ToList();
                //List<INVENTORY.DA.Color> oColList = db.Colors.ToList();
                List<INVENTORY.DA.Category> oCategoryList = db.Categorys.ToList();

                Product oProduct = null;
                INVENTORY.DA.Color oColor = null;
                Category oCategory = null;
                foreach (CreditSalesDetail oSItem in CreditSDetails)
                {
                    oSDRow = CreditSalesDT.NewRow();

                    oSDRow["ScheduleNo"] = oSItem.ScheduleNo;
                    oSDRow["PaymentDate"] = Convert.ToDateTime(oSItem.PaymentDate).ToString("dd MMM yyyy");
                    oSDRow["Balance"] = oSItem.Balance;
                    oSDRow["InstallmetAmt"] = oSItem.InstallmentAmt;
                    oSDRow["ClosingBalance"] = oSItem.ClosingBalance;
                    oSDRow["PaymentStatus"] = oSItem.PaymentStatus;
                    CreditSalesDT.Rows.Add(oSDRow);
                }

                CreditSalesDT.TableName = "rptDataSet_CreditSalesInfo";
                ds.Tables.Add(CreditSalesDT);

                string Warrenty = string.Empty;
                INVENTORY.UI.rptDataSet.CSalesProductDataTable CSProductDT = new rptDataSet.CSalesProductDataTable();
                DataRow oCSPRow = null;
                int nCOunt = 1;
                List<CreditSaleProduct> NobarcodeCreditSProductList = new List<CreditSaleProduct>();
                foreach (CreditSaleProduct item in CreditSProducts)
                {
                    oProduct = ProductList.FirstOrDefault(i => i.ProductID == item.ProductID);
                    oColor = db.Colors.FirstOrDefault(c => c.ColorID == item.ColorInfoID);
                    oCategory = oCategoryList.FirstOrDefault(c => c.CategoryID == oProduct.CategoryID);
                    if (oProduct.ProductType != (int)EnumProductType.NoBarCode)
                    {
                        oCSPRow = CSProductDT.NewRow();
                        oCSPRow["SLNo"] = nCOunt.ToString();
                        oCSPRow["PName"] = item.Product.ProductName;
                        oCSPRow["ColorCode"] = oColor.Description;
                        oCSPRow["SerialNo"] = item.StockDetail.IMENO.ToString();

                        Warrenty = string.IsNullOrEmpty(item.CompressorWarrenty) ? "" : "Compressor: " + item.CompressorWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.MotorWarrenty) ? "" : "Motor: " + item.MotorWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.PanelWarrenty) ? "" : "Panel: " + item.PanelWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.SparePartsWarrenty) ? "" : "SpareParts: " + item.SparePartsWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.ServiceWarrenty) ? "" : "Service: " + item.ServiceWarrenty;
                        oCSPRow["WPeriod"] = Warrenty;
                        Warrenty = string.Empty;

                        oCSPRow["Qty"] = item.Quantity.ToString();
                        oCSPRow["UnitPrice"] = (item.UnitPrice + item.TotalInterest).ToString();
                        oCSPRow["TotalAmt"] = item.UTAmount.ToString();
                        oCSPRow["CategoryName"] = oCategory.Description;
                        CSProductDT.Rows.Add(oCSPRow);
                        nCOunt++;
                    }
                    else
                    {
                        NobarcodeCreditSProductList.Add(item);
                    }


                }

                if (NobarcodeCreditSProductList.Count() != 0)
                {
                    var nobarcodesd = from sod in NobarcodeCreditSProductList
                                      join sd in db.StockDetails on sod.StockDetailID equals sd.SDetailID
                                      group sod by new { sod.ProductID, sd.ColorID } into g
                                      select new
                                      {
                                          ProductID = g.Key.ProductID,
                                          CompressorWarrenty = g.FirstOrDefault().CompressorWarrenty,
                                          MotorWarrenty = g.FirstOrDefault().MotorWarrenty,
                                          PanelWarrenty = g.FirstOrDefault().PanelWarrenty,
                                          SparePartsWarrenty = g.FirstOrDefault().SparePartsWarrenty,
                                          ServiceWarrenty = g.FirstOrDefault().ServiceWarrenty,
                                          Quantity = g.Sum(i => i.Quantity),
                                          UnitPrice = g.FirstOrDefault().UnitPrice,
                                          UTAmount = g.Sum(i => i.UTAmount),
                                          TotalInterest = g.FirstOrDefault().TotalInterest,
                                          ColorID = g.FirstOrDefault().ColorInfoID
                                      };

                    foreach (var item in nobarcodesd)
                    {
                        oProduct = ProductList.FirstOrDefault(p => p.ProductID == item.ProductID);
                        oColor = db.Colors.FirstOrDefault(co => co.ColorID == item.ColorID);
                        oCategory = oCategoryList.FirstOrDefault(c => c.CategoryID == oProduct.CategoryID);
                        oCSPRow = CSProductDT.NewRow();
                        oCSPRow["SLNo"] = nCOunt.ToString();
                        oCSPRow["PName"] = oProduct.ProductName;
                        oCSPRow["ColorCode"] = oColor.Description;
                        oCSPRow["SerialNo"] = "No Barcode";
                        Warrenty = string.IsNullOrEmpty(item.CompressorWarrenty) ? "" : "Compressor: " + item.CompressorWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.MotorWarrenty) ? "" : "Motor: " + item.MotorWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.PanelWarrenty) ? "" : "Panel: " + item.PanelWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.SparePartsWarrenty) ? "" : "SpareParts: " + item.SparePartsWarrenty + Environment.NewLine;
                        Warrenty += string.IsNullOrEmpty(item.ServiceWarrenty) ? "" : "Service: " + item.ServiceWarrenty;
                        oCSPRow["WPeriod"] = Warrenty;
                        Warrenty = string.Empty;

                        oCSPRow["Qty"] = item.Quantity.ToString();
                        oCSPRow["UnitPrice"] = (item.UnitPrice + item.TotalInterest).ToString();
                        oCSPRow["TotalAmt"] = item.UTAmount.ToString();
                        oCSPRow["CategoryName"] = oCategory.Description;
                        CSProductDT.Rows.Add(oCSPRow);
                        nCOunt++;
                    }
                }

                CSProductDT.TableName = "rptDataSet_CSalesProduct";
                ds.Tables.Add(CSProductDT);

                string embededResource = "INVENTORY.UI.RDLC.CreditSalesInfo.rdlc";
                ReportParameter rParam = new ReportParameter();
                List<ReportParameter> parameters = new List<ReportParameter>();

                rParam = new ReportParameter("InvoiceNo", oCreditSale.InvoiceNo);
                parameters.Add(rParam);

                rParam = new ReportParameter("SalesDate", oCreditSale.SalesDate.ToString());
                parameters.Add(rParam);

                rParam = new ReportParameter("ProductName", "");
                parameters.Add(rParam);

                rParam = new ReportParameter("CustomerName", oCreditSale.Customer.Name);
                parameters.Add(rParam);

                rParam = new ReportParameter("CAddress", oCreditSale.Customer.Address);
                parameters.Add(rParam);

                rParam = new ReportParameter("CContactNo", oCreditSale.Customer.ContactNo);
                parameters.Add(rParam);

                rParam = new ReportParameter("Remarks", oCreditSale.Remarks);
                parameters.Add(rParam);

                rParam = new ReportParameter("SalesPrice", oCreditSale.TSalesAmt.ToString());
                parameters.Add(rParam);

                rParam = new ReportParameter("DownPayment", oCreditSale.DownPayment.ToString());
                parameters.Add(rParam);

                rParam = new ReportParameter("RemainingAmt", oCreditSale.Remaining.ToString());
                parameters.Add(rParam);

                rParam = new ReportParameter("PrintedBy", Global.CurrentUser.UserName);
                parameters.Add(rParam);

                fReportViewer frm = new fReportViewer();
                if (CreditSalesDT.Rows.Count > 0)
                {
                    frm.CommonReportViewer(embededResource, ds, parameters, true);
                }
                else
                {
                    MessageBox.Show("No Recors Found.", "Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void MoneyReceipt()
        {

            DataSet ds = new DataSet();
            string embededResource = "INVENTORY.UI.RDLC.CreditMoneyReceipt.rdlc";
            ReportParameter rParam = new ReportParameter();
            List<ReportParameter> parameters = new List<ReportParameter>();

            rParam = new ReportParameter("CusCode", _CreditSale.Customer.Code);
            parameters.Add(rParam);

            string sInwodTk = Global.TakaFormat(Convert.ToDouble(_CreditSale.TSalesAmt));

            rParam = new ReportParameter("CusName", _CreditSale.Customer.Name);
            parameters.Add(rParam);

            rParam = new ReportParameter("InvoiceNo", _CreditSale.InvoiceNo);
            parameters.Add(rParam);

            rParam = new ReportParameter("TSalesAmt", sInwodTk);
            parameters.Add(rParam);

            rParam = new ReportParameter("Remaining", _CreditSale.Remaining.ToString());
            parameters.Add(rParam);



            if (_oCSDetails != null)
            {

                sInwodTk = Global.TakaFormat(Convert.ToDouble(_oCSDetails.InstallmentAmt.ToString()));
                sInwodTk = sInwodTk.Replace(" Taka", "");
                sInwodTk = sInwodTk.Replace("Only", "Taka Only");
                rParam = new ReportParameter("InWordTK", sInwodTk);
                parameters.Add(rParam);

                rParam = new ReportParameter("TDue", _oCSDetails.InstallmentAmt.ToString());
                parameters.Add(rParam);
                rParam = new ReportParameter("PaymentDate", _oCSDetails.PaymentDate.Value.ToString("dd MMM yyyy"));
                parameters.Add(rParam);

                string sInwodTk2 = Global.TakaFormat(Convert.ToDouble((_CreditSale.MergeTotalSales - (_CreditSale.Remaining)).ToString()));
                sInwodTk2 = sInwodTk2.Replace("Taka", "");
                sInwodTk2 = sInwodTk2.Replace("Only", "Taka Only");
                rParam = new ReportParameter("TReceiveAmt", ((_CreditSale.MergeTotalSales - _CreditSale.Discount) - (_CreditSale.Remaining)).ToString() + '(' + sInwodTk2 + ')');
                parameters.Add(rParam);
            }
            else
            {
                sInwodTk = Global.TakaFormat(Convert.ToDouble(_CreditSale.DownPayment.ToString()));
                sInwodTk = sInwodTk.Replace(" Taka", "");
                sInwodTk = sInwodTk.Replace("Only", "Taka Only");
                rParam = new ReportParameter("InWordTK", sInwodTk);
                parameters.Add(rParam);

                rParam = new ReportParameter("TDue", _CreditSale.DownPayment.ToString());
                parameters.Add(rParam);
                rParam = new ReportParameter("PaymentDate", _CreditSale.IssueDate.ToString("dd MMM yyyy"));
                parameters.Add(rParam);

                sInwodTk = Global.TakaFormat(Convert.ToDouble((_CreditSale.DownPayment).ToString()));
                sInwodTk = sInwodTk.Replace("Taka", "");
                sInwodTk = sInwodTk.Replace("Only", "Taka Only");

                rParam = new ReportParameter("TReceiveAmt", _CreditSale.DownPayment.ToString() + '(' + sInwodTk + ')');
                parameters.Add(rParam);

            }

            string SModels = "";
            Product product = null;
            var ProductList = db.Products;
            List<CreditSaleProduct> NobarcodecreditSaleProduct = new List<CreditSaleProduct>();
            foreach (CreditSaleProduct oSItem in _CreditSale.CreditSaleProducts)
            {
                product = ProductList.FirstOrDefault(i => i.ProductID == oSItem.ProductID);
                if (product.ProductType != (int)EnumProductType.NoBarCode)
                {
                    if (SModels == "")
                    {
                        SModels = oSItem.Product.ProductName;
                    }
                    else
                    {
                        SModels = SModels + "," + oSItem.Product.ProductName;
                    }
                }
                else
                {
                    NobarcodecreditSaleProduct.Add(oSItem);
                }

            }
            if (NobarcodecreditSaleProduct.Count() != 0)
            {
                var nobarcode = from nb in NobarcodecreditSaleProduct
                                group nb by nb.ProductID into g
                                select new
                                {
                                    ProductID = g.Key
                                };
                var productList = (from nb in nobarcode
                                   join p in ProductList on nb.ProductID equals p.ProductID
                                   select new
                                   {
                                       ProductName = p.ProductName
                                   }).ToList();
                foreach (var item in productList)
                {
                    if (SModels == "")
                    {
                        SModels = item.ProductName;
                    }
                    else
                    {
                        SModels = SModels + "," + item.ProductName;
                    }
                }
            }

            rParam = new ReportParameter("PModels", SModels);
            parameters.Add(rParam);

            //rParam = new ReportParameter("Logo", Application.StartupPath + @"\Logo.bmp");
            //parameters.Add(rParam);

            fReportViewer frm = new fReportViewer();
            frm.CommonReportViewer(embededResource, ds, parameters, true);
        }

        private void FCreditSale_Load(object sender, EventArgs e)
        {
            CreditSale oCS = new CreditSale();
            _CreditSaleList = db.CreditSales.ToList();

            int gn = _CreditSaleList.Count() + db.SOrders.ToList().Count();
            if (_IsSaved)
                txtVoucherNo.Text = GenerateInvoiceNo();
            // txtVoucherNo.Text = "INV-" + GetUniqueKey(4);
            //RefreshWarrantyType();
        }

        //private void RefreshWarrantyType()
        //{


        //    cboWarrantyType.DisplayMember = "Name";
        //    cboWarrantyType.ValueMember = "ID";
        //    cboWarrantyType.DataSource = Enum.GetValues(typeof(EnumWarrantyType)).Cast<EnumWarrantyType>().Select(x => new { ID = (int)x, Name = x.ToString() }).ToList();
        //}
        private string GenerateInvoiceNo()
        {
            int i = 0;
            i = db.SOrders.Count() + db.CreditSales.Count();
            return "INV-000" + (i + 1);
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            //bd
            if (keyData == Keys.Enter)
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void txtInterestRate_TextChanged(object sender, EventArgs e)
        {
            txtFixedAmt.Text = (Convert.ToDecimal(txtRemaining.Text != "" ? txtRemaining.Text : "0") * Convert.ToDecimal(txtInterestRate.Text) / 100).ToString();
        }


        private void btnPaid_Click(object sender, EventArgs e)
        {
            if (chkIsAllPaid.Checked)
                AllInstallmentsPaid();
            else
                SingleInstallmentPaid();
        }


        private void SingleInstallmentPaid()
        {
            try
            {
                if (!IsSingleInstallmentPaidValid()) return;

                #region Unexpected Installment No-1
                if (_isPaidUnexpected)
                {
                    db.CreditSalesDetails.RemoveRange(_CreditSalesDetailforDelete);

                    int detailid = db.CreditSalesDetails.Count() > 0 ? db.CreditSalesDetails.Max(obj => obj.CSDetailsID) + 1 : 1;
                    foreach (CreditSalesDetail oCDItem in _CreditSale.CreditSalesDetails)
                    {
                        if (!(oCDItem.CSDetailsID > 0))
                        {
                            oCDItem.CSDetailsID = detailid;
                            detailid++;
                        }
                    }
                    if (_CreditSale.CreditSaleProducts.Count > 0)
                    {
                        int cProdid = db.CreditSaleProducts.Count() > 0 ? db.CreditSaleProducts.Max(obj => obj.CreditSaleProductsID) + 1 : 1;
                        foreach (CreditSaleProduct oCPItem in _CreditSale.CreditSaleProducts)
                        {
                            if (oCPItem.CreditSaleProductsID <= 0)
                            {
                                oCPItem.CreditSaleProductsID = cProdid;
                                cProdid++;
                            }
                            // _CreditSaleSave.CreditSalesDetails.Add(oCDItem);
                        }
                    }

                    _Customer = (Customer)(db.Customers.FirstOrDefault(o => o.CustomerID == _CreditSale.CustomerID));
                    _Customer.CreditDue = _Customer.CreditDue + ((decimal)_CreditSale.Remaining - _prevOrderdue);


                    db.SaveChanges();
                }

                #endregion

                if (dgvCreditSales.SelectedRows.Count > 0)
                {
                    // DataGridViewRow startingBalanceRow = dgvCreditSales.SelectedRows[0].Cells;
                    DateTime paydate = Convert.ToDateTime((dgvCreditSales.SelectedRows[0].Cells[1].Value));
                    CreditSalesDetail oCSDetails = (CreditSalesDetail)dgvCreditSales.SelectedRows[0].Tag;
                    if (oCSDetails.PaymentStatus.Equals("Paid"))
                    {
                        MessageBox.Show("Please select due schedule.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    _oCSDetails = oCSDetails;
                    oCSDetails.PaymentStatus = "Paid";
                    oCSDetails.LastPayAdjustment = numAdjustment.Value;
                    oCSDetails.PaymentDate = paydate;
                    oCSDetails.Remarks = (string)dgvCreditSales.Rows[dgvCreditSales.SelectedRows[0].Index].Cells[8].Value;

                    if (oCSDetails.InstallmentAmt != Convert.ToDecimal(numPayment.Value + numAdjustment.Value))
                    {
                        //bdtests
                        _ratio = (double)oCSDetails.HireValue / (double)oCSDetails.InstallmentAmt;
                        oCSDetails.HireValue = Convert.ToDecimal(numPayment.Value) * (decimal)_ratio;
                        oCSDetails.NetValue =Convert.ToDecimal(numPayment.Value)- Convert.ToDecimal(numPayment.Value) * (decimal)_ratio ;
                        oCSDetails.InstallmentAmt = Convert.ToDecimal(numPayment.Value);

                        oCSDetails.ClosingBalance = _CreditSale.Remaining - oCSDetails.InstallmentAmt;
                        _CreditSale.Remaining = (decimal)((double)_CreditSale.Remaining - (double)oCSDetails.InstallmentAmt);
                        ReSchedule(true);
                        db.CreditSalesDetails.RemoveRange(_CreditSalesDetailforDelete);
                        int detailid = db.CreditSalesDetails.Count() > 0 ? db.CreditSalesDetails.Max(obj => obj.CSDetailsID) + 1 : 1;
                        foreach (CreditSalesDetail oCDItem in _CreditSale.CreditSalesDetails)
                        {
                            if (!(oCDItem.CSDetailsID > 0))
                            {
                                oCDItem.CSDetailsID = detailid;
                                detailid++;
                            }
                        }

                        _Customer = (Customer)(db.Customers.FirstOrDefault(o => o.CustomerID == _CreditSale.CustomerID));
                        _Customer.CreditDue = _Customer.CreditDue - Convert.ToDecimal(numPayment.Value);
                    }
                    else
                    {
                        _CreditSale.Remaining = (decimal)((double)oCSDetails.Balance - ((double)numPayment.Value + (double)numAdjustment.Value));
                        oCSDetails.InstallmentAmt = Convert.ToDecimal(numPayment.Value);
                        _CreditSale.WInterestAmt = numAdjustment.Value; //Last payment adjustment 
                        _Customer = (Customer)(db.Customers.FirstOrDefault(o => o.CustomerID == _CreditSale.CustomerID));
                        _Customer.CreditDue = _Customer.CreditDue - (numPayment.Value + numAdjustment.Value);
                    }


                    using (var Transaction = db.Database.BeginTransaction())
                    {
                        try
                        {
                            if (cmbCardType.SelectedValue != null && numCardPaidAmt.Value > 0)
                            {
                                int CardTypeSetupID = (int)cmbCardType.SelectedValue;
                                int BankTranID = 0;
                                decimal percentage = 0;
                                decimal CardPaidAmt = numCardPaidAmt.Value;

                                oCSDetails.CardPaidAmount = CardPaidAmt;
                                oCSDetails.CardTypeSetupID = CardTypeSetupID;
                                BankTranID = BankDeposit(CardTypeSetupID, CardPaidAmt, _CreditSale.InvoiceNo + "-" + oCSDetails.ScheduleNo, oCSDetails.PaymentDate.Value, out percentage);
                                oCSDetails.DepositChargePercent = percentage;
                                oCSDetails.BankTranID = BankTranID;
                            }
                            db.SaveChanges();
                            Transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            Transaction.Rollback();
                            MessageBox.Show("Transaction Failed." + Environment.NewLine + ex.Message, "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            return;
                        }
                    }
                    MessageBox.Show("Paid successfully.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    MoneyReceipt();
                    RefreshScheduleGrid();
                }
                else
                {
                    MessageBox.Show("Please Select a Schedule", "Select", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ReSchedule(bool isfrompay)
        {
            int remainingNoofIns = 0;
            DateTime dMonth = dtpIssueDate.Value;

            List<CreditSalesDetail> paidlist = _CreditSale.CreditSalesDetails.Where(o => o.PaymentStatus == "Paid").ToList();
            List<CreditSalesDetail> paidlistDue = _CreditSale.CreditSalesDetails.Where(o => o.PaymentStatus == "Due").ToList();
            _CreditSalesDetailforDelete = paidlistDue;

            if (chkUnExpected.Checked && !isfrompay)
            {
                remainingNoofIns = Convert.ToInt32(txtUnExpectedIns.Text);// -paidlist.Count;
            }
            else if (!isfrompay)
            {
                remainingNoofIns = Convert.ToInt32(txtNoOfInstallment.Text);// -paidlist.Count;
            }
            else if (isfrompay)
            {
                remainingNoofIns = _CreditSale.CreditSalesDetails.Count - paidlist.Count;
            }

            foreach (CreditSalesDetail item in paidlistDue)
            {
                _CreditSale.CreditSalesDetails.Remove(item);
            }

            CreditSalesDetail oCSalesDetail = null;


            decimal nTotalBalance = 0;
            decimal nInstallmentAmt = 0;

            decimal nRate = Convert.ToDecimal(this.txtInterestRate.Text) / 100;
            nTotalBalance = Convert.ToDecimal(_CreditSale.Remaining);
            nInstallmentAmt = Convert.ToDecimal(_CreditSale.Remaining) / remainingNoofIns;
            dMonth = dMonth.AddMonths(paidlist.Count + 1);

            for (int i = 0; i < remainingNoofIns; i++)
            {
                //bdCreditSales
                oCSalesDetail = new CreditSalesDetail();
                oCSalesDetail.MonthDate = dMonth;
                oCSalesDetail.ScheduleNo = paidlist.Count + 1 + i;

                oCSalesDetail.PaymentStatus = "Due";
                oCSalesDetail.InstallmentAmt = nInstallmentAmt;

                oCSalesDetail.HireValue = nInstallmentAmt * (decimal)_ratio;
                oCSalesDetail.NetValue = nInstallmentAmt * (decimal)(1 - _ratio);

                oCSalesDetail.PaymentDate = dMonth;
                oCSalesDetail.Balance = nTotalBalance;

                nTotalBalance -= (decimal)oCSalesDetail.InstallmentAmt;
                oCSalesDetail.ClosingBalance = nTotalBalance;
                oCSalesDetail.IsUnExpected = 1;
                _CreditSale.CreditSalesDetails.Add(oCSalesDetail);
                dMonth = dMonth.AddMonths(1);
            }

            if (!isfrompay && remainingNoofIns == 1)
            {
                btnPaid.Enabled = true;
                numPayment.Value = nInstallmentAmt;
                numPaymentNew.Value = nInstallmentAmt;
                _isPaidUnexpected = true;
            }

        }

        private void txtFixedAmt_TextChanged(object sender, EventArgs e)
        {
            RecalculateNet();
        }

        private void btnNewCustomer_Click(object sender, EventArgs e)
        {
            try
            {
                ForNewCustomer = true;
                fCustomer ofCustomer = new fCustomer();
                ofCustomer.ShowDlg(new Customer(), true);

                if (ForNewCustomer)
                {
                    db = new DEWSRMEntities();
                    List<Customer> oCusList = db.Customers.ToList();
                    ctlCustomer.SelectedID = oCusList[oCusList.Count - 1].CustomerID;
                    ForNewCustomer = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void numQTY_ValueChanged(object sender, EventArgs e)
        {
            if (numQTY.Value != 0)
            {
                numUTotal.Value = numQTY.Value * numUnitPrice.Value;
                numStock.Value = (_stock != null ? (decimal)_stock.Quantity : 0) - numQTY.Value;
                numUTotal.Value = numQTY.Value * numUnitPrice.Value;
                numSGrand.Value = numUTotal.Value + numTotalInterest.Value;
            }
        }

        private void numUnitPrice_ValueChanged(object sender, EventArgs e)
        {
            numUTotal.Value = numQTY.Value * numUnitPrice.Value;
            numSGrand.Value = numUTotal.Value + numTotalInterest.Value;
        }

        private void txtTotalAmt_TextChanged(object sender, EventArgs e)
        {
            RecalculateNet();
        }

        private void txtDiscount_TextChanged(object sender, EventArgs e)
        {
            RecalculateNet();
        }

        private void ctlCustomer_SelectedItemChanged(object sender, EventArgs e)
        {
            //bddd
            // List<INVENTORY.DA.CreditSalesDetail> CSDList = db.CreditSalesDetails.ToList();
            _sum_HireValue = 0m;
            _sum_remaning = 0m;
            _MergeTotalAmount = 0m;
            var PreviousCreditSales = from CS in db.CreditSales
                                      where (CS.CustomerID == ctlCustomer.SelectedID)
                                      select new
                                      {
                                          CS.CreditSalesID,
                                          CS.FirstTotalInterest,
                                          CS.TSalesAmt,
                                          CS.DownPayment,
                                          CS.CustomerID
                                      };


            if (PreviousCreditSales.ToList().Count() != 0)
            {

                foreach (var x in PreviousCreditSales)
                {
                    _MergeTotalAmount += (decimal)x.TSalesAmt;

                    DeleteCrediSalesDetails DCSD = new DeleteCrediSalesDetails();
                    var gi = (from CSD in db.CreditSalesDetails
                              join CS in db.CreditSales on CSD.CreditSalesID equals CS.CreditSalesID
                              where (CS.CustomerID == ctlCustomer.SelectedID && CSD.InstallmentAmt != null && CSD.PaymentStatus == "Due" && CS.CreditSalesID == x.CreditSalesID)
                              select new
                               {
                                   CS.CustomerID
                               });


                    if (gi.ToList().Count() != 0)
                    {
                        decimal _sum_remaning_temp = (from CSD in db.CreditSalesDetails
                                                      join CS in db.CreditSales on CSD.CreditSalesID equals CS.CreditSalesID
                                                      where (CS.CustomerID == ctlCustomer.SelectedID && CSD.InstallmentAmt != null && CSD.PaymentStatus == "Due" && CS.CreditSalesID == x.CreditSalesID)
                                                      group new { CSD, CS } by new { CS.CustomerID } into g
                                                      select new
                                                      {
                                                          Remaning_from_PreCreditSales = (decimal)g.Sum(i => i.CSD.InstallmentAmt)
                                                      }).Sum(o => o.Remaning_from_PreCreditSales);


                        DCSD.CustomerID = x.CustomerID;
                        DCSD.CreditSalesID = x.CreditSalesID;
                        decimal ratio = (x.FirstTotalInterest) / (decimal)(x.TSalesAmt - x.DownPayment);
                        decimal _sum_HireValue_temp = _sum_remaning_temp * ratio;
                        _sum_remaning = _sum_remaning + _sum_remaning_temp;
                        _sum_HireValue = _sum_HireValue + _sum_HireValue_temp;
                    }
                    DeleteCreditsalesDetailsList.Add(DCSD);
                }


            }

            //  if(gi.ToList().Count()!=0)
            //_sum_remaning = (from CSD in db.CreditSalesDetails
            //              join CS in  db.CreditSales on CSD.CreditSalesID equals CS.CreditSalesID 
            //              where (CS.CustomerID==ctlCustomer.SelectedID && CSD.InstallmentAmt!=null && CSD.PaymentStatus=="Due")
            //              group new {CSD,CS} by new { CS.CustomerID }
            //              into g
            //              select new
            //              {

            //                  Remaning_from_PreCreditSales = (decimal)g.Sum(i => i.CSD.InstallmentAmt)
            //              }).Sum(o=>o.Remaning_from_PreCreditSales );


            //bd

            try
            {
                _FromControlCustomer = (Customer)(db.Customers.FirstOrDefault(o => o.CustomerID == ctlCustomer.SelectedID));
                if (_FromControlCustomer != null)
                {
                    numPrevDue.Value = _FromControlCustomer.CreditDue;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ctlProduct_SelectedItemChanged(object sender, EventArgs e)
        {
            try
            {

                _StockDetail = (StockDetail)(db.StockDetails.FirstOrDefault(o => o.SDetailID == ctlProduct.SelectedID));
                if (_StockDetail != null)
                {
                    _oProduct = _StockDetail.Product;

                    txtBarcode.Text = _StockDetail.IMENO;
                    if (_oProduct != null)
                    {
                        _stock = _StockDetail.Stock;
                        txtMRP.Text = _StockDetail.SalesRate.ToString();
                        numStock.Value = (decimal)_stock.Quantity;
                        numUnitPrice.Value = _StockDetail.SalesRate;
                        numPRate.Value = (decimal)_StockDetail.PRate;
                        txtCompressor.Text = _oProduct.CompressorWarrenty;
                        txtMotor.Text = _oProduct.MotorWarrenty;
                        txtSpareparts.Text = _oProduct.SparePartsWarrenty;
                        txtPanel.Text = _oProduct.PanelWarrenty;
                        txtService.Text = _oProduct.ServiceWarrenty;
                        if (_oProduct.ProductType == (int)EnumProductType.SerialNo || _oProduct.ProductType == (int)EnumProductType.BarCode)
                        {
                            numQTY.Value = 1;
                            numQTY.Enabled = false;
                        }
                        else
                        {
                            numQTY.Enabled = true;
                            numQTY.Value = 0;
                        }
                        txtGrandTotalAmt.Focus();
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


        }

        private void numDownPayment_Enter(object sender, EventArgs e)
        {
            numDownPayment.Select(0, numDownPayment.Text.Length);
        }
        private void numUnitPrice_Enter(object sender, EventArgs e)
        {
            numUnitPrice.Select(0, numUnitPrice.Text.Length);
        }

        private void numQTY_Enter(object sender, EventArgs e)
        {
            numQTY.Select(0, numQTY.Text.Length);
        }

        private void btnMoneyRept_Click(object sender, EventArgs e)
        {
            try
            {
                if (dgvCreditSales.SelectedRows.Count > 0)
                {
                    CreditSalesDetail oCSDetails = (CreditSalesDetail)dgvCreditSales.SelectedRows[0].Tag;
                    _oCSDetails = oCSDetails;
                    MoneyReceipt();
                }
                else
                {
                    MessageBox.Show("Please Select a Schedule", "Select", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgvCreditSales_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (_CreditSale.NoOfInstallment == 1)
                {
                    numAdjustment.Enabled = true;
                }
                else
                {
                    if (dgvCreditSales.Rows.Count != 1 && dgvCreditSales.Rows.Count == (dgvCreditSales.SelectedRows[0].Index + 1))
                    {
                        numAdjustment.Enabled = true;
                    }
                    else
                    {
                        numAdjustment.Enabled = false;
                    }
                }
                CreditSalesDetail dtail = _CreditSale.CreditSalesDetails.FirstOrDefault(o => o.PaymentStatus == "Due");
                numPayment.Value = dtail != null ? (decimal)dtail.InstallmentAmt : 0;
                numPaymentNew.Value = numPayment.Value;
                numCashInstallmentPaid.Value = 0m;
            }
            catch (Exception)
            {

                numAdjustment.Enabled = false;
            }
        }

        private void numInterestRate_ValueChanged(object sender, EventArgs e)
        {
            numTotalInterest.Value = numUTotal.Value * (numInterestRate.Value / 100);
        }

        private void numTotalInterest_ValueChanged(object sender, EventArgs e)
        {
            numSGrand.Value = numUTotal.Value + numTotalInterest.Value;

        }

        private void txtBarcode_TextChanged(object sender, EventArgs e)
        {

            if (ctlProduct.SelectedID == 0)
            {


                try
                {

                    if (txtBarcode.Text != string.Empty)
                    {
                        _StockDetail = (StockDetail)(db.StockDetails.FirstOrDefault(o => o.IMENO == txtBarcode.Text.Trim()));

                        //if (_StockDetail == null)
                        //{
                        //    MessageBox.Show("This product is not available in stock.", "Product Sold.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //    txtBarcode.Text = "";
                        //    return;
                        //}

                        //if (_StockDetail.Status == (int)EnumStockDetailStatus.Sold)
                        //{
                        //    MessageBox.Show("This product already sold.", "Product Sold.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        //    txtBarcode.Text = "";
                        //    txtBarcode.Focus();
                        //    return;

                        //}
                        if (_StockDetail != null)
                        {
                            _oProduct = _StockDetail.Product;

                            if (_oProduct != null)
                            {
                                ctlProduct.SelectedID = _StockDetail.SDetailID;

                                _stock = _StockDetail.Stock;
                                numStock.Value = _stock.Quantity != null ? (decimal)_stock.Quantity : 0;
                                txtMRP.Text = _StockDetail.SalesRate.ToString();

                                numUnitPrice.Value = _StockDetail.SalesRate;
                                numQTY.Value = 0;
                                numQTY.Value = 1;
                                numUnitPrice.Focus();
                            }
                            //txtBarcode.Text = "";
                        }

                    }

                    #region Old Code
                    //_StockDetail = (StockDetail)(db.StockDetails.FirstOrDefault(o => o.IMENO == txtBarcode.Text.Trim()));

                    //    if (_StockDetail == null)
                    //    {
                    //        MessageBox.Show("This product is not available in stock.", "Product Sold.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    //        return;
                    //    }

                    //    if (_StockDetail != null)
                    //    {
                    //        _oProduct = _StockDetail.Product;
                    //        if (_oProduct != null)
                    //        {

                    //            _stock = _StockDetail.Stock;
                    //            numStock.Value = _stock.Quantity != null ? (decimal)_stock.Quantity : 0;
                    //            numMAPRate.Value = _stock.PMPrice != null ? (decimal)_stock.PMPrice : 0;
                    //            numUnitPrice.Value = 0;
                    //            numQTY.Value = 1;
                    //            numUnitPrice.Focus();
                    //        }
                    //    }

                    //    RefrehAfterCheckBC();
                    #endregion

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Barcode");
                }

            }

        }

        private void numWPeriod_ValueChanged(object sender, EventArgs e)
        {
            try
            {

                //dtpWDate.Value = DateTime.Now.AddMonths((int)numWPeriod.Value);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void dgvCreditSales_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // If any cell is clicked on the Second column which is our date Column  
            if (e.ColumnIndex == 1)
            {
                //Initialized a new DateTimePicker Control  
                dateTimePicker1 = new DateTimePicker();

                //Adding DateTimePicker control into DataGridView   
                dgvCreditSales.Controls.Add(dateTimePicker1);

                // Setting the format (i.e. 2014-10-10)  
                dateTimePicker1.Format = DateTimePickerFormat.Custom;
                dateTimePicker1.CustomFormat = "dd MMM yyyyy";

                // It returns the retangular area that represents the Display area for a cell  
                Rectangle oRectangle = dgvCreditSales.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);

                //Setting area for DateTimePicker Control  
                dateTimePicker1.Size = new Size(oRectangle.Width, oRectangle.Height);

                // Setting Location  
                dateTimePicker1.Location = new Point(oRectangle.X, oRectangle.Y);

                // An event attached to dateTimePicker Control which is fired when DateTimeControl is closed  
                dateTimePicker1.CloseUp += new EventHandler(dateTimePicker1_CloseUp);

                dgvCreditSales.CurrentCell.Value = (dateTimePicker1.Text);
                dgvCreditSales.CurrentCell.Value = (dateTimePicker1.Text);

                // An event attached to dateTimePicker Control which is fired when any date is selected  
                //  bd      // dateTimePicker1.TextChanged += new EventHandler(dateTimePicker_OnTextChange);

                // Now make it visible  
                dateTimePicker1.Visible = true;
            }
        }

        private void dateTimePicker1_CloseUp(object sender, EventArgs e)
        {

            dgvCreditSales.CurrentCell.Value = (dateTimePicker1.Text);
            dgvCreditSales.CurrentCell.Value = (dateTimePicker1.Text);
        }
        bool IsSingleInstallmentPaidValid()
        {
            if (numPayment.Value <= 0m)
            {
                MessageBox.Show("Paid amount can't be zero or less than zero.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (numCardPaidAmt.Value > 0 && (int)cmbBank.SelectedValue == 0)
            {
                MessageBox.Show("Please select card.", "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
        bool IsAllPaidValid()
        {
            if ((numPayment.Value + numAdjustment.Value) != _CreditSale.Remaining)
            {
                MessageBox.Show("During all paid, Paid amount and last pay adjustment should be equals to remaining amount.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (_CreditSale.Remaining == 0m)
            {
                MessageBox.Show("This credit sales is already paid.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (numCardPaidAmt.Value > 0 && (int)cmbBank.SelectedValue == 0)
            {
                MessageBox.Show("Please select card.", "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
        private void AllInstallmentsPaid()
        {
            if (!IsAllPaidValid()) return;

            if (_CreditSale.CreditSalesID > 0)
            {
                decimal TotalHireValue = 0;
                decimal TotalNetValue = 0;
                var FirstDueSchedule = _CreditSale.CreditSalesDetails.FirstOrDefault(i => i.PaymentStatus == "Due");
                _ratio = (double)FirstDueSchedule.HireValue / (double)FirstDueSchedule.InstallmentAmt;

                FirstDueSchedule.HireValue = Convert.ToDecimal(numPayment.Value) * (decimal)_ratio;
                FirstDueSchedule.NetValue =Convert.ToDecimal(numPayment.Value)- Convert.ToDecimal(numPayment.Value) * (decimal)_ratio  ;         
                FirstDueSchedule.InstallmentAmt = numPayment.Value;

                FirstDueSchedule.Remarks = "All Paid.";
                FirstDueSchedule.PaymentDate = DateTime.Now;
                FirstDueSchedule.ClosingBalance = 0m;
                FirstDueSchedule.PaymentStatus = "Paid";
                FirstDueSchedule.LastPayAdjustment = numAdjustment.Value;
                TotalHireValue = TotalHireValue + FirstDueSchedule.HireValue;
                TotalNetValue = TotalNetValue + FirstDueSchedule.NetValue;
                _CreditSale.Remaining = 0;
                _CreditSale.WInterestAmt = numAdjustment.Value;
                var AllDueSchedule = _CreditSale.CreditSalesDetails.Where(i => i.PaymentStatus == "Due" && i.CSDetailsID != FirstDueSchedule.CSDetailsID);
                foreach (var item in AllDueSchedule)
                {
                    item.Remarks = "All Paid";
                    item.PaymentDate = DateTime.Now;
                    item.Balance = 0m;
                    item.InstallmentAmt = 0m;
                    item.ClosingBalance = 0m;
                    item.PaymentStatus = "Paid";

                    TotalHireValue = TotalHireValue + item.HireValue;
                    TotalNetValue = TotalNetValue + item.NetValue;

                    item.HireValue = 0m;
                    item.NetValue = 0m;
                }
                FirstDueSchedule.HireValue = TotalHireValue;
                FirstDueSchedule.NetValue = TotalNetValue;
                _oCSDetails = FirstDueSchedule;
                if (!IsSaveValid()) return;
                using (var Transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (cmbCardType.SelectedValue != null && numCardPaidAmt.Value > 0)
                        {
                            int CardTypeSetupID = (int)cmbCardType.SelectedValue;
                            int BankTranID = 0;
                            decimal percentage = 0;
                            decimal CardPaidAmt = numCardPaidAmt.Value;

                            FirstDueSchedule.CardPaidAmount = CardPaidAmt;
                            FirstDueSchedule.CardTypeSetupID = CardTypeSetupID;
                            BankTranID = BankDeposit(CardTypeSetupID, CardPaidAmt, _CreditSale.InvoiceNo + "-" + FirstDueSchedule.ScheduleNo, FirstDueSchedule.PaymentDate.Value, out percentage);
                            FirstDueSchedule.DepositChargePercent = percentage;
                            FirstDueSchedule.BankTranID = BankTranID;
                        }

                        var Customer = db.Customers.FirstOrDefault(i => i.CustomerID == _CreditSale.CustomerID);
                        Customer.CreditDue = Customer.CreditDue - (numPayment.Value + numAdjustment.Value);
                        db.SaveChanges();
                        Transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Transaction.Rollback();
                        MessageBox.Show("Transaction Failed." + Environment.NewLine + ex.Message, "Save Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                }

                MessageBox.Show("All Paid successfully.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                MoneyReceipt();
                RefreshScheduleGrid();
            }
        }

        private void chkIsAllPaid_CheckedChanged(object sender, EventArgs e)
        {
            numCardPaidAmt.Value = 0m;
            if (chkIsAllPaid.Checked)
            {
                numPayment.Value = _CreditSale.Remaining;
                numPaymentNew.Value = numPayment.Value;
                numAdjustment.Enabled = true;
                numCashInstallmentPaid.Value = _CreditSale.Remaining;
            }
            else
            {
                numPayment.Value = 0m;
                numPaymentNew.Value = 0m;
                numAdjustment.Value = 0m;
                numAdjustment.Enabled = false;
                numCashInstallmentPaid.Value = 0m;
            }
        }

        private void numPayment_ValueChanged(object sender, EventArgs e)
        {
            //bd
            if (numAdjustment.Enabled == true)
                numAdjustment.Value = numPaymentNew.Value - numPayment.Value;
        }
        bool IsIncreaseValid(CreditSale oCreditSale)
        {
            if (oCreditSale.Remaining <= 0)
            {
                MessageBox.Show("Due amount is zero or less than zero.You can't increase installments.", "Increase", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            if (Convert.ToDecimal(txtNoOfInstallment.Text) <= 0)
            {
                MessageBox.Show("Installment number can't be  zero or less than zero.", "Increase", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            return true;
        }
        private void btnIncreaseInstall_Click(object sender, EventArgs e)
        {
            if (_CreditSale.CreditSalesID > 0)
            {
                //Validation
                if (!IsIncreaseValid(_CreditSale)) return;

                var allDueSchedules = _CreditSale.CreditSalesDetails.Where(i => i.PaymentStatus == "Due").ToList();
                decimal interestAmt = numEXTInterestAmt.Value;
                decimal PrevInterest = 0, PrevRemaining = 0;
                PrevInterest = _CreditSale.FixedAmt;
                PrevRemaining = _CreditSale.Remaining;

                decimal nTotalBalance = _CreditSale.Remaining + interestAmt;

                decimal TotalNetValue = allDueSchedules.Sum(i => i.NetValue);
                decimal TotalHireValue = allDueSchedules.Sum(i => i.HireValue) + interestAmt;
                decimal NoOfInstallment = Convert.ToDecimal(txtNoOfInstallment.Text);
                decimal InstallmentAmount = nTotalBalance / NoOfInstallment;
                decimal NetValue = TotalNetValue / NoOfInstallment;
                decimal HireValue = TotalHireValue / NoOfInstallment;

                #region _CreditSale Update
                _CreditSale.FixedAmt += interestAmt;
                _CreditSale.InterestRate = Convert.ToInt32(txtInterestRate.Text);
                _CreditSale.NoOfInstallment = Convert.ToInt32(_CreditSale.CreditSalesDetails.Where(i => i.PaymentStatus == "Paid").Count() + NoOfInstallment);
                _CreditSale.Remaining = nTotalBalance;
                _CreditSale.ModifiedBy = Global.CurrentUser.UserID;
                _CreditSale.ModifiedDate = DateTime.Now;
                #endregion

                CreditSalesDetail oCSalesDetail = null;
                int ScheduleNo = 0;
                DateTime dMonth = dtpIssueDate.Value.AddMonths(1);
                var LastPaidScheduel = _CreditSale.CreditSalesDetails.OrderByDescending(i => i.PaymentDate).FirstOrDefault(i => i.PaymentStatus == "Paid");
                if (LastPaidScheduel != null)
                    ScheduleNo = (int)LastPaidScheduel.ScheduleNo;
                for (int i = 0; i < NoOfInstallment; i++)
                {
                    ScheduleNo++;
                    oCSalesDetail = new CreditSalesDetail();
                    oCSalesDetail.MonthDate = dMonth;
                    oCSalesDetail.ScheduleNo = ScheduleNo;
                    oCSalesDetail.PaymentStatus = "Due";
                    oCSalesDetail.InstallmentAmt = InstallmentAmount;
                    oCSalesDetail.HireValue = HireValue;
                    oCSalesDetail.NetValue = NetValue;
                    oCSalesDetail.PaymentDate = dMonth;
                    oCSalesDetail.Balance = nTotalBalance;
                    nTotalBalance -= (decimal)oCSalesDetail.InstallmentAmt;
                    oCSalesDetail.ClosingBalance = nTotalBalance;
                    oCSalesDetail.IsUnExpected = 0;
                    _CreditSale.CreditSalesDetails.Add(oCSalesDetail);
                    dMonth = dMonth.AddMonths(1);
                }
                foreach (var item in allDueSchedules)
                {
                    db.CreditSalesDetails.Remove(item);
                }

                var Customer = db.Customers.FirstOrDefault(i => i.CustomerID == _CreditSale.CustomerID);
                Customer.CreditDue = Customer.CreditDue + interestAmt;
                db.SaveChanges();
                MessageBox.Show("Installment Increase successfull.", "Increase", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshScheduleGrid();
            }

        }

        private void numExtTimeInterestRate_ValueChanged(object sender, EventArgs e)
        {
            if (numExtTimeInterestRate.Value > 0m)
                numEXTInterestAmt.Value = (_CreditSale.Remaining * numExtTimeInterestRate.Value) / 100m;
            else
                numEXTInterestAmt.Value = 0m;
        }

        private void numEXTInterestAmt_ValueChanged(object sender, EventArgs e)
        {
            if (numEXTInterestAmt.Value > 0)
                txtRemaining.Text = (numEXTInterestAmt.Value + _CreditSale.Remaining).ToString("F");
            else
                txtRemaining.Text = (_CreditSale.Remaining).ToString("F");
        }

        private void numAdjustment_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numDownPayment_ValueChanged(object sender, EventArgs e)
        {
            RecalculateNet();
        }
        private void CalculatePaymentAmount()
        {
            if (_IsSaved == false && sAgreement == "OnlyPaid")
                numPayment.Value = numCashInstallmentPaid.Value + numCardPaidAmt.Value;
            else
                numDownPayment.Value = numCashDownPayment.Value + numCardPaidAmt.Value;
        }
        private void numCardPaidAmt_ValueChanged(object sender, EventArgs e)
        {
            CalculatePaymentAmount();
        }

        private void txtGrandTotalAmt_TextChanged(object sender, EventArgs e)
        {
            RecalculateNet();
        }

        private void numCashDownPayment_ValueChanged(object sender, EventArgs e)
        {
            CalculatePaymentAmount();
        }

        private void numCashInstallmentPaid_ValueChanged(object sender, EventArgs e)
        {
            CalculatePaymentAmount();
        }

        private void btnInstallmentRemind_Click(object sender, EventArgs e)
        {

            try
            {
                
             if (dgvCreditSales.SelectedRows.Count > 0)
                {
                    // DataGridViewRow startingBalanceRow = dgvCreditSales.SelectedRows[0].Cells;
                    DateTime paydate = Convert.ToDateTime((dgvCreditSales.SelectedRows[0].Cells[1].Value));
                    CreditSalesDetail oCSDetails = (CreditSalesDetail)dgvCreditSales.SelectedRows[0].Tag;
                    if (oCSDetails.PaymentStatus.Equals("Paid"))
                    {
                        MessageBox.Show("Please select due schedule.", "Paid Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    _oCSDetails = oCSDetails;
                    oCSDetails.RemindDateForInstallment= dtpRemindDateForInstallment.Value;
                    db.SaveChanges();

                    MessageBox.Show("Remind Date Changed successfully.", "Remind DateInformation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                  
                }
                else
                {
                    MessageBox.Show("Please Select a Schedule", "Select", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void numInterestRate_Enter(object sender, EventArgs e)
        {
            numInterestRate.Select(0, numInterestRate.Text.Length);
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void numPrevDue_ValueChanged(object sender, EventArgs e)
        {

        }

        private void txtVoucherNo_TextChanged(object sender, EventArgs e)
        {

        }

        private void dtpSalesDate_ValueChanged(object sender, EventArgs e)
        {

        }

        private void numUTotal_ValueChanged(object sender, EventArgs e)
        {
            numGSTAmt.Value = (numGSTPerc.Value / 100) * numUTotal.Value;
            numCGSTAmt.Value = numGSTAmt.Value * (numCGSTPerc.Value / 100);
            numSGSTAmt.Value = numGSTAmt.Value * (numSGSTPerc.Value / 100);
            numIGSTAmt.Value = numGSTAmt.Value * (numIGSTPerc.Value / 100);
        
        }
    }
}