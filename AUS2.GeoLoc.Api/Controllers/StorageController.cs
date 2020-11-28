using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AUS2.GeoLoc.Api.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AUS2.GeoLoc.Api.Controllers
{
    public class StorageController : Controller
    {
        private readonly PropertiesStorage _context;

        public StorageController(PropertiesStorage context)
        {
            _context = context;
        }

        [HttpGet("seed/{count}")]
        public IActionResult SeedData([FromQuery] int count)
        {
            if (_context.CanSeed) {
                _context.SeedData(count);
                return Ok("Data Successfully seeded.");
            } else {
                return BadRequest("Data can not be seeded.");
            }
        }

        [HttpGet("addresses")]
        public IActionResult GetAddresses()
        {
            return Ok(_context.GetBlockAddresses());
        }
    }
}
