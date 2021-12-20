using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataLayer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NSwag.Annotations;
using System.Net;
using Microsoft.Extensions.Configuration;
using Azure.Storage.Blobs;
using System.IO;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.StaticFiles;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

namespace RESTfulWebAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Teacher")]
    [Route("[controller]")]
    [ApiController]
    [OpenApiTag("Student", Description = "Methods to work with Students")]
    public class StudentsController : ControllerBase
    {
        private readonly WebAPIModel _context;
        private readonly IConfiguration _configuration;
        public StudentsController(WebAPIModel context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]       
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents()
        {
            return await _context.Students
                .Include(x => x.Address)
                .Select(x => new Student
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    Age = x.Age,
                    StudentNumber = x.StudentNumber,
                    CreatedBy = x.CreatedBy,
                    Avatar = x.Avatar,
                    AddressId = x.AddressId,
                    Address = x.Address
                })
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStudent(int id)
        {
            var student = await _context.Students
                .Include(x => x.Address)
                .Select(x => new
                {
                    x.Id,
                    x.FirstName,
                    x.LastName,
                    x.Age,
                    x.StudentNumber,
                    x.CreatedBy,
                    x.Avatar,
                    Address = new
                    {
                        x.Address.Id,
                        x.Address.Street,
                        x.Address.City,
                        x.Address.Country,
                        x.Address.AdditionalInfo.StreetNumber,
                        x.Address.AdditionalInfo.AdditionalNumber,
                        x.Address.AdditionalInfo.Zip
                    }
                })
                .Where(x => x.Id == id)
                .SingleOrDefaultAsync();

            if (student == null)
            {
                return NotFound();
            }

            return Ok(student);
        }

        [HttpPut("{id}")]
        [SwaggerResponse(HttpStatusCode.BadRequest, null, Description = "Id from Body and url does not match")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "Student you want to modify does not excist")]
        [SwaggerResponse(HttpStatusCode.NoContent, null, Description = "Student Successfully Updated")]
        public async Task<IActionResult> PutStudent(int id, Student student)
        {
            if (id != student.Id)
            {
                return BadRequest();
            }

            _context.Entry(student).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!StudentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        [SwaggerResponse(HttpStatusCode.Created, typeof(Student), Description = "Successfully Created Student")]
        public async Task<ActionResult<Student>> PostStudent(Student student)
        {
            var claims = User.Claims.ToList();

            //var id = User.Claims.First().Value;

            var id = User.Claims.Where(x => x.Type.Contains("claims/nameidentifier")).FirstOrDefault().Value;
            var name = User.Claims.Where(x => x.Type.Contains("claims/name") && !x.Type.Contains("identifier")).FirstOrDefault().Value;
            var role = User.Claims.Where(x => x.Type.Contains("claims/role")).FirstOrDefault().Value;

            student.CreatedBy = id; // Logged In UserId
            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetStudent", new { id = student.Id }, student);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }       
        [HttpPost("{id}")]
        [SwaggerResponse(HttpStatusCode.OK,null, Description = "Successfully uploaded Image")]
        public async Task<IActionResult> UploadImage(int id, [FromForm] IFormFile file)
        {
            // Use Key Vault to store app.settings keys
            SecretClient secretClient = new SecretClient(new Uri("https://kostic.vault.azure.net/"), new DefaultAzureCredential());
            KeyVaultSecret storageAccountSecret = secretClient.GetSecret("StorageAccount");
            string connectionString = storageAccountSecret.Value;
            
            BlobServiceClient blobServiceClient = new BlobServiceClient(_configuration.GetConnectionString("StorageAccount"));
            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(_configuration["BlobContainer"]);
            BlobClient blobClient = blobContainerClient.GetBlobClient(file.FileName);

            //// With MemoryStream
            // using (MemoryStream memory = new MemoryStream())
            // {
            //     await file.CopyToAsync(memory);
            //     memory.Position = 0;
            //     var result = await blobClient.UploadAsync(memory, true);
            // }    

            Stream image = file.OpenReadStream();            
            await blobClient.UploadAsync(image, new BlobHttpHeaders { ContentType = GetContentType(file.FileName) });
            string imagePath = blobClient.Uri.AbsoluteUri;

            var student = await _context.Students.FindAsync(id);
            student.Avatar = imagePath;
            _context.Entry(student).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok(string.Format("Image Uploaded To {0} \nUrl: {1}",blobContainerClient.AccountName, imagePath));
        }
        [AllowAnonymous]
        [HttpGet("downloadImage/{id}")]
        [SwaggerResponse(HttpStatusCode.OK, typeof(FileStreamResult), Description = "Successfully downloaded Image")]
        [SwaggerResponse(HttpStatusCode.NotFound, null, Description = "There is no Student with that Id")]
        public async Task<IActionResult> DownloadImage(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                string fileName = Path.GetFileName(student.Avatar);

                BlobServiceClient blobServiceClient = new BlobServiceClient(_configuration.GetConnectionString("StorageAccount"));
                BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(_configuration["BlobContainer"]);
                BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);

                MemoryStream memoryStream = new MemoryStream();
                await blobClient.DownloadToAsync(memoryStream);
                memoryStream.Position = 0;               

                return new FileStreamResult(memoryStream, GetContentType(fileName)) { FileDownloadName = fileName };
            }
            return NotFound(id);
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
        private static string GetContentType(string file)
        {
            var provider = new FileExtensionContentTypeProvider();
           
            if (!provider.TryGetContentType(file, out string contentType))
                contentType = "application/octet-stream";

            return contentType;
        }
    }
}
