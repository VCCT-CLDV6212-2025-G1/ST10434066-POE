using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ABCRetailStorageApp.Controllers
{
    public class QueuesController : Controller
    {
        private readonly string _conn;
        private readonly string _queue;

        public QueuesController(IOptions<AzureStorageOptions> opt)
        {
            _conn = opt.Value.ConnectionString;
            _queue = opt.Value.QueueName;
        }

        private QueueClient GetQueue()
        {
            var q = new QueueClient(_conn, _queue);
            q.CreateIfNotExists();
            return q;
        }

        public async Task<IActionResult> Index()
        {
            var q = GetQueue();
            var peek = await q.PeekMessagesAsync(32); 
            var msgs = peek.Value.Select(m => m.MessageText).ToList();
            return View(msgs);
        }

        [HttpPost]
        public async Task<IActionResult> Enqueue(string text)
        {
            var q = GetQueue();
            await q.SendMessageAsync(string.IsNullOrWhiteSpace(text) ? "Default test message" : text);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Seed()
        {
            var q = GetQueue();
            var samples = new[]
            {
                "Processing order #1001",
                "Inventory updated",
                "Uploaded image productA.jpg",
                "Order #1002 packed",
                "Order #1003 shipped"
            };
            foreach (var s in samples)
            {
                await q.SendMessageAsync(s);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}