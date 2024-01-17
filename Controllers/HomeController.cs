using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using sm_coding_challenge.Models;
using sm_coding_challenge.Services.DataProvider;

namespace sm_coding_challenge.Controllers
{
    public class HomeController : Controller
    {

        readonly IDataProvider _dataProvider;
        readonly ILogger _logger;


        public HomeController(
            IDataProvider dataProvider,
            ILogger<HomeController> logger
            )
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        ObjectResult fromError(Exception ex)
        {

            var errorId = Guid.NewGuid().ToString();

            _logger.LogError(ex, $"error id {errorId}" );

            if (ex is FileNotFoundException)
            {
                return StatusCode((int)HttpStatusCode.NotFound,new { error = "not found", errorId});
            }

            return StatusCode((int)HttpStatusCode.InternalServerError, new { error = "generic error", errorId });
        }

        [HttpGet]
        public async Task<IActionResult> Player(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                    throw new ArgumentNullException(nameof(id));


                return Json(await _dataProvider.GetPlayerById(id));
            }
            catch(Exception ex)
            {
                
                return fromError(ex);
            }
            
        }

        [HttpGet]
        public async Task<IActionResult> Players(string ids)
        {
            try
            {
                if (string.IsNullOrEmpty(ids))
                return Json(Array.Empty<string>());

            // dee : We should remove duplicated Ids fetched
            var idList = ids.Split(',').Distinct();

            var playersMap = await _dataProvider.GetPlayerMap();

            // dee : Optimized list using linq
            var returnList = idList.Select(anId =>
                playersMap.ContainsKey(anId) ? playersMap[anId].First().Player : null)
                .Where(r => null != r)
                .ToArray();

            return Json(returnList);
            }
            catch (Exception ex)
            {
                
                return fromError(ex);
            }
        }

        [HttpGet]
        public async Task<IActionResult> LatestPlayers(string ids)
        {
            try
            {
                if (string.IsNullOrEmpty(ids))
                    return Json(Array.Empty<string>());

                var playersMap = await _dataProvider.GetPlayerMap();

                var idList = ids.Split(',').Distinct();

                var returnList = idList.Select(anId =>
                    playersMap.ContainsKey(anId) ? playersMap[anId] : null)
                    .Where(r => null != r)
                    .ToArray();

                var ret = (from p in returnList.SelectMany(p => p)
                           group p by p.Position into g
                           select new { position = g.Key, players = g.ToArray() })
                          .ToDictionary(k => k.position, v => v.players.Select(p => p.Player))
                          ;

                return Json(ret);
            }
            catch (Exception ex)
            {
                
                return fromError(ex);
            }

        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
