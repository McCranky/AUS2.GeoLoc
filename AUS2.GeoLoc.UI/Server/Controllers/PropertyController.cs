using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AUS2.GeoLoc.UI.Server.Data;
using AUS2.GeoLoc.UI.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AUS2.GeoLoc.UI.Server.Controllers
{
    public class PropertyController : Controller
    {
        private readonly PropertyStorage _context;

        public PropertyController(PropertyStorage context)
        {
            _context = context;
        }

        [HttpGet("save")]
        public IActionResult Save()
        {
            _context.Save();
            return Ok();
        }

        [HttpGet("seed/{count}")]
        public IActionResult SeedData([FromRoute] int count)
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
            var result = _context.GetBlockAddresses();
            return Ok(result);
        }

        [HttpGet("address/{address}")]
        public IActionResult GetAllFromAddress([FromRoute] int address)
        {
            var result = _context.GetAllFromAddress(address);
            return Ok(result);
        }

        [HttpGet("overflow/{address}")]
        public IActionResult GetAllFromOverflow([FromRoute] int address)
        {
            var result = _context.GetAllFromOverflow(address);
            return Ok(result);
        }

        [HttpGet("property/{id}")]
        public IActionResult Get([FromRoute] int id)
        {
            var result = _context.GetPropertyById(id);
            if (result != null) {
                return Ok(result);
            }
            return BadRequest();
        }

        [HttpPost("property")]
        public IActionResult Add([FromBody] Property property)
        {
            _context.AddProperty(ref property);
            return Ok(property);
        }

        [HttpPut("save")]
        public IActionResult Update([FromBody] Property property)
        {
            if (_context.UpdateProperty(property)) {
                return Ok(property);
            }
            return BadRequest();
        }

        [HttpDelete("delete/{id}")]
        public IActionResult Delete([FromRoute] int id)
        {
            if (_context.DeleteProperty(id)) {
                return NoContent();
            }
            return BadRequest();
        }
    }
}
