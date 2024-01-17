using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using sm_coding_challenge.Models;
using sm_coding_challenge.Services.DataProvider;

namespace sm_coding_challenge.Controllers
{
    public class HomeController : Controller
    {

        private IDataProvider _dataProvider;
        public HomeController(IDataProvider dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Player(string id)
        {
            return Json(await _dataProvider.GetPlayerById(id));
        }

        [HttpGet]
        public async Task<IActionResult> Players(string id)
        {
            // dee : We should remove duplicated Ids fetched
            var idList = id.Split(',').Distinct();

            var playersMap = await _dataProvider.GetPlayerMap();

            // dee : Optimized list using linq
            var returnList = idList.Select(anId =>
                playersMap.ContainsKey(anId) ? playersMap[anId].First().Player : null)
                .Where(r => null != r)
                .ToArray();

            return Json(returnList);
        }

        [HttpGet]
        public async Task<IActionResult> LatestPlayers(string id)
        {
            var playersMap = await _dataProvider.GetPlayerMap();

            if (playersMap.TryGetValue(id, out var playerAndPos))
            {
                var ret = (from p in playerAndPos
                          group p by p.Position into g
                          select new { position = g.Key, players = g.ToArray() })
                          .ToDictionary(k=>k.position, v=>v.players)
                          ;

                return Json(ret);
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
