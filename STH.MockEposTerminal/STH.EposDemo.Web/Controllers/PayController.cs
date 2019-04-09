using Microsoft.AspNetCore.Mvc;

namespace STH.EposDemo.Web.Controllers
{
    public class PayController : Controller
    {
        private readonly IFingopayService _fingopayService;

        public PayController(IFingopayService fingopayService)
        {
            _fingopayService = fingopayService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }



    }

    public interface IFingopayService
    {
    }
}