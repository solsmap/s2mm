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
    public class EventArgsTranscript : EventArgs
    {
        public string Transcript { get; set; }
        public int ResultIndex { get; set; }

        public EventArgsTranscript(string transcript, int resultIndex)
        {
            Transcript = transcript;
            ResultIndex = resultIndex;
        }
    }

    public class Alternative
    {
        public string transcript { get; set; }
    }

    public class Result
    {
        public List<Alternative> alternatives { get; set; }
        public bool final { get; set; }
    }

    public class RootObject
    {
        public List<Result> results { get; set; }
        public int result_index { get; set; }
        public List<string> warnings { get; set; }
    }

    public delegate void TranscriptEventHandler(object sender, EventArgsTranscript e);

    public class WatsonStreamAdapter
    {
        #region variables

        private const string Password = "b7FDf70iTGjR";
        private const string UserName = "d320547c-e2bf-4077-8e0a-5a2359e8e28a";

        private const string JajpModelString = "ja-JP_BroadbandModel";
        private const string SpeechToTextEndpoint = @"wss://stream.watsonplatform.net/speech-to-text/api/v1/recognize?watson-token={0}&model={1}";

        private static readonly Uri AuthEndpointUri = new Uri(@"https://stream.watsonplatform.net/authorization/api/v1/token?url=https://stream.watsonplatform.net/speech-to-text/api");

        private static readonly ArraySegment<byte> OpenMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"action\": \"start\", \"content-type\": \"audio/l16;rate=22050\", \"continuous\" : true, \"interim_results\": true}"));
        private static readonly ArraySegment<byte> CloseMessage = new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"action\": \"stop\"}"));

        private ClientWebSocket _socket = new ClientWebSocket();

        public static event TranscriptEventHandler TranscriptEvent;

        private static bool _isListening = false;

        public static bool IsListening
        {
            get
            {
                return _isListening;
            }
        }

        #endregion

        public bool IsConnect()
        {
            return _socket.State == WebSocketState.Connecting;
        }

        public async Task ConnectAsync()
        {
            var token = await GetAuthTokenAsync(UserName, Password);
            var uri = GetUri(token, JajpModelString);

            // socket open
            await _socket.ConnectAsync(uri, CancellationToken.None);
            // open message send
            await Task.WhenAll(_socket.SendAsync(OpenMessage, WebSocketMessageType.Text, true, CancellationToken.None), HandleCallback(_socket));
            // send audio
            //await Task.WhenAll(SendAudioToWatson(_socket, filePath), HandleCallback(_socket));
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

                if (IsDelimeter(message))
                {
                    _isListening = true;
                }
                else
                {
                    //T.B.D 何故か配列で来るので、とりあえず０番目を参照・・・
                    //イベント登録あり、解析文字列が１文字以上、最終結果のみ
                    if (TranscriptEvent != null && JsonConvert.DeserializeObject<RootObject>(message).results[0].alternatives[0].transcript.Length > 0 &&
                        JsonConvert.DeserializeObject<RootObject>(message).results[0].final == true)
                    {
                        TranscriptEvent(null, new EventArgsTranscript(JsonConvert.DeserializeObject<RootObject>(message).results[0].alternatives[0].transcript,
                                                                      JsonConvert.DeserializeObject<RootObject>(message).result_index));
                    }
                }
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

        public async Task Close()
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
        }

        public async Task SendAsync(byte[] buffer, int size)
        {
            if (_socket.State == WebSocketState.Open)
            {
                await _socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }
        
        private static Uri GetUri(string token, string model) => new Uri(string.Format(SpeechToTextEndpoint, token, model));
    }
}
