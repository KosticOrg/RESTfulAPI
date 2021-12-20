using DataLayer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NSwag.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace RESTfulWebAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Student")]
    [Route("[controller]")]
    [ApiController]
    [Produces("application/xml")]
    [OpenApiTag("Address", Description = "Methods to work with Addresses")]
    public class AddressController : ControllerBase
    {
        private readonly WebAPIModel database;
        public AddressController(WebAPIModel database)
        {
            this.database = database;
        }      
       
        [HttpGet]
        [SwaggerResponse(HttpStatusCode.OK, typeof(List<Address>), Description = "Successfully Returned List of Address")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "List of Address is Empty")]
        public async Task<IActionResult> Get()
        {
            IList<Address> addresses = await database.Addresses
                .Include(x => x.Students)
                .Select(x => new Address 
                { 
                   Id = x.Id,
                   Street = x.Street,
                   City = x.City,
                   Country = x.Country,
                   AdditionalInfo = new AdditionalInfo 
                   {
                      StreetNumber = x.AdditionalInfo.StreetNumber,
                      AdditionalNumber = x.AdditionalInfo.AdditionalNumber,
                      Zip = x.AdditionalInfo.Zip
                   },
                   Students = x.Students
                })
                .ToListAsync();
            if (addresses.Count != 0)
                return Ok(addresses);
            else
                return NotFound();
        }       
        
        [HttpGet("getById/{id}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(Address), Description = "Successfully Returned Address")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "There is no Address with that Id")]
        public async Task<IActionResult> GetById(int id)
        {
            Address address = await database.Addresses.FindAsync(id);
            if (address != null)
            {
                database.Entry(address).Collection(x => x.Students).Load();
                string json = JsonConvert.SerializeObject(
                       address,
                       Formatting.None,
                       new JsonSerializerSettings()
                       {
                           ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                           NullValueHandling = NullValueHandling.Ignore
                       });
                return Ok(json);
            }               
            else
                return NotFound(id);
        }
       
        [HttpGet("getByStreet/{street}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(List<Address>), Description = "Successfully Returned List of Addresses")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "There is no Addresses with that Street")]
        public async Task<IActionResult> GetByStreet(string street)
        {
            IList<Address> addresses = await database.Addresses.Include(x => x.Students).Where(x => x.Street == street).ToListAsync();
            if (addresses != null)
            {              
                string json = JsonConvert.SerializeObject(
                       addresses,
                       Formatting.None,
                       new JsonSerializerSettings()
                       {
                           ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                           NullValueHandling = NullValueHandling.Ignore
                       });
                return Ok(json);
            }                
            else
                return NotFound(street);
        }
        
        [HttpGet("getByCity/{city}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(List<Address>), Description = "Successfully Returned List of Addresses")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "There is no Addresses with that City")]
        public async Task<IActionResult> GetByCity(string city)
        {
            var addresses = await database.Addresses
                .Include(x => x.Students)
                .Select(x => new
                {
                    x.Id,
                    x.Street,
                    x.City,
                    x.Country,
                    x.AdditionalInfo.StreetNumber,
                    x.AdditionalInfo.AdditionalNumber,
                    ZipCode = x.AdditionalInfo.Zip,
                    Students = x.Students.Select(y => new
                    {
                      y.FirstName,
                      y.LastName,
                      y.StudentNumber
                    }).ToList()
                })
                .Where(x => x.City == city)
                .ToListAsync();
            if (addresses.Count != 0)
                return Ok(addresses);  
            else
                return NotFound(city);
        }
      
        [HttpGet("getByStreetAndCity")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(Address), Description = "Successfully Returned Address")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "There is no Address with that Street in that City")]
        public async Task<IActionResult> GetByStreetAndCity(string street,string city)
        {
            Address address = await database.Addresses.Where(x => x.Street == street && x.City == city).SingleOrDefaultAsync();
            if (address != null)
                return Ok(address);
            else
                return NotFound(new {street,city});
        }
      
        [HttpPost]
        [SwaggerResponse(HttpStatusCode.Created, typeof(Address), Description = "Successfully Created Address")]
        public async Task<IActionResult> Post(Address address)
        {
            database.Add<Address>(address);
            try
            {
                await database.SaveChangesAsync();
            }
            catch (Exception e)
            {
                return StatusCode(500,new { Error = e.Message });
            }          
            return CreatedAtAction("GetById", new {id = address.Id},address);
        }
      
        [HttpPut]
        [SwaggerResponse(HttpStatusCode.NoContent, null, Description = "Successfully Updated Address")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "Address you want to Modify does not excist")]
        public async Task<IActionResult> Put(Address address)
        {          
            Address originAddress = await database.Addresses.Where(x => x.Id == address.Id).AsNoTracking().SingleOrDefaultAsync();
            if (originAddress != null)
            {
                database.Update<Address>(address);
                try
                {
                    await database.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    return StatusCode(500, new { Error = e.Message });
                }
                return Ok(address);
            }
            else
                return NotFound(address);           
        }
      
        [HttpDelete("{id}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(Address), Description = "Successfully Deleted Address")]
        [SwaggerResponse(HttpStatusCode.NotFound, typeof(int), Description = "Address you want to Delete does not excist")]
        public async Task<IActionResult> Delete(int id)
        {
            Address address = await database.Addresses.FindAsync(id);
            if (address != null)
            {
                database.Remove<Address>(address);
                await database.SaveChangesAsync();
                return NoContent();
            }
            else
                return NotFound(id);
        }      
    }
}
