using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.UI;
using System.Web.Script.Serialization;


namespace ScillUtilities
{
    public class ScillScout
    {
        public static ScillScout _instance;

        public ScillScout()
        {
            Console.WriteLine("TESTIS TESTIS");
        }

        public static ScillScout GetInstance()
        {
            Console.WriteLine("singleton 1");
            if (_instance == null)
            {
                Console.WriteLine("singleton 2");
                _instance = new ScillScout();
            }
            return _instance;
        }

        public static string API_KEY = "";
        //private int GAME_ID = 0;
        public const string API_URL = "https://ep.scillgame.com/v2";
        //public const string API_URL = "http://localhost:2001/v2";
        public const int HTTP_SUCCESS = 200;
        public const int HTTP_BAD_REQ = 400;
        public const int HTTP_UNAUTHORIZED = 401;
        public const int HTTP_FORBIDDEN = 403;
        static public int[] NO_AUTH_STATUSES = { HTTP_UNAUTHORIZED, HTTP_FORBIDDEN };
        public const string WRONG_API_KEY = "EMPTY_OR_INVALID_API_KEY";
        public static readonly HttpClient client = new HttpClient();
        public Task[] taskArray = { };
        public static Dictionary<string, string> initResponse = new Dictionary<string, string>();

        public async Task<Dictionary<string, string>> init(string APIkey)
        {
            Console.WriteLine("xyz22");
            Console.WriteLine("initResponse.Count = ");
            Console.Write(initResponse.Count);
            
            initResponse = await GetRequest(API_URL + "/scout-supported-games/validate/" + APIkey, new Dictionary<string, string>());
            Console.Write("get init resp, validate key");
            Console.Write(initResponse);
            if (initResponse["status"] == HTTP_SUCCESS.ToString())
            {
                Console.WriteLine("xyz3");
                API_KEY = APIkey;
            }
            
            return initResponse;
        }

        public void Init(string APIkey)
        {
            Console.WriteLine("xyz1");

            var thread = new Thread(async () => {
                //do your work here
                Console.WriteLine("Init with key" + APIkey);
                var resp = await init(APIkey);
                Console.WriteLine("Resp = " + resp["response"]);
            });

            thread.Start();
        }

        public async static Task<Dictionary<string, string>> GetRequest(string url, Dictionary<string, string> payload)
        {
            Dictionary<string, string> returnResponse = new Dictionary<string, string>() { };


            var http = new HttpClient();
            var content = string.Join("&", payload.Select(x => x.Key + "=" + x.Value).ToArray());
            var response = await http.GetAsync(url + "?" + content);
            var result = await response.Content.ReadAsStringAsync();

            returnResponse["status"] = ((int)response.StatusCode).ToString();
            returnResponse["response"] = result;

            return returnResponse;
        }

        public async static Task<Dictionary<string, string>> PostRequest(string url, Dictionary<string, object> payload)
        {
            Dictionary<string, string> returnResponse = new Dictionary<string, string>() { };

            var http = new HttpClient();
            var json = new JavaScriptSerializer().Serialize(payload.ToDictionary(item => item.Key, item => item.Value.ToString()));

            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await http.PostAsync(url, stringContent);
            var result = await response.Content.ReadAsStringAsync();


            returnResponse["status"] = ((int)response.StatusCode).ToString();
            returnResponse["response"] = result;

            return returnResponse;
        }

        public async static Task<Dictionary<string, string>> getGameEvents()
        {
            var response = new Dictionary<string, string>();


            if (ScillScout.API_KEY == "")
            {
                response["status"] = HTTP_BAD_REQ.ToString();
                response["response"] = WRONG_API_KEY;
                return response;
            }

            var payload = new Dictionary<string, string>() { };

            var eventResponse = await GetRequest(API_URL + "/available-events/" + API_KEY, payload);
            var responseCode = Convert.ToInt32(eventResponse["status"]);

            if (ScillScout.NO_AUTH_STATUSES.Contains(responseCode))
            {
                eventResponse["response"] = WRONG_API_KEY;
            }

            return eventResponse;
        }

        public static string GetSDKEventParserURL()
        {
            Console.WriteLine("get URL =====> " + API_URL + "/scout-supported-games/parse/" + API_KEY + "<====");
            return API_URL + "/scout-supported-games/parse/" + API_KEY;
        }

        public async static Task<Dictionary<string, string>> sendGameEvent(Dictionary<string, string> metaData, string eventText, Dictionary<string, Object> payloadMeta)
        {
            var json = new JavaScriptSerializer().Serialize(payloadMeta.ToDictionary(item => item.Key, item => item.Value.ToString()));
            var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var payload = new Dictionary<string, object>() {
                { "event_type", metaData["type"] },
                { "message", eventText },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["match_uuid"]},
                { "round_id", metaData["round_id"]},
                { "meta_data", stringContent }
            };

            var eventResponse = await PostRequest(GetSDKEventParserURL(), payload);
            var responseCode = Convert.ToInt32(eventResponse["status"]);

            if (NO_AUTH_STATUSES.Contains(responseCode))
            {
                eventResponse["message"] = WRONG_API_KEY;
            }

            return eventResponse;
        }

        public static void GetGameEvents()
        {
            var thread = new Thread(async () => {
                //do your work here
                await getGameEvents();
            });

            thread.Start();
        }

        public Task<Dictionary<string, string>> MapLoading(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "mapLoading"},
                { "mapName", data["mapName"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> MatchStarted(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "matchStarted"},
                { "mapName", data["mapName"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]}

            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerPurchasedEvent(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "gameStarted"},
                { "user", data["user"] },
                { "weapon_name", data["weaponName"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}

            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> RoundEndEvent(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "roundEnd"},
                { "winner_team", data["winnerTeam"] },
                { "win_message", data["winMessage"] },
                { "team_one", data["teamOne"] },
                { "team_one_score", data["teamOneScore"] },
                { "team_two", data["teamTwo"] },
                { "team_two_score", data["teamTwoScore"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> MatchEndEvent(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object> {
                { "type", "matchEnd"},
                { "game_mode", data["gameMode"] },
                { "game_mode_log", data["gameModeLog"] },
                { "map_name", data["mapName"] },
                { "counter_terrorist_score", data["counterTerroristScore"] },
                { "terrorist_score", data["terroristScore"] },
                { "game_duration", data["gameDuration"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> TeamSwitchedEvent(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "teamSwitched"},
                { "player_name", data["playerName"] },
                { "user_steam_id", data["userSteamId"] },
                { "old_team", data["oldTeam"] },
                { "new_team", data["newTeam"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> FunFacts(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "funFacts"},
                { "username", data["userName"] },
                { "user_one_steam_id", data["userOneSteamId"] },
                { "value", data["value"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerDisconnected(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "playerDisconnected"},
                { "username", data["userName"] },
                { "user_steam_id", data["userSteamId"] },
                { "user_team", data["userTeam"]},
                { "disconnect_reason", data["disconnectReason"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]}
            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }


        public Task<Dictionary<string, string>> PlayerTriggeredCSCGO(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "playerTriggered"},
                { "username", data["userName"] },
                { "user_steam_id", data["userSteamId"] },
                { "user_team", data["userTeam"]},
                { "event", data["event"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }


        public Task<Dictionary<string, string>> MatchStart(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "matchStarted"},
                { "map_name", data["mapName"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerStartedGame(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "gameStarted"},
                { "user", data["user"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> LapPosition(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "lapPostion"},
                { "user", data["user"] },
                { "lap", data["lap"] },
                { "total_laps", data["total_laps"] },
                { "position", data["position"] },
                { "total_positions", data["total_positions"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> RacePosition(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "racePosition"},
                { "user", data["user"] },
                { "position", data["position"] },
                { "total_positions", data["total_positions"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> LapTime(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "lapTime"},
                { "user", data["user"] },
                { "current_lap", data["current_lap"] },
                { "total_laps", data["total_laps"] },
                { "lap_time", data["lap_time"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> RaceTime(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "raceTime"},
                { "user", data["user"] },
                { "total_laps", data["total_laps"] },
                { "race_time", data["race_time"] },
                { "position", data["position"] },
                { "total_positions", data["total_positions"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> Damage(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "type", "gameStarted"},
                { "user", data["user"] },
                { "current_lap", data["current_lap"] },
                { "laps", data["laps"] },
                { "position", data["position"] },
                { "number_of_players", data["number_of_players"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerExitedTheGame(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "gameStopped"},
                { "user", data["user"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> MatchCreated(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "matchCreated"},
                { "map", data["map"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerJoined(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "playerEntered"},
                { "user", data["user"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }


        public Task<Dictionary<string, string>> PlayerCommittedSuicide(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {

                { "type", "playerDisconnected"},
                { "user", data["user"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]},
                { "weapon", data["weapon"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerKilled(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "kill"},
                { "user", data["user"] },
                { "killed_user", data["killedUser"] },
                { "is_headshoot", data["isHeadshoot"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]},
                { "weapon", data["weapon"]}

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerAttacked(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "attack"},
                { "user", data["user"] },
                { "injured_user", data["injured_user"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]},
                { "weapon", data["weapon"]},
                { "damage", data["damage"] }

            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerSelectedTeam(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "teamSelected"},
                { "user", data["user"] },
                { "nextTeam", metaData["nextTeam"] },
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]},
                { "match_uuid", metaData["matchUUID"]},
                { "round_id", metaData["roundID"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PlayerChangedName(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {

            var payload = new Dictionary<string, Object>() {
                { "type", "nameChanged"},
                { "user", data["user"] },
                { "name", metaData["newName"]},
                { "server", metaData["server"]},
                { "server_name", metaData["server_name"]},
                { "game", metaData["game"]},
                { "game_name", metaData["game_name"]}
            };

            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> EnemyShot(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var json = new JavaScriptSerializer().Serialize(data.ToDictionary(item => item.Key, item => item.Value.ToString()));
            //var stringContent = new StringContent(json, Encoding.UTF8, "application/json");

            var payload = new Dictionary<string, Object>() {
                { "event_type", "enemyShotEvent" },
                { "message", metaData.ContainsKey("message") ? metaData["message"] : "" },
                { "server", metaData.ContainsKey("server") ? metaData["server"] : "" },
                { "server_name", metaData.ContainsKey("server_name") ? metaData["server_name"] : "" },
                { "game", metaData.ContainsKey("game") ? metaData["game"] : "" },
                { "game_mode", metaData.ContainsKey("game_mode") ? metaData["game_mode"] : "" },
                { "match_uuid", metaData.ContainsKey("match_uuid") ? metaData["match_uuid"] : "" },
                { "round_id", metaData.ContainsKey("round_id") ? metaData["round_id"] : "" },
                { "meta_data", json }
            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> HealthPackCollected(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "event_type", "healthPackCollectedEvent"},
                { "message", metaData.ContainsKey("message") ? metaData["message"] : "" },
                { "server", metaData.ContainsKey("server") ? metaData["server"] : "" },
                { "server_name", metaData.ContainsKey("server_name") ? metaData["server_name"] : "" },
                { "game", metaData.ContainsKey("game") ? metaData["game"] : "" },
                { "game_mode", metaData.ContainsKey("game_mode") ? metaData["game_mode"] : "" },
                { "match_uuid", metaData.ContainsKey("match_uuid") ? metaData["match_uuid"] : "" },
                { "round_id", metaData.ContainsKey("round_id") ? metaData["round_id"] : "" },
                { "meta_data", data }
            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> PasswordGained(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "event_type", "passwordGainedEvent"},
                { "message", metaData.ContainsKey("message") ? metaData["message"] : "" },
                { "server", metaData.ContainsKey("server") ? metaData["server"] : "" },
                { "server_name", metaData.ContainsKey("server_name") ? metaData["server_name"] : "" },
                { "game", metaData.ContainsKey("game") ? metaData["game"] : "" },
                { "game_mode", metaData.ContainsKey("game_mode") ? metaData["game_mode"] : "" },
                { "match_uuid", metaData.ContainsKey("match_uuid") ? metaData["match_uuid"] : "" },
                { "round_id", metaData.ContainsKey("round_id") ? metaData["round_id"] : "" },
                { "meta_data", data }
            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public Task<Dictionary<string, string>> InformantSecretGained(Dictionary<string, string> metaData, Dictionary<string, string> data)
        {
            var payload = new Dictionary<string, Object>() {
                { "event_type", "informantSecretGainedEvent"},
                { "message", metaData.ContainsKey("message") ? metaData["message"] : "" },
                { "server", metaData.ContainsKey("server") ? metaData["server"] : "" },
                { "server_name", metaData.ContainsKey("server_name") ? metaData["server_name"] : "" },
                { "game", metaData.ContainsKey("game") ? metaData["game"] : "" },
                { "game_mode", metaData.ContainsKey("game_mode") ? metaData["game_mode"] : "" },
                { "match_uuid", metaData.ContainsKey("match_uuid") ? metaData["match_uuid"] : "" },
                { "round_id", metaData.ContainsKey("round_id") ? metaData["round_id"] : "" },
                { "meta_data", data }
            };
            var eventResponse = PostRequest(GetSDKEventParserURL(), payload);
            return eventResponse;
        }

        public static void SendGameEvent(Dictionary<string, string> metaData, string eventText, Dictionary<string, Object> payloadMeta)
        {
            var thread = new Thread(async () => {
                await sendGameEvent(metaData, eventText, payloadMeta);
            });

            thread.Start();
        }
        
    }
}
