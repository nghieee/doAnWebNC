using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace web_ban_thuoc.Models
{
    // Model cho PayOS request theo documentation mới
    public class PayOSRequest
    {
        [JsonProperty("orderCode")]
        public long OrderCode { get; set; }
        
        [JsonProperty("amount")]
        public long Amount { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("cancelUrl")]
        public string CancelUrl { get; set; } = string.Empty;
        
        [JsonProperty("returnUrl")]
        public string ReturnUrl { get; set; } = string.Empty;
        
        [JsonProperty("signature")]
        public string Signature { get; set; } = string.Empty;
        
        [JsonProperty("items")]
        public List<PayOSItem> Items { get; set; } = new List<PayOSItem>();
        
        [JsonProperty("buyerName")]
        public string BuyerName { get; set; } = string.Empty;
        
        [JsonProperty("buyerEmail")]
        public string BuyerEmail { get; set; } = string.Empty;
        
        [JsonProperty("buyerPhone")]
        public string BuyerPhone { get; set; } = string.Empty;
        
        [JsonProperty("buyerAddress")]
        public string BuyerAddress { get; set; } = string.Empty;
        
        // Thêm fields theo documentation mới
        [JsonProperty("expiredAt")]
        public long? ExpiredAt { get; set; }
        
        // [JsonProperty("language")]
        // public string Language { get; set; } = "vi";
        // [JsonProperty("currency")]
        // public string Currency { get; set; } = "VND";
    }

    // Model cho PayOS item
    public class PayOSItem
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonProperty("quantity")]
        public int Quantity { get; set; }
        
        [JsonProperty("price")]
        public long Price { get; set; }
    }

    // Model cho PayOS response theo API thực tế
    public class PayOSResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonProperty("desc")]
        public string Desc { get; set; } = string.Empty;
        
        [JsonProperty("data")]
        public PayOSData? Data { get; set; }
        
        // Helper properties để tương thích với code cũ
        public int Error => Code == "00" ? 0 : 1;
        public string Message => Desc;
    }

    // Model cho PayOS data theo API mới
    public class PayOSData
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("referenceId")]
        public string ReferenceId { get; set; } = string.Empty;
        
        [JsonProperty("transactions")]
        public List<PayOSTransaction>? Transactions { get; set; }
        
        [JsonProperty("category")]
        public List<string>? Category { get; set; }
        
        [JsonProperty("approvalState")]
        public string ApprovalState { get; set; } = string.Empty;
        
        [JsonProperty("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;
        
        // Legacy properties để tương thích
        [JsonProperty("bin")]
        public string Bin { get; set; } = string.Empty;
        
        [JsonProperty("accountNo")]
        public string AccountNo { get; set; } = string.Empty;
        
        [JsonProperty("accountName")]
        public string AccountName { get; set; } = string.Empty;
        
        [JsonProperty("acqId")]
        public string AcqId { get; set; } = string.Empty;
        
        [JsonProperty("amount")]
        public long Amount { get; set; }
        
        [JsonProperty("format")]
        public string Format { get; set; } = string.Empty;
        
        [JsonProperty("tmnCode")]
        public string TmnCode { get; set; } = string.Empty;
        
        [JsonProperty("txnRef")]
        public string TxnRef { get; set; } = string.Empty;
        
        [JsonProperty("hash")]
        public string Hash { get; set; } = string.Empty;
        
        [JsonProperty("qrCode")]
        public string QrCode { get; set; } = string.Empty;
        
        [JsonProperty("checkoutUrl")]
        public string CheckoutUrl { get; set; } = string.Empty;
    }

    // Model cho PayOS transaction
    public class PayOSTransaction
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;
        
        [JsonProperty("referenceId")]
        public string ReferenceId { get; set; } = string.Empty;
        
        [JsonProperty("amount")]
        public long Amount { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("toBin")]
        public string ToBin { get; set; } = string.Empty;
        
        [JsonProperty("toAccountNumber")]
        public string ToAccountNumber { get; set; } = string.Empty;
        
        [JsonProperty("toAccountName")]
        public string ToAccountName { get; set; } = string.Empty;
        
        [JsonProperty("reference")]
        public string Reference { get; set; } = string.Empty;
        
        [JsonProperty("transactionDatetime")]
        public string TransactionDatetime { get; set; } = string.Empty;
        
        [JsonProperty("errorMessage")]
        public string ErrorMessage { get; set; } = string.Empty;
        
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; } = string.Empty;
        
        [JsonProperty("state")]
        public string State { get; set; } = string.Empty;
    }

    // Model cho PayOS webhook theo documentation
    public class PayOSWebhook
    {
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonProperty("desc")]
        public string Desc { get; set; } = string.Empty;
        
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("data")]
        public PayOSWebhookData? Data { get; set; }
        
        [JsonProperty("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    // Model cho PayOS webhook data
    public class PayOSWebhookData
    {
        [JsonProperty("orderCode")]
        public string OrderCode { get; set; } = string.Empty;
        
        [JsonProperty("amount")]
        public long Amount { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;
        
        [JsonProperty("accountNumber")]
        public string AccountNumber { get; set; } = string.Empty;
        
        [JsonProperty("reference")]
        public string Reference { get; set; } = string.Empty;
        
        [JsonProperty("transactionDateTime")]
        public string TransactionDateTime { get; set; } = string.Empty;
        
        [JsonProperty("currency")]
        public string Currency { get; set; } = string.Empty;
        
        [JsonProperty("paymentLinkId")]
        public string PaymentLinkId { get; set; } = string.Empty;
        
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
        
        [JsonProperty("desc")]
        public string Desc { get; set; } = string.Empty;
        
        [JsonProperty("counterAccountBankId")]
        public string CounterAccountBankId { get; set; } = string.Empty;
        
        [JsonProperty("counterAccountBankName")]
        public string CounterAccountBankName { get; set; } = string.Empty;
        
        [JsonProperty("counterAccountName")]
        public string CounterAccountName { get; set; } = string.Empty;
        
        [JsonProperty("counterAccountNumber")]
        public string CounterAccountNumber { get; set; } = string.Empty;
        
        [JsonProperty("virtualAccountName")]
        public string VirtualAccountName { get; set; } = string.Empty;
        
        [JsonProperty("virtualAccountNumber")]
        public string VirtualAccountNumber { get; set; } = string.Empty;
    }

    // Model cho payment status check
    public class PayOSStatusResponse
    {
        [JsonProperty("error")]
        public int Error { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
        
        [JsonProperty("data")]
        public PayOSStatusData? Data { get; set; }
    }

    public class PayOSStatusData
    {
        [JsonProperty("orderCode")]
        public string OrderCode { get; set; } = string.Empty;
        
        [JsonProperty("amount")]
        public long Amount { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;
        
        [JsonProperty("transactionTime")]
        public long TransactionTime { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    // Model cho email confirmation
    public class OrderConfirmationEmail
    {
        public string OrderCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal VoucherDiscount { get; set; }
        public string VoucherCode { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
} 