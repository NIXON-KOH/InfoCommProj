using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TryMeBitch.Models;

namespace TryMeBitch.Controllers
{

    [Authorize(Roles = "Station,Administrator")]
    public class BlockchainController : Controller
    {
        private readonly MRTDbContext _repo;
        private readonly Blockchain _blockchain;
        public BlockchainController(MRTDbContext repo, Blockchain blockchain)
        {
            _repo = repo;
            _blockchain = blockchain;
        }

        public ActionResult Index()
        {
            var events = _repo.Blockchain.OrderBy(e => e.Timestamp).ToList();
            return View(events);
        }

       
        [HttpGet("validate")]
        [Route("/blockchain/validate")]
        public ActionResult<List<Blockchain.ValidationIssue>> Validate()
        {
            var issues = _blockchain.Validate();
            return Ok(issues);
        }
       
        [HttpPost("fixall")]
        [Route("/blockchain/fixall")]
        public IActionResult FixAll()
        {
            _blockchain.FixAll();
            return Ok();
        }

        [HttpPost("fix/card/{cardId:guid}")]
        [Route("/blockchain/fix/card/{CardId}")]
        public IActionResult FixByCard(Guid cardId)
        {
            _blockchain.FixByCard(cardId);
            return Ok();
        }

       
        [HttpPost("/blockchain/fix/event/{eventId:guid}")]
        [Route("/blockchain/fix/event/{EventId}")]
        public IActionResult FixByEvent(Guid eventId)
        {
            _blockchain.FixEvent(eventId);
            return Ok();
        }

        // POST api/blockchain/tamper/event/{eventId}
        [HttpPost("tamper")]
        [Route("/blockchain/tamper")]
        public IActionResult TamperEvent()
        {
            _blockchain.TamperRandomEvent();
            return Ok();
        }

    }
}
