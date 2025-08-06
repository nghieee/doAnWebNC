using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;
using web_ban_thuoc.Models;
using web_ban_thuoc.Services;

namespace web_ban_thuoc.Controllers
{
    [Authorize]
    public class PayOSController : Controller
    {
        private readonly IPayOSService _payOSService;
        private readonly IOrderEmailService _orderEmailService;
        private readonly LongChauDbContext _context;
        private readonly ILogger<PayOSController> _logger;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public PayOSController(IPayOSService payOSService, IOrderEmailService orderEmailService, 
            LongChauDbContext context, ILogger<PayOSController> logger, UserManager<IdentityUser> userManager,
            IConfiguration configuration)
        {
            _payOSService = payOSService;
            _orderEmailService = orderEmailService;
            _context = context;
            _logger = logger;
            _userManager = userManager;
            _configuration = configuration;
        }

        // Hiển thị trang tạo thanh toán PayOS
        [HttpGet]
        public async Task<IActionResult> CreatePayment(int orderId)
        {
            try
            {
                // Lấy thông tin đơn hàng
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return RedirectToAction("Index", "Cart");
                }

                // Kiểm tra quyền truy cập
                var currentUserId = _userManager.GetUserId(User);
                if (order.UserId != currentUserId)
                {
                    return RedirectToAction("Index", "Cart");
                }

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PayOS create payment page");
                return RedirectToAction("Index", "Cart");
            }
        }

        // Model cho request tạo thanh toán
        public class CreatePaymentRequest
        {
            public int OrderId { get; set; }
        }

        // Tạo thanh toán PayOS
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                _logger.LogInformation($"PayOS CreatePayment called for orderId: {request.OrderId}");
                
                // Lấy thông tin đơn hàng
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

                if (order == null)
                {
                    _logger.LogError($"Order not found for orderId: {request.OrderId}");
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                _logger.LogInformation($"Order Details:");
                _logger.LogInformation($"  OrderId: {order.OrderId}");
                _logger.LogInformation($"  UserId: {order.UserId}");
                _logger.LogInformation($"  TotalAmount: {order.TotalAmount}");
                _logger.LogInformation($"  FullName: {order.FullName}");
                _logger.LogInformation($"  Phone: {order.Phone}");
                _logger.LogInformation($"  ShippingAddress: {order.ShippingAddress}");
                _logger.LogInformation($"  OrderItems Count: {order.OrderItems?.Count ?? 0}");

                // Debug logging
                var currentUserId = _userManager.GetUserId(User);
                _logger.LogInformation($"Current user ID: {currentUserId}");
                _logger.LogInformation($"Order user ID: {order.UserId}");
                _logger.LogInformation($"User authenticated: {User.Identity?.IsAuthenticated}");

                // Kiểm tra quyền truy cập - cho phép nếu user đã đăng nhập và là chủ đơn hàng
                if (User.Identity?.IsAuthenticated != true)
                {
                    _logger.LogError("User not authenticated");
                    return Json(new { success = false, message = "Vui lòng đăng nhập để thanh toán" });
                }

                if (order.UserId != currentUserId)
                {
                    _logger.LogError($"User {currentUserId} not authorized for order {request.OrderId} (order belongs to {order.UserId})");
                    return Json(new { success = false, message = "Không có quyền truy cập đơn hàng này" });
                }

                _logger.LogInformation($"Processing PayOS payment for order {request.OrderId}, amount: {order.TotalAmount}");

                // Tạo mã đơn hàng PayOS theo documentation (number)
                var orderCode = long.Parse($"{order.OrderId}{DateTime.Now:yyyyMMddHHmmss}");
                _logger.LogInformation($"Generated OrderCode: {orderCode}");

                // Tạo danh sách sản phẩm cho PayOS
                var items = order.OrderItems.Select(oi => new PayOSItem
                {
                    Name = oi.Product?.ProductName ?? "Sản phẩm",
                    Quantity = oi.Quantity,
                    Price = (long)oi.Price
                }).ToList();

                _logger.LogInformation($"Order Items Details:");
                foreach (var item in items)
                {
                    _logger.LogInformation($"  Item: {item.Name}, Quantity: {item.Quantity}, Price: {item.Price}");
                }

                // Tạo request PayOS
                var payOSRequest = new PayOSRequest
                {
                    OrderCode = orderCode,
                    Amount = (long)(order.TotalAmount ?? 0),
                    Description = ($"Thanh toán đơn hàng #{order.OrderId} - Nhà Thuốc Long Châu").Substring(0, Math.Min(25, $"Thanh toán đơn hàng #{order.OrderId} - Nhà Thuốc Long Châu".Length)),
                    CancelUrl = $"{Request.Scheme}://{Request.Host}/PayOS/Cancel?orderId={order.OrderId}",
                    ReturnUrl = $"{Request.Scheme}://{Request.Host}/PayOS/Return?orderId={order.OrderId}",
                    Items = items,
                    BuyerName = order.FullName ?? "Khách hàng",
                    BuyerEmail = User.Identity?.Name ?? "",
                    BuyerPhone = order.Phone ?? "",
                    BuyerAddress = order.ShippingAddress ?? "",
                    ExpiredAt = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
                };

                _logger.LogInformation($"PayOS Request Details:");
                _logger.LogInformation($"  OrderCode: {payOSRequest.OrderCode}");
                _logger.LogInformation($"  Amount: {payOSRequest.Amount}");
                _logger.LogInformation($"  Description: {payOSRequest.Description}");
                _logger.LogInformation($"  CancelUrl: {payOSRequest.CancelUrl}");
                _logger.LogInformation($"  ReturnUrl: {payOSRequest.ReturnUrl}");
                _logger.LogInformation($"  BuyerName: {payOSRequest.BuyerName}");
                _logger.LogInformation($"  BuyerEmail: {payOSRequest.BuyerEmail}");
                _logger.LogInformation($"  BuyerPhone: {payOSRequest.BuyerPhone}");
                _logger.LogInformation($"  Items Count: {payOSRequest.Items.Count}");

                // Gọi API PayOS
                _logger.LogInformation($"Calling PayOS API...");
                var payOSResponse = await _payOSService.CreatePaymentAsync(payOSRequest);

                _logger.LogInformation($"PayOS API Response: Code={payOSResponse.Code}, Desc={payOSResponse.Desc}");
                _logger.LogInformation($"PayOS API Response Details: {JsonConvert.SerializeObject(payOSResponse)}");
                if (payOSResponse.Data != null)
                {
                    _logger.LogInformation($"PayOS Data: TxnRef={payOSResponse.Data.TxnRef}, CheckoutUrl={payOSResponse.Data.CheckoutUrl}");
                }

                if (payOSResponse.Code == "00" && payOSResponse.Data != null)
                {
                    var transactionId = payOSResponse.Data.Id ?? payOSResponse.Data.TxnRef ?? payOSResponse.Data.ReferenceId;
                    var checkoutUrl = payOSResponse.Data.CheckoutUrl ?? $"https://payos.vn/payment/{payOSResponse.Data.Id}";
                    
                    _logger.LogInformation($"PayOS payment created successfully. Id: {payOSResponse.Data.Id}, TransactionId: {transactionId}, CheckoutUrl: {checkoutUrl}");
                    
                    // Cập nhật thông tin payment trong database
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.OrderId == order.OrderId);

                    if (payment != null)
                    {
                        payment.TransactionId = transactionId;
                        payment.PaymentStatus = "Pending";
                        payment.PaymentDate = DateTime.Now;
                    }
                    else
                    {
                        payment = new Payment
                        {
                            OrderId = order.OrderId,
                            PaymentMethod = "PayOS",
                            Amount = order.TotalAmount,
                            PaymentStatus = "Pending",
                            PaymentDate = DateTime.Now,
                            TransactionId = transactionId
                        };
                        _context.Payments.Add(payment);
                    }

                    await _context.SaveChangesAsync();

                    // Gửi email xác nhận đơn hàng
                    var orderEmailData = new OrderConfirmationEmail
                    {
                        OrderCode = orderCode.ToString(),
                        CustomerName = order.FullName ?? "",
                        CustomerEmail = User.Identity?.Name ?? "",
                        CustomerPhone = order.Phone ?? "",
                        ShippingAddress = order.ShippingAddress ?? "",
                        TotalAmount = order.TotalAmount ?? 0,
                        VoucherDiscount = order.VoucherDiscount ?? 0,
                        VoucherCode = order.VoucherCode ?? "",
                        OrderDate = order.OrderDate ?? DateTime.Now,
                        PaymentMethod = "PayOS",
                        OrderItems = order.OrderItems.ToList()
                    };

                    await _orderEmailService.SendOrderConfirmationEmailAsync(orderEmailData);

                    return Json(new
                    {
                        success = true,
                        checkoutUrl = checkoutUrl,
                        orderCode = orderCode
                    });
                }
                else
                {
                    _logger.LogError($"PayOS API Error: Code={payOSResponse.Code}, Desc={payOSResponse.Desc}");
                    _logger.LogError($"PayOS API Response Details: {JsonConvert.SerializeObject(payOSResponse)}");
                    return Json(new { success = false, message = $"PayOS Error: {payOSResponse.Desc}", details = payOSResponse });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayOS payment");
                _logger.LogError($"Exception Details: {ex.Message}");
                _logger.LogError($"Stack Trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Server Error: {ex.Message}", details = ex.StackTrace });
            }
        }

        // Trang return sau khi thanh toán
        [HttpGet]
        public async Task<IActionResult> Return(int orderId, string orderCode)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId);

                if (order == null)
                {
                    return RedirectToAction("Error", "Home");
                }

                // Kiểm tra trạng thái thanh toán
                var statusResponse = await _payOSService.CheckPaymentStatusAsync(orderCode);

                if (statusResponse.Error == 0 && statusResponse.Data != null)
                {
                    var payment = await _context.Payments
                        .FirstOrDefaultAsync(p => p.OrderId == orderId);

                    if (statusResponse.Data.Status == "PAID")
                    {
                        // Thanh toán thành công
                        if (payment != null)
                        {
                            payment.PaymentStatus = "Completed";
                            payment.PaymentDate = DateTime.Now;
                        }

                        order.PaymentStatus = "Đã thanh toán";
                        order.Status = "Đã xác nhận";

                        await _context.SaveChangesAsync();

                        // Gửi email thành công
                        var orderEmailData = new OrderConfirmationEmail
                        {
                            OrderCode = orderCode.ToString(),
                            CustomerName = order.FullName ?? "",
                            CustomerEmail = User.Identity?.Name ?? "",
                            CustomerPhone = order.Phone ?? "",
                            ShippingAddress = order.ShippingAddress ?? "",
                            TotalAmount = order.TotalAmount ?? 0,
                            VoucherDiscount = order.VoucherDiscount ?? 0,
                            VoucherCode = order.VoucherCode ?? "",
                            OrderDate = order.OrderDate ?? DateTime.Now,
                            PaymentMethod = "PayOS",
                            OrderItems = order.OrderItems.ToList()
                        };

                        await _orderEmailService.SendPaymentSuccessEmailAsync(orderEmailData);

                        TempData["SuccessMessage"] = "Thanh toán thành công!";
                        return RedirectToAction("Success", new { orderId = orderId });
                    }
                    else
                    {
                        // Thanh toán thất bại
                        if (payment != null)
                        {
                            payment.PaymentStatus = "Failed";
                        }

                        order.PaymentStatus = "Thanh toán thất bại";

                        await _context.SaveChangesAsync();

                        // Gửi email thất bại
                        var orderEmailData = new OrderConfirmationEmail
                        {
                            OrderCode = orderCode.ToString(),
                            CustomerName = order.FullName ?? "",
                            CustomerEmail = User.Identity?.Name ?? "",
                            CustomerPhone = order.Phone ?? "",
                            ShippingAddress = order.ShippingAddress ?? "",
                            TotalAmount = order.TotalAmount ?? 0,
                            VoucherDiscount = order.VoucherDiscount ?? 0,
                            VoucherCode = order.VoucherCode ?? "",
                            OrderDate = order.OrderDate ?? DateTime.Now,
                            PaymentMethod = "PayOS",
                            OrderItems = order.OrderItems.ToList()
                        };

                        await _orderEmailService.SendPaymentFailedEmailAsync(orderEmailData);

                        TempData["ErrorMessage"] = "Thanh toán thất bại!";
                        return RedirectToAction("Failed", new { orderId = orderId });
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể kiểm tra trạng thái thanh toán!";
                    return RedirectToAction("Failed", new { orderId = orderId });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS return");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý thanh toán!";
                return RedirectToAction("Failed", new { orderId = orderId });
            }
        }

        // Trang cancel
        [HttpGet]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = "Đã hủy";
                await _context.SaveChangesAsync();
            }

            TempData["ErrorMessage"] = "Thanh toán đã bị hủy!";
            return RedirectToAction("Failed", new { orderId = orderId });
        }

        // Trang thành công
        [HttpGet]
        public async Task<IActionResult> Success(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return RedirectToAction("Error", "Home");
            }

            return View(order);
        }

        // Trang thất bại
        [HttpGet]
        public async Task<IActionResult> Failed(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null)
            {
                return RedirectToAction("Error", "Home");
            }

            return View(order);
        }

        // Webhook endpoint cho PayOS
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Webhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation($"PayOS Webhook received: {body}");

                var webhook = JsonConvert.DeserializeObject<PayOSWebhook>(body);
                if (webhook == null)
                {
                    return BadRequest("Invalid webhook data");
                }

                // Verify signature theo documentation PayOS
                var dataToVerify = $"{webhook.Code}|{webhook.Desc}|{webhook.Success}";
                if (webhook.Data != null)
                {
                    dataToVerify += $"|{webhook.Data.OrderCode}|{webhook.Data.Amount}|{webhook.Data.Description}|{webhook.Data.Reference}|{webhook.Data.TransactionDateTime}";
                }
                
                if (!_payOSService.VerifyWebhookSignature(webhook.Signature, dataToVerify))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return BadRequest("Invalid signature");
                }

                // Xử lý webhook
                if (webhook.Data == null)
                {
                    _logger.LogWarning("Webhook data is null");
                    return BadRequest("Invalid webhook data");
                }

                var orderCode = webhook.Data.OrderCode;
                var orderId = int.Parse(orderCode.Split('_')[1]);

                var order = await _context.Orders.FindAsync(orderId);
                var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId);

                if (order != null && payment != null)
                {
                    if (webhook.Success && webhook.Data.Code == "00")
                    {
                        // Thanh toán thành công
                        payment.PaymentStatus = "Completed";
                        payment.PaymentDate = DateTime.Now;
                        order.PaymentStatus = "Đã thanh toán";
                        order.Status = "Đã xác nhận";

                        await _context.SaveChangesAsync();

                        // Gửi email thành công
                        var orderEmailData = new OrderConfirmationEmail
                        {
                            OrderCode = orderCode.ToString(),
                            CustomerName = order.FullName ?? "",
                            CustomerEmail = order.UserId ?? "",
                            CustomerPhone = order.Phone ?? "",
                            ShippingAddress = order.ShippingAddress ?? "",
                            TotalAmount = order.TotalAmount ?? 0,
                            VoucherDiscount = order.VoucherDiscount ?? 0,
                            VoucherCode = order.VoucherCode ?? "",
                            OrderDate = order.OrderDate ?? DateTime.Now,
                            PaymentMethod = "PayOS",
                            OrderItems = new List<OrderItem>() // Sẽ load từ database nếu cần
                        };

                        await _orderEmailService.SendPaymentSuccessEmailAsync(orderEmailData);

                        _logger.LogInformation($"Payment completed for order {orderId}");
                    }
                    else
                    {
                        // Thanh toán thất bại
                        payment.PaymentStatus = "Failed";
                        order.PaymentStatus = "Thanh toán thất bại";

                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"Payment failed for order {orderId}");
                    }
                }

                return Ok("Webhook processed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayOS webhook");
                return StatusCode(500, "Internal server error");
            }
        }

        // Test endpoint để kiểm tra PayOS configuration
        [HttpGet]
        public IActionResult Test()
        {
            try
            {
                var config = new
                {
                    ClientId = _configuration["PayOS:ClientId"],
                    ApiKey = _configuration["PayOS:ApiKey"],
                    ChecksumKey = _configuration["PayOS:ChecksumKey"],
                    BaseUrl = _configuration["PayOS:BaseUrl"]
                };
                
                // Test signature generation với format đơn giản
                var testData = "TEST_ORDER|10000|Test payment";
                var signature = _payOSService.GenerateSignature(testData);
                
                // Test với format khác
                var testData2 = "10000|TEST_ORDER|Test payment";
                var signature2 = _payOSService.GenerateSignature(testData2);
                
                // Test với format đầy đủ
                var testData3 = "TEST_ORDER|10000|Test payment|http://localhost/cancel|http://localhost/return";
                var signature3 = _payOSService.GenerateSignature(testData3);
                
                return Json(new { 
                    message = "PayOS Controller is working",
                    config = config,
                    testSignature = signature,
                    testData = testData,
                    testSignature2 = signature2,
                    testData2 = testData2,
                    testSignature3 = signature3,
                    testData3 = testData3,
                    userAuthenticated = User.Identity?.IsAuthenticated,
                    userName = User.Identity?.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Test endpoint");
                return Json(new { error = ex.Message });
            }
        }

        // Test endpoint để test với format signature đúng
        [HttpGet]
        public async Task<IActionResult> TestCorrectSignature()
        {
            try
            {
                var testRequest = new PayOSRequest
                {
                    OrderCode = long.Parse($"1{DateTime.Now:yyyyMMddHHmmss}"),
                    Amount = 10000,
                    Description = "Correct signature test payment",
                    CancelUrl = $"{Request.Scheme}://{Request.Host}/PayOS/Cancel?orderId=1",
                    ReturnUrl = $"{Request.Scheme}://{Request.Host}/PayOS/Return?orderId=1",
                    Items = new List<PayOSItem>
                    {
                        new PayOSItem
                        {
                            Name = "Correct Signature Test Product",
                            Quantity = 1,
                            Price = 10000
                        }
                    },
                    BuyerName = "Correct Signature Test User",
                    BuyerEmail = "test@example.com",
                    BuyerPhone = "0123456789",
                    BuyerAddress = "Correct Signature Test Address",
                    ExpiredAt = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
                };

                // Test với nhiều format signature khác nhau
                var dataToSign1 = $"{testRequest.OrderCode}|{testRequest.Amount}|{testRequest.Description}";
                var dataToSign2 = $"{testRequest.Amount}|{testRequest.OrderCode}|{testRequest.Description}";
                var dataToSign3 = $"{testRequest.OrderCode}|{testRequest.Amount}|{testRequest.Description}|{testRequest.CancelUrl}|{testRequest.ReturnUrl}";
                
                var signature1 = _payOSService.GenerateSignature(dataToSign1);
                var signature2 = _payOSService.GenerateSignature(dataToSign2);
                var signature3 = _payOSService.GenerateSignature(dataToSign3);

                // Sử dụng format 3 (đầy đủ nhất)
                testRequest.Signature = signature3;

                var jsonRequest = JsonConvert.SerializeObject(testRequest);
                var content = new StringContent(jsonRequest, Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));

                var httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri("https://api-merchant.payos.vn");
                httpClient.DefaultRequestHeaders.Add("x-client-id", _configuration["PayOS:ClientId"]);
                httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["PayOS:ApiKey"]);
                httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                httpClient.DefaultRequestHeaders.Add("User-Agent", "PayOS-Integration/1.0");

                var response = await httpClient.PostAsync("/v2/payment-requests", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                return Json(new
                {
                    testRequest = testRequest,
                    dataToSign1 = dataToSign1,
                    signature1 = signature1,
                    dataToSign2 = dataToSign2,
                    signature2 = signature2,
                    dataToSign3 = dataToSign3,
                    signature3 = signature3,
                    responseStatus = response.StatusCode,
                    responseContent = responseContent,
                    config = new
                    {
                        ClientId = _configuration["PayOS:ClientId"],
                        ApiKey = _configuration["PayOS:ApiKey"],
                        ChecksumKey = _configuration["PayOS:ChecksumKey"],
                        BaseUrl = _configuration["PayOS:BaseUrl"]
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TestCorrectSignature");
                return Json(new { error = ex.Message, stackTrace = ex.StackTrace });
            }
        }
    }
} 