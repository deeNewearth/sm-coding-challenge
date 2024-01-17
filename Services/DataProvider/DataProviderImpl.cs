using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using sm_coding_challenge.Models;

namespace sm_coding_challenge.Services.DataProvider
{
    public class DataProviderImpl : IDataProvider
    {
        readonly HttpClient _client;
        readonly IDistributedCache _cache;
        readonly ILogger _logger;


        static readonly string _DataCacheName = "PlayerMapStr";

        // dee: injecting HttpClient instead of creating it each time
        public DataProviderImpl(
            HttpClient client,
            IDistributedCache cache,
            ILogger<DataProviderImpl> logger
            )
        {
            _client = client;
            _cache = cache;
            _logger = logger;
        }

        static readonly JsonSerializerSettings _mySerializationSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        /// <summary>
        /// Used to fetch data when cache expires
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        async Task<string> FetchData()
        {


            var mapStr = await _cache.GetStringAsync(_DataCacheName);

            if (!string.IsNullOrWhiteSpace(mapStr))
            {
                _logger.LogDebug("dataInCache");
                return mapStr;
            }


            _logger.LogDebug("dataNotInCache");

            using var response = await _client.GetAsync("https://gist.githubusercontent.com/RichardD012/a81e0d1730555bc0d8856d1be980c803/raw/3fe73fafadf7e5b699f056e55396282ff45a124b/basic.json");

            // dee : Making sure we have a success code
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("failedToFetchData {0}", response.StatusCode);
                throw new Exception("failedToFetchData");
            }

            var stringData = response.Content.ReadAsStringAsync().Result;

            var dataResponse = JsonConvert.DeserializeObject<DataResponseModel>(stringData, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });

            var playerMap = CreatePlayerMap(dataResponse);

            mapStr = JsonConvert.SerializeObject(playerMap, _mySerializationSettings);

            await _cache.SetStringAsync(_DataCacheName, mapStr, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(6)
            });

            _logger.LogDebug("dataSavedInCache");


            return mapStr;

        }


        public async Task<IDictionary<string, PlayerAndPosition[]>> GetPlayerMap()
        {
            var mapStr = await FetchData();

            return JsonConvert.DeserializeObject<Dictionary<string, PlayerAndPosition[]>>(mapStr, _mySerializationSettings);
        }


        Dictionary<string, PlayerAndPosition[]> CreatePlayerMap(DataResponseModel dataResponse)
        {
            var allPlayers =
                dataResponse.Kicking.Select(player => new PlayerAndPosition { Position = PlayPosition.kicking, Player = player })
                .Concat(dataResponse.Passing.Select(player => new PlayerAndPosition { Position = PlayPosition.passing, Player = player }))
                .Concat(dataResponse.Receiving.Select(player => new PlayerAndPosition { Position = PlayPosition.receiving, Player = player }))
                .Concat(dataResponse.Rushing.Select(player => new PlayerAndPosition { Position = PlayPosition.rushing, Player = player }))
                .ToArray();


            return (from p in allPlayers
                              group p by p.Player.Id into g
                              select new { id = g.Key, playersAndPosition = g.ToArray() })
                     .ToDictionary(k => k.id, v => v.playersAndPosition)

                     ;

        }



        public async Task<PlayerModel> GetPlayerById(string id)
        {
            var playersMap = await GetPlayerMap();

            if (playersMap.TryGetValue(id,out var playerAndPos))
            {
                return playerAndPos.First().Player;
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}
