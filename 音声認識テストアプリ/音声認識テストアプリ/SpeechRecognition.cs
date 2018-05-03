#define Use_SystemSpeech

#if Use_MicrosoftSpeech || Use_SystemSpeech
#if Use_MicrosoftSpeech
using Microsoft.Speech.Recognition;
#elif Use_SystemSpeech
using System.Speech.Recognition;
#endif

namespace MMFrame.Media
{
    /// <summary>
    /// 音声認識に関するクラス
    /// </summary>
    public static class SpeechRecognition
    {
        /// <summary>
        /// 音声認識エンジンを設定、取得します。
        /// </summary>
        public static SpeechRecognitionEngine Engine;

        /// <summary>
        /// 音声認識エンジンが利用可能かどうかを取得します。
        /// </summary>
        public static bool IsAvailable
        {
            get
            {
                return (Engine != null && !IsDestroyed);
            }
        }

        /// <summary>
        /// 音声認識を実行中かどうかを取得、設定します。
        /// </summary>
        public static bool IsRecognizing
        {
            get
            {
                return (IsAvailable && Engine.AudioState != AudioState.Stopped);
            }
        }

        /// <summary>
        /// このコンピュータでサポートしている音声認識エンジンを取得します。
        /// </summary>
        public static System.Collections.ObjectModel.ReadOnlyCollection<RecognizerInfo> InstalledRecognizers
        {
            get
            {
                return SpeechRecognitionEngine.InstalledRecognizers();
            }
        }

        /// <summary>
        /// 一時的に音声の一部を認識した場合のイベントを設定、取得します。
        /// </summary>
        public static System.Action<SpeechHypothesizedEventArgs> SpeechHypothesizedEvent;

        /// <summary>
        /// 信頼性の高い 1 つ以上の句を認識した場合のイベントを設定、取得します。
        /// </summary>
        public static System.Action<SpeechRecognizedEventArgs> SpeechRecognizedEvent;

        /// <summary>
        /// 信頼性の低い候補句のみ認識した場合のイベントを設定、取得します。
        /// </summary>
        public static System.Action<SpeechRecognitionRejectedEventArgs> SpeechRecognitionRejectedEvent;

        /// <summary>
        /// 音声認識が終了した場合のイベントを設定、取得します。
        /// </summary>
        public static System.Action<RecognizeCompletedEventArgs> SpeechRecognizeCompletedEvent;

        /// <summary>
        /// 音声認識エンジンが破棄されているかどうかを取得、設定します。
        /// </summary>
        private static bool IsDestroyed;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        static SpeechRecognition()
        {
            IsDestroyed = true;
        }

        /// <summary>
        /// システム既定の音声認識エンジンを作成します。
        /// System.Speech が利用できる言語は OS の標準言語のみとなります。
        /// </summary>
        public static void CreateEngine()
        {
            CreateEngine<object>(null);
        }

        /// <summary>
        /// 認識する言語を指定して、音声認識エンジンを作成します。
        /// </summary>
        /// <param name="culture">認識する言語</param>
        public static void CreateEngine(System.Globalization.CultureInfo culture)
        {
            CreateEngine<System.Globalization.CultureInfo>(culture);
        }

        /// <summary>
        /// 音声認識エンジンの名前を指定して、音声認識エンジンを作成します。
        /// SR_MS_ja-JP_TELE_11.0 などの名前を指定します。
        /// </summary>
        /// <param name="engineName">音声認識エンジン名</param>
        public static void CreateEngine(string engineName)
        {
            CreateEngine<string>(engineName);
        }

        /// <summary>
        /// 音声認識エンジンを破棄します。
        /// </summary>
        public static void DestroyEngine()
        {
            if (!IsAvailable)
            {
                return;
            }

            Engine.SpeechHypothesized -= SpeechHypothesized;
            Engine.SpeechRecognized -= SpeechRecognized;
            Engine.SpeechRecognitionRejected -= SpeechRecognitionRejected;
            Engine.RecognizeCompleted -= SpeechRecognizeCompleted;
            Engine.UnloadAllGrammars();
            Engine.Dispose();

            IsDestroyed = true;
        }

        /// <summary>
        /// ルール型による音声認識方法を追加します。
        /// </summary>
        /// <param name="grammarName">文法名</param>
        /// <param name="words">追加する語彙</param>
        public static void AddGrammar(string grammarName, params string[] words)
        {
            Choices choices = new Choices();
            choices.Add(words);

            GrammarBuilder grammarBuilder = new GrammarBuilder();
            grammarBuilder.Append(choices);

            AddGrammar(grammarName, grammarBuilder);
        }

        /// <summary>
        /// ルール型による音声認識方法を追加します。
        /// </summary>
        /// <param name="grammarName">文法名</param>
        /// <param name="grammarBuilder">音声認識の文法</param>
        public static void AddGrammar(string grammarName, GrammarBuilder grammarBuilder)
        {
            Grammar grammar = new Grammar(grammarBuilder)
            {
                Name = grammarName
            };

            AddGrammar(grammar);
        }

        /// <summary>
        /// ルール型による音声認識方法を追加します。
        /// </summary>
        /// <param name="grammar">音声認識の文法</param>
        public static void AddGrammar(Grammar grammar)
        {
            if (!IsAvailable)
            {
                return;
            }

            Engine.LoadGrammar(grammar);
        }

        /// <summary>
        /// 自由発話のディクテーション型による音声認識方法を追加します。Grammar.Name は Dictation です。
        /// System.Speech.dll のみ使用可能です。
        /// </summary>
        public static void AddDictation()
        {
#if Use_SystemSpeech
            DictationGrammar dictation = new DictationGrammar()
            {
                Name = "Dictation"
            };

            AddGrammar(dictation);
#endif
        }

        /// <summary>
        /// 登録されている音声認識方法を削除します。
        /// </summary>
        /// <param name="grammarName">文法名</param>
        public static void ClearGrammar(string grammarName)
        {
            if (!IsAvailable)
            {
                return;
            }

            foreach (Grammar g in Engine.Grammars)
            {
                if (g.Name == grammarName)
                {
                    Engine.UnloadGrammar(g);
                    break;
                }
            }
        }

        /// <summary>
        /// 登録されているすべての音声認識方法を削除します。
        /// </summary>
        public static void ClearGrammar()
        {
            if (!IsAvailable)
            {
                return;
            }

            Engine.UnloadAllGrammars();
        }

        /// <summary>
        /// 非同期で音声認識を開始します。
        /// </summary>
        /// <param name="multiple">常に音声を認識する場合は true</param>
        public static void RecognizeAsync(bool multiple)
        {
            if (IsRecognizing || Engine.Grammars.Count <= 0)
            {
                return;
            }

            RecognizeMode mode = (multiple) ? RecognizeMode.Multiple : RecognizeMode.Single;
            Engine.RecognizeAsync(mode);
        }

        /// <summary>
        /// 現在の音声認識操作の完了を待たずに非同期認識を終了します。
        /// </summary>
        public static void RecognizeAsyncCancel()
        {
            if (!IsRecognizing)
            {
                return;
            }

            Engine.RecognizeAsyncCancel();
        }

        /// <summary>
        /// 現在の音声認識操作の完了後に非同期認識を終了します。
        /// </summary>
        public static void RecognizeAsyncStop()
        {
            if (!IsRecognizing)
            {
                return;
            }

            Engine.RecognizeAsyncStop();
        }

        // 音声認識エンジンを作成します。
        private static void CreateEngine<T>(object arg)
        {
            if (IsAvailable)
            {
                return;
            }

            if (arg == null)
            {
                Engine = new SpeechRecognitionEngine();
            }
            else if (typeof(T) == typeof(System.String))
            {
                Engine = new SpeechRecognitionEngine((string)arg);
            }
            else if (typeof(T) == typeof(System.Globalization.CultureInfo))
            {
                Engine = new SpeechRecognitionEngine((System.Globalization.CultureInfo)arg);
            }
            else
            {
                return;
            }

            IsDestroyed = false;

            Engine.SetInputToDefaultAudioDevice();

            Engine.SpeechHypothesized += SpeechHypothesized;
            Engine.SpeechRecognized += SpeechRecognized;
            Engine.SpeechRecognitionRejected += SpeechRecognitionRejected;
            Engine.RecognizeCompleted += SpeechRecognizeCompleted;
        }

        // 一時的に音声の一部を認識した場合のイベント
        private static void SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (e.Result != null && SpeechHypothesizedEvent != null)
            {
                SpeechHypothesizedEvent(e);
            }
        }

        // 信頼性の高い 1 つ以上の句を認識した場合のイベント
        private static void SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result != null && SpeechRecognizedEvent != null)
            {
                SpeechRecognizedEvent(e);
            }
        }

        // 信頼性の低い候補句のみ認識した場合のイベント
        private static void SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (e.Result != null && SpeechRecognitionRejectedEvent != null)
            {
                SpeechRecognitionRejectedEvent(e);
            }
        }

        // 音声認識が終了した場合のイベント
        private static void SpeechRecognizeCompleted(object sender, RecognizeCompletedEventArgs e)
        {
            if (e.Result != null && SpeechRecognizeCompletedEvent != null)
            {
                SpeechRecognizeCompletedEvent(e);
            }
        }
    }
}
#endif