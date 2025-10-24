using InventorySync.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InventorySync.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InfigoController : ControllerBase
    {
        private readonly IInfigoService _infigoService;

        public InfigoController(IInfigoService infigoService)
        {
            _infigoService = infigoService;
        }

    }
}
