namespace TaxiLink.UI.Areas.Driver.Models
{
    public class WalletViewModel
    {
        public decimal WalletBalance { get; set; }
        public List<TransactionItemViewModel> Transactions { get; set; } = new List<TransactionItemViewModel>();
    }

    public class TransactionItemViewModel
    {
        public string Title { get; set; }
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public bool IsIncome { get; set; } 
    }
}
