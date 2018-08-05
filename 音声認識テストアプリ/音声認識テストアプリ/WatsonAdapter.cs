using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace 音声認識テストアプリ
{
    public class WatsonAdapter
    {
        #region variables

        private const string Password = "b7FDf70iTGjR";
        private const string UserName = "d320547c-e2bf-4077-8e0a-5a2359e8e28a";

        private const string JajpModelString = "ja-JP_BroadbandModel";
        private const string SpeechToTextEndpoint = @"wss://stream.watsonplatform.net/speech-to-text/api/v1/recognize?watson-token={0}&model={1}";

        private static readonly Uri AuthEndpointUri = new Uri(@"https://stream.watsonplatform.net/authorization/api/v1/token?url=https://stream.watsonplatform.net/speech-to-text/api");

        private static readonly ArraySegment<byte> OpenMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"action\": \"start\", \"content-type\": \"audio/mp3\", \"continuous\" : true, \"interim_results\": true}"));
        private static readonly ArraySegment<byte> CloseMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"action\": \"stop\"}"));

        #endregion

        //classをnewしてこのメソッド呼べば動きます。
        //今回は、音声データ（wav）のファイルパスを指定する想定で書いてます。
        public async Task SendAudioFileAsync(string filePath)
        {
            var token = await GetAuthTokenAsync(UserName, Password);
            var uri = GetUri(token, JajpModelString);

            var socket = new ClientWebSocket();
            // socket open
            await socket.ConnectAsync(uri, CancellationToken.None);
            // open message send
            await Task.WhenAll(socket.SendAsync(OpenMessage, WebSocketMessageType.Text, true, CancellationToken.None), HandleCallback(socket));
            // send audio
            await Task.WhenAll(SendAudioToWatson(socket, filePath), HandleCallback(socket));

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);

        }

        private static async Task SendAudioToWatson(ClientWebSocket client, string filePath)
        {
            using (var fileStream = File.OpenRead(filePath))
            {
                var bytes = new byte[1024];
                while (fileStream.Read(bytes, 0, bytes.Length) > 0)
                {
                    await client.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
                await client.SendAsync(CloseMessage, WebSocketMessageType.Text, true, CancellationToken.None);
            }
        }


        private static async Task HandleCallback(ClientWebSocket socket)
        {
            var buffer = new byte[1024];
            while (true)
            {
                var segment = new ArraySegment<byte>(buffer);
                var result = await socket.ReceiveAsync(segment, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close) return;


                var count = result.Count;
                while (!result.EndOfMessage)
                {
                    if (count >= buffer.Length)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.InvalidPayloadData, "count >= buffer.Length!!!!!", CancellationToken.None);
                        return;
                    }

                    segment = new ArraySegment<byte>(buffer, count, buffer.Length - count);
                    result = await socket.ReceiveAsync(segment, CancellationToken.None);
                    count += result.Count;
                }

                var message = Encoding.UTF8.GetString(buffer, 0, count);
                // logging
                Console.WriteLine(message);

                if (IsDelimeter(message)) return;
            }


        }

        private static bool IsDelimeter(string json) => JsonConvert.DeserializeObject<dynamic>(json).state == "listening";

        private static async Task<string> GetAuthTokenAsync(string user, string pass)
        {
            var credential = Encoding.UTF8.GetBytes($"{user}:{pass}");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(credential));
                var response = await client.GetAsync(AuthEndpointUri);

                return await response.Content.ReadAsStringAsync();
            }
        }

        private static Uri GetUri(string token, string model) => new Uri(string.Format(SpeechToTextEndpoint, token, model));
    }
}
