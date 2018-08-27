using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;

namespace 音声認識テストアプリ
{
    public partial class Form1 : Form
    {
        private WaveIn _waveIn = null;
        WatsonStreamAdapter _adapter = null;

        private int _resultIndex = -1;

        public Form1()
        {
            InitializeComponent();

            MMFrame.Media.SpeechRecognition.CreateEngine("MS-1041-80-DESK");

            foreach (RecognizerInfo ri in MMFrame.Media.SpeechRecognition.InstalledRecognizers)
            {
                textBox1.Text = ri.Name + "(" + ri.Culture + ")" + "\r\n" + textBox1.Text;
            }

            MMFrame.Media.SpeechRecognition.SpeechRecognitionRejectedEvent = (e) =>
            {
                textBox1.Text = "認識できません。" + "\r\n" + textBox1.Text;
            };

            MMFrame.Media.SpeechRecognition.SpeechRecognizedEvent = (e) =>
            {
                textBox1.Text = "確定：" + e.Result.Grammar.Name + " " + e.Result.Text + "(" + e.Result.Confidence + ")" + "\r\n" + textBox1.Text;
                if(e.Result.Text == textBox2.Text)
                {
                    MMFrame.Media.SpeechRecognition.RecognizeAsyncCancel();
                    button8_Click(this, e);
                }
            };

            MMFrame.Media.SpeechRecognition.SpeechHypothesizedEvent = (e) =>
            {
                textBox1.Text = "候補：" + e.Result.Grammar.Name + " " + e.Result.Text + "(" + e.Result.Confidence + ")" + "\r\n" + textBox1.Text;
            };

            MMFrame.Media.SpeechRecognition.SpeechRecognizeCompletedEvent = (e) =>
            {
                if (e.Cancelled)
                {
                    textBox1.Text = "キャンセルされました。" + "\r\n" + textBox1.Text;
                }

                textBox1.Text = "認識終了" + "\r\n" + textBox1.Text;
            };

            MMFrame.Media.SpeechRecognition.CreateEngine("MS-1041-80-DESK");

            AddGrammar();

            MMFrame.Media.SpeechRecognition.RecognizeAsync(true);
        }

        private void AddGrammar()
        {
            MMFrame.Media.SpeechRecognition.AddGrammar("weather", new string[] { "今日もイイ天気" });

            string[] words = new string[] { "本日は晴天なり" , textBox2.Text };
            MMFrame.Media.SpeechRecognition.AddGrammar("words", words);

            Choices choices1 = new Choices();
            choices1.Add(new string[] { "バナナ", "りんご", "すいか", "メロン", "みかん", "いちご", "ぶどう", "オレンジ", "グレープフルーツ" });
            GrammarBuilder grammarBuilder1 = new GrammarBuilder();
            grammarBuilder1.Append(choices1);
            grammarBuilder1.Append("が好きだ");
            MMFrame.Media.SpeechRecognition.AddGrammar("seelowe", grammarBuilder1);

            Choices choices2 = new Choices();
            choices2.Add(new string[] { "平原", "街道", "塹壕", "草原", "凍土", "砂漠", "海上", "空中", "泥中", "湿原" });
            GrammarBuilder grammarBuilder2 = new GrammarBuilder();
            grammarBuilder2.AppendWildcard();
            grammarBuilder2.Append("は");
            grammarBuilder2.Append(new SemanticResultKey("field", new GrammarBuilder(choices2)));
            grammarBuilder2.Append("が好きです");
            MMFrame.Media.SpeechRecognition.AddGrammar("field", grammarBuilder2);
        }

        private void button1_Click(object sender, System.EventArgs e)
        {
            MMFrame.Media.SpeechRecognition.RecognizeAsync(true);
        }

        private void button2_Click(object sender, System.EventArgs e)
        {
            MMFrame.Media.SpeechRecognition.RecognizeAsyncCancel();
        }

        private void button3_Click(object sender, System.EventArgs e)
        {
            MMFrame.Media.SpeechRecognition.RecognizeAsyncStop();
        }

        private void button4_Click(object sender, System.EventArgs e)
        {
            MMFrame.Media.SpeechRecognition.CreateEngine("MS-1041-80-DESK");
        }

        private void button5_Click(object sender, System.EventArgs e)
        {
            MMFrame.Media.SpeechRecognition.DestroyEngine();
        }

        private void button6_Click(object sender, System.EventArgs e)
        {
            AddGrammar();
        }

        private void button7_Click(object sender, System.EventArgs e)
        {
            MMFrame.Media.SpeechRecognition.ClearGrammar();
        }

        /// <summary>
        /// Watson音声認識開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button8_Click(object sender, EventArgs e)
        {
            _adapter = new WatsonStreamAdapter();
            Task.Run(_adapter.ConnectAsync);

            _waveIn = new WaveIn()
            {
                DeviceNumber = 0, // Default
            };
            _waveIn.DataAvailable += WaveIn_DataAvailable;
            _waveIn.WaveFormat = new WaveFormat(sampleRate: 44100, channels: 1);
            _waveIn.StartRecording();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var deviceInfo = WaveIn.GetCapabilities(i);
                textBox1.Text = String.Format("音声入力デバイス {0}: {1}, {2} チャンネル\r\n", i, deviceInfo.ProductName, deviceInfo.Channels) + textBox1.Text;
            }
        }

        private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (WatsonStreamAdapter.IsListening == true)
            {
                Task.Run(async () => { await _adapter.SendAsync(e.Buffer, e.BytesRecorded); });
                WatsonStreamAdapter.TranscriptEvent += CallbackTranscript;
            }
        }

        private void CallbackTranscript(object sender, EventArgsTranscript e)
        {
            this.Invoke((MethodInvoker)(() => {
                if(e.ResultIndex != _resultIndex)
                {
                    textBox1.Text = String.Format("解析：{0}\r\n", e.Transcript) + textBox1.Text;
                    _resultIndex = e.ResultIndex;
                }

                if(_resultIndex == -1)
                {
                    MMFrame.Media.SpeechRecognition.RecognizeAsync(true);
                }
            }));
        }
    }
}
