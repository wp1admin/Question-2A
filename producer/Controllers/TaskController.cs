using Microsoft.AspNetCore.Mvc;
using System;
using producer.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using RabbitMQ.Client;

namespace producer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TaskController : ControllerBase
    {
      /*  public IActionResult Index()
        {
            return View();
        }*/
      public TaskController()
        {

        }

        [HttpPost]
        public ActionResult <Token> PostTask(Models.Task task)
        {
            // check if token exits

            Models.Token token = null;

            try
            {
                //1. post task to get token


                string url = "https://reqres.in/api/login";

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.BaseAddress = new Uri(url);

                    try
                    {
                        string json1 = JsonConvert.SerializeObject(task);
                        var data1 = new StringContent(json1, Encoding.UTF8, "application/json");

                        var response = client.PostAsync(url, data1);
                        response.Wait();

                        var result = response.Result;
                        if (result.IsSuccessStatusCode)
                        {
                            var returnResult = result.Content.ReadAsStringAsync();
                            returnResult.Wait();

                            token = JsonConvert.DeserializeObject<Token>(returnResult.Result);
                        }
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                }

                //if token valid -200 or not valid 401

                if (token == null) // not valid
                    return StatusCode(401, "Unauthorized error"); 
                
                var factory = new ConnectionFactory()
                {
                        //HostName = "localhost" , 
                        //Port = 30724
                        HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
                        Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))
                };
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: "TaskQueue",
                                         durable: false,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                //    string message = greeting.Greet;
                    var body = Encoding.UTF8.GetBytes(token.token);

                    channel.BasicPublish(exchange: "",
                                         routingKey: "TaskQueue",
                                         basicProperties: null,
                                         body: body);
                }



            }
            catch (Exception e)
            {
                throw;
            }


            return token;
        }

        
    }
}
