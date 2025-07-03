using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TryMeBitch.Data;
using TryMeBitch.Models;
using static TryMeBitch.Models.Blockchain;


namespace TryMeBitch.Controllers
{
    public class ReadBlockchain : Controller
    {

        private readonly MRTDbContext _repo;
        public ReadBlockchain(MRTDbContext repo) { _repo = repo; }
       


        public async Task<IActionResult> Index()
        {
            var GetAllBlockchain = _repo.Blockchain.OrderByDescending(t => t.Timestamp).ToList();
            return View(GetAllBlockchain);
        }

        [HttpPost]
        [Route("Read/checkBlockchain")]
        public async Task<IActionResult> checkBlockchain()
        { 

            var issues = await CheckBlockchainIntegrityAndReport(_repo.Blockchain.OrderByDescending(t => t.Timestamp).ToList(), _repo.cards.ToList());
            return Ok(issues);
        }

        [HttpPost]
        [Route("Read/FixTamper")]
        public async Task<IActionResult> FixTamper(Guid cardId, Guid Id)
        {

            var affectedEvents = _repo.Blockchain.Where(e => e.CardId == cardId).OrderByDescending(t => t.Timestamp).ToList();
            await new Blockchain(_repo).RebuildMerkleBranch(affectedEvents);
            Console.WriteLine("Tamper Fixed");

            return Json(new { success = true, card=cardId, events=Id });      
        }


        [HttpPost]
        [Route("Read/FixAll")]
        public async Task<IActionResult> FixAll()
        {

            foreach (var card in _repo.cards.ToList()) {
                var affectedEvents = _repo.Blockchain.Where(e => e.CardId == card.Id).OrderByDescending(t => t.Timestamp).ToList();
                new Blockchain(_repo).RebuildMerkleBranch(affectedEvents);
                Console.WriteLine("Tamper Fixed");
            }

            return Json(new { success = true, message = "Fixed" });
        }


        [HttpPost]
        [Route("Read/Tamper")]
        public IActionResult Tamper()
        {
            new Blockchain(_repo).TamperBlockchain(_repo.Blockchain.ToList());
            return Json(new { success = true, message = "Tampered" });
        }
    }
}
