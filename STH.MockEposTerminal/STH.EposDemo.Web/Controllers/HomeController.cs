using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using STH.EposDemo.Web.Models;

namespace STH.EposDemo.Web.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// Process a payment using Fingopay cloud services
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult MakeFingerPayment()
        {
            if (string.IsNullOrEmpty(this.Request.Form["txtAuthData"])) return BadRequest();

            var basket = GetBasketItemsList(); //--> basket data will be passed.
            var template = this.Request.Form["txtAuthData"];
            var request = new BiometricPaymentRequest()
            {
                Template = template,
                MerchantId = "Sthaler-Fingopay-Demo-POS-101",
                TotalAmount = GetTotalFromBasketItems(basket),
                BasketItems = basket,
                TransactionId = Guid.NewGuid().ToString(),
                Currency = "GBP"
            };

            var initialdatetime = DateTime.UtcNow;
            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:9001/");
                client.Timeout = new TimeSpan(0,1,5,0);
                var result = client.PostAsync("/api/bir/makepayment",
                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")).Result;
                if (!result.IsSuccessStatusCode) return BadRequest(new[] {"Error processing request"});
                // get response back from server with a transaction id and message

                //Deserialize
                var data = result.Content.ReadAsStringAsync().Result;
                var response = JsonConvert.DeserializeObject<BiometricPaymentResponse>(data);
                return Ok(new BiometricPaymentResponse()
                {
                    Transaction = request,
                    InitialStartDateTime = initialdatetime,
                    RemoteProcessStartDateTime = response.RemoteProcessStartDateTime,
                    RemoteProcessEndDateTime = response.RemoteProcessEndDateTime,
                    ResponseDateTime = DateTime.UtcNow,
                    UniqueReferenceId = Guid.NewGuid().ToString()
                });

            }
        }

        private double GetQuantityTotalFromBasketItems(IEnumerable<BasketItem> basket)
        {
            return basket.Select(x => x.Quantity).Sum();
        }
        private double GetDiscountTotalFromBasketItems(IEnumerable<BasketItem> basket)
        {
            return basket.Select(x => x.Discount).Sum();
        }
        private double GetTotalFromBasketItems(IEnumerable<BasketItem> basket)
        {
            return basket.Select(x => (x.UnitPrice-x.Discount) * x.Quantity).Sum();
        }

        private IEnumerable<BasketItem> GetBasketItemsList()
        {
            return new[]
            {
                new BasketItem()
                {
                    Barcode = "XXX-000-1-2-3-4",
                    Discount = 0.04,
                    Name = "Bananas",
                    Quantity = 3,
                    UnitPrice = 3.14,
                    Currency = "GBP"
                },
                new BasketItem()
                {
                    Barcode = "XXX-222-1-2-3-4",
                    Discount = 5.10,
                    Name = "Victoria Sponge Cake",
                    Quantity = 10,
                    UnitPrice  = 16.10,
                    Currency = "GBP"
                },
                new BasketItem()
                {
                    Barcode = "XXX-333-1-2-3-4",
                    Discount = 0.3,
                    Name = "Mars Bar",
                    Quantity = 3,
                    UnitPrice  = 1,
                    Currency = "GBP"
                }
            };
        }

        /// <summary>
        /// Process a payment using Fingopay cloud services
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public IActionResult EnrollAndPay()
        {
            if (string.IsNullOrEmpty(this.Request.Form["txtAuthData"])) return BadRequest();
                // if (string.IsNullOrEmpty(this.Request.Form["txtEnrollmentPin"])) return BadRequest();
            //if (string.IsNullOrEmpty(this.Request.Form["txtAccountId"])) return BadRequest();

            var basket = GetBasketItemsList();
            var enrollmentPinCode = this.Request.Form["txtEnrollmentPin"].ToString() ?? "ABCD"; ;
            var accountId = this.Request.Form["txtAccountId"].ToString() ?? "1010101";
            var template = this.Request.Form["txtAuthData"];
            // -> pin and account id would be pre set by the qr code present request for demo- manual
            var request = new BiometricEnrollAndPaymentRequest()
            {
                AccountId = accountId,
                EnrollmentPinCode = enrollmentPinCode,
                Template = template,
                MerchantId = "Sthaler-Fingopay-Demo-POS-101",
                TotalAmount = GetTotalFromBasketItems(basket),
                BasketItems = basket,
                Currency = "GBP",
                TransactionId = Guid.NewGuid().ToString()
            };

            var initialdatetime = DateTime.UtcNow;
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://localhost:9001/");
                client.Timeout = new TimeSpan(0, 1, 5, 0);
                var result = client.PostAsync("/api/bir/EnrollAndPayment",
                    new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json")).Result;
                if (!result.IsSuccessStatusCode) return BadRequest(new[] {"error processing request"});
                // get response back from server with a transaction id and message
                //Deserialize
                var data = result.Content.ReadAsStringAsync().Result;
                var response = JsonConvert.DeserializeObject<BiometricEnrollAndPaymentResponse>(data);
                return Ok(new BiometricEnrollAndPaymentResponse()
                {
                    Transaction = request,
                    InitialStartDateTime = initialdatetime,
                    RemoteProcessStartDateTime = response.RemoteProcessStartDateTime,
                    RemoteProcessEndDateTime = response.RemoteProcessEndDateTime,
                    ResponseDateTime = DateTime.UtcNow,
                    UniqueReferenceId = Guid.NewGuid().ToString(),
                    Username = response.Username
                });

            }
        }


        /// <summary>
        /// Process a payment using Fingopay cloud services
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult MakeFingerPaymentGet([FromBody] string request)
        {
            if (string.IsNullOrEmpty(request)) return BadRequest();

            //var template = request["txtAuth"];
            return RedirectToRoute("ThankYou");
        }
    }

    public class BiometricEnrollAndPaymentRequest
    {
        public string AccountId { get; set; }
        public string EnrollmentPinCode { get; set; }
        public string Template { get; set; }
        public IEnumerable<BasketItem> BasketItems { get; set; }
        public string MerchantId { get; set; }
        public double TotalAmount { get; set; }
        public string TransactionId { get; set; }
        public string Currency { get; set; }
    }

    public class BiometricPaymentRequest
    {
        public string Template { get; set; }
        public IEnumerable<BasketItem> BasketItems { get; set; }
        public string MerchantId { get; set; }
        public double TotalAmount { get; set; }
        public string TransactionId { get; set; }
        public string Currency { get; set; }
    }

    public class BiometricEnrollAndPaymentResponse
    {   public string Username { get; set; } 
        public BiometricEnrollAndPaymentRequest Transaction { get; set; }
        public string UniqueReferenceId { get; set; }
        public DateTime InitialStartDateTime { get; set; }
        public DateTime RemoteProcessStartDateTime { get; set; }
        public DateTime RemoteProcessEndDateTime { get; set; }
        public DateTime ResponseDateTime { get; set; }
    }

    public class BiometricPaymentResponse
    {
        public string UniqueReferenceId { get; set; }
        public BiometricPaymentRequest Transaction { get; set; }
        public DateTime InitialStartDateTime { get; set; }
        public DateTime RemoteProcessStartDateTime { get; set; }
        public DateTime RemoteProcessEndDateTime { get; set; }
        public DateTime ResponseDateTime { get; set; }
    }

    public class BasketItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double Discount { get; set; }
        public string Barcode { get; set; }
        public string Currency { get; set; }
    }
}
