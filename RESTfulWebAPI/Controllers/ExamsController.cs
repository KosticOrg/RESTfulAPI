using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataLayer.Models;
using Newtonsoft.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Configuration;
using RESTfulWebAPI.Models;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using NSwag.Annotations;

namespace RESTfulWebAPI.Controllers
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Teacher")]
    [Route("[controller]")]
    [ApiController]
    [OpenApiTag("Exams", Description = "Methods to work with Exams")]
    public class ExamsController : ControllerBase
    {
        private readonly WebAPIModel _context;
        private readonly IConfiguration _configuration;

        public ExamsController(WebAPIModel context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: URL/Exams
        [HttpGet]
        public ActionResult<List<Exam>> GetExams()
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(_configuration.GetConnectionString("StorageAccount"));
            CloudTableClient tableClient = account.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("exams");

            TableQuery<Exam> query = new TableQuery<Exam>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "studentId"));
            List<Exam> tableItems = table.ExecuteQuery<Exam>(query).ToList();

            return Ok(tableItems);
        }

        // GET:  URL/Exams/5
        [HttpGet("{id}")]
        public ActionResult<Exam> GetExam(int id)
        {
            CloudStorageAccount account = CloudStorageAccount.Parse(_configuration.GetConnectionString("StorageAccount"));
            CloudTableClient tableClient = account.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("exams");

            TableQuery<Exam> query = new TableQuery<Exam>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "studentId"));                                                        

            List<Exam> tableItems = table.ExecuteQuery<Exam>(query).Where(x => x.StudentId == id).ToList();

            return Ok(tableItems);
        }

        // POST:  URL/Exams/postExams
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("postExams")]
        public async Task<IActionResult> PostExams(List<Exam> exams)
        {
            QueueClient examQueue = new QueueClient(_configuration.GetConnectionString("StorageAccount"), "exams");

            if (examQueue.Exists())
            {
                foreach (Exam exam in exams)
                {
                    string msgBody = JsonConvert.SerializeObject(exam);
                    await examQueue.SendMessageAsync(msgBody);
                }
                return Ok("Queue with Exams list created successfully !!!");
            }

            return NotFound("Queue not Fount !!!");
        }
        // POST:  URL/Exams/postExamsTrigger
        [HttpPost("postExamsTrigger")]
        public async Task<IActionResult> PostExamsTrigger(List<Exam> exams)
        {
            QueueClient examQueue = new QueueClient(_configuration.GetConnectionString("StorageAccount"), "exams");

            if (examQueue.Exists())
            {
                foreach (Exam exam in exams)
                {
                    string msgBody = JsonConvert.SerializeObject(exam);
                    await examQueue.SendMessageAsync(Base64Encode(msgBody));
                }
                return Ok("Queue with Exams list created successfully !!!");
            }

            return NotFound("Queue not Fount !!!");
        }
        // GET:  URL/Exams/readQueue
        [HttpGet("processExams")]
        public async Task<IActionResult> ProcessExams()
        { 
            QueueClient examQueue = new QueueClient(_configuration.GetConnectionString("StorageAccount"), "exams");
            CloudStorageAccount account = CloudStorageAccount.Parse(_configuration.GetConnectionString("StorageAccount"));
            CloudTableClient tableClient = account.CreateCloudTableClient();
            CloudTable table = tableClient.GetTableReference("exams");

            if (examQueue.Exists())
            {
                var message = await examQueue.PeekMessageAsync();
                while (message.Value != null && message.Value.DequeueCount == 0)
                {
                    var result = await examQueue.ReceiveMessageAsync();
                    Exam exam = JsonConvert.DeserializeObject<Exam>(result.Value.Body.ToString());
                    Course course = await _context.Courses.FindAsync(exam.CourseId);

                    if (exam.Points >= course.Points)
                        exam.Passed = true;

                    exam.PartitionKey = "studentId";
                    exam.RowKey = Guid.NewGuid().ToString();
                    exam.Timestamp = DateTime.Now;

                    TableOperation insert = TableOperation.InsertOrMerge(exam);

                    await table.ExecuteAsync(insert);

                    message = await examQueue.PeekMessageAsync();
                }

                return CreatedAtAction("GetExams",null);
            }

            return NotFound("Queue not Fount !!!");
        }
        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}
