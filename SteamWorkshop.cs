using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Newtonsoft.Json.Linq;
using Serilog;

namespace RimworldModUpdater
{
    public static class SteamWorkshop
    {
        public static JObject lastResult;

        private static Encoding GetResponseEncoding(HttpContent content, Encoding fallbackEncoding)
        {
            if (content.Headers.ContentType == null || content.Headers.ContentType.CharSet == null)
                return fallbackEncoding;

            try
            {
                return Encoding.GetEncoding(content.Headers.ContentType.CharSet);
            }
            catch (ArgumentException)
            {
                return fallbackEncoding;
            }
        }

        public static async Task<JObject> GetWorkshopFileDetailsJSON(string[] fileIds, bool collection = false)
        {
            string url = $"http://api.steampowered.com/ISteamRemoteStorage/{(collection ? "GetCollectionDetails" : "GetPublishedFileDetails")}/v0001/";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RimworldModUpdater/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            var values = new Dictionary<string, string>();

            values.Add(collection ? "collectioncount" : "itemcount", fileIds.Length.ToString());

            var ids = new List<string>();
            for (int i = 0; i < fileIds.Length; i++)
            {
                string id = fileIds[i];
                if (ids.Contains(id)) continue; // Don't allow requesting duplicates.

                values.Add($"publishedfileids[{i}]", id);
                ids.Add(id);
            }
            values.Add("format", "json");

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(url, content);
            var data = await response.Content.ReadAsByteArrayAsync();

            var encoding = GetResponseEncoding(response.Content, Encoding.UTF8);

            return JObject.Parse(encoding.GetString(data));
        }

        public static async Task<JObject> GetWorkshopFileDetailsJSON(BaseMod[] mods, bool collection = false)
        {
            string url = $"http://api.steampowered.com/ISteamRemoteStorage/{(collection ? "GetCollectionDetails" : "GetPublishedFileDetails")}/v0001/";

            var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("RimworldModUpdater/1.0");
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            var values = new Dictionary<string, string>();

            values.Add(collection ? "collectioncount" : "itemcount", mods.Length.ToString());

            var ids = new List<string>();
            for (int i = 0; i < mods.Length; i++)
            {
                string id = mods[i].ModId;
                if (ids.Contains(id)) continue; // Don't allow requesting duplicates.

                values.Add($"publishedfileids[{i}]", id);
                ids.Add(id);
            }
            values.Add("format", "json");

            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync(url, content);
            var data = await response.Content.ReadAsByteArrayAsync();

            var encoding = GetResponseEncoding(response.Content, Encoding.UTF8);

            return JObject.Parse(encoding.GetString(data));
        }

        public static async Task<bool> IsWorkshopCollection(string fileId)
        {
            JObject obj = await GetWorkshopFileDetailsJSON(new []{fileId}, true);

            lastResult = obj;

            return obj["response"]["resultcount"].ToObject<int>() == 1;
        }

        public static async Task<List<WorkshopFileDetails>> GetWorkshopFileDetailsFromCollection(string collectionId)
        {
            List<WorkshopFileDetails> list = new List<WorkshopFileDetails>();
            JObject obj = await GetWorkshopFileDetailsJSON(new []{collectionId}, true);
            JObject collectionResponse = obj["response"]["collectiondetails"].ToObject<JArray>()[0].ToObject<JObject>();
            JArray arr = collectionResponse["children"].ToObject<JArray>();

            if (arr != null)
            {
                int count = arr.Count;
                List<string> ids = new List<string>();

                foreach (var jToken in arr)
                {
                    JObject file = (JObject) jToken;
                    if (file.ContainsKey("publishedfileid"))
                    {
                        ids.Add(file["publishedfileid"].ToObject<string>());
                    }
                }

                JObject fobj = await GetWorkshopFileDetailsJSON(ids.ToArray());

                return fobj["response"]["publishedfiledetails"].ToObject<List<WorkshopFileDetails>>();
            }
            else
            {
                Log.Error("Couldn't get array from collection request result. JSON:\n" + obj.ToString());
            }

            return list;
        }

        public static async Task<List<WorkshopFileDetails>> GetWorkshopFileDetails(BaseMod[] mods)
        {
            JObject obj = await GetWorkshopFileDetailsJSON(mods);

            lastResult = obj;

            return obj["response"]["publishedfiledetails"].ToObject<JArray>().ToObject<List<WorkshopFileDetails>>();
        }

        public static async Task<WorkshopFileDetails> GetWorkshopFileDetails(string fileId)
        {
            JObject obj = await GetWorkshopFileDetailsJSON(new []{fileId});

            lastResult = obj;

            return obj["response"]["publishedfiledetails"].ToObject<JArray>()[0].ToObject<WorkshopFileDetails>();
        }

        public static async Task<List<WorkshopFileDetails>> GetWorkshopFileDetails(string[] fileId)
        {
            JObject obj = await GetWorkshopFileDetailsJSON(fileId);

            lastResult = obj;

            return obj["response"]["publishedfiledetails"].ToObject<List<WorkshopFileDetails>>();
        }
    }
}
