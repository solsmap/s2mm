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

namespace 音声認識テストアプリ
{
    public partial class Form1 : Form
    {
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
    }
}
