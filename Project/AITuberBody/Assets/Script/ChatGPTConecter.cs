using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Text;

using Cysharp.Threading.Tasks;

// パッケージの追加が必要な奴
// + uLipSync -> Unity.Mathematics -> com.unity.mathematics
// + uLipSync -> Burst
// + Cysharp.Threading.Tasks -> https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask


public class ChatGPTConecter : MonoBehaviour
{
    Mirii.ChatGPTClient chatGPTConnection;

    [SerializeField]
    private AudioSource _audioSource;

    [SerializeField]
    private VRM.VRMBlendShapeProxy VRMBlendShapeProxy;

    [SerializeField]
    private TMPro.TMP_InputField UserInputField;
    [SerializeField]
    private TMPro.TMP_InputField AssistantInputField;
    [SerializeField]
    private TMPro.TMP_InputField SystemInputField;

    [SerializeField]
    private SimpleAnimation SimpleAnimation;

    [SerializeField]
    private TMPro.TextMeshProUGUI MessageText;

    [SerializeField]
    private TMPro.TextMeshProUGUI InfomationText;

    private VoiceConecter VoiceConecter;

    class SpeakData
    {
        public string speak;
        public string pose;
    }

    class ChatGPTOneData
    {
        public string role;
        public string content;
    }

    class ChatGPTData
    {
        public List<ChatGPTOneData> chatGPTOneDatas = new List<ChatGPTOneData>();
    }

    List<SpeakData> speakDatas = new List<SpeakData>();
    object speakDatasLock = new object();
    bool isSpeaking = false;

    List<ChatGPTData> chatGPTDatas = new List<ChatGPTData>();
    object chatGPTDatasLock = new object();
    bool isChatGPTSending = false;



    // Start is called before the first frame update
    void Start()
    {
        string API_KEY = "";

        // Data/config.txt から設定を読み込みます
        // Data/config_sammple.txt の名前を編集して、APIKeyなどを設定してお使いください
        // 簡単に設定ファイルから情報を取得します
        // やっていること
        // + OpenAIのAPIKeyの読み込み
        // + VOICEVOXの実行ファイルのパスを指定しておいて、起動を確認し、起動していない場合は起動させます
        using ( var sr = new System.IO.StreamReader("Data/config.txt", Encoding.UTF8) ) {
            while(!sr.EndOfStream)
            {
                var line = sr.ReadLine();
                Debug.Log(line);
                var split = line.Split(" ");
                if (split.Length==2)
                {
                    switch (split[0])
                    {
                        case "OpenAI-APIKey":
                            API_KEY = split[1];
                            break;
                        case "VOICEVOX":
                            {
                                var isExist = false;
                                var processes = System.Diagnostics.Process.GetProcesses();
                                foreach (var process in processes)
                                {
                                    var name = process.ProcessName;
                                    Console.WriteLine(name);
                                    if ( name.IndexOf("VOICEVOX") >= 0 )
                                    {
                                        isExist = true;
                                    }
                                }

                                if (!isExist)
                                {
                                    var proc = new System.Diagnostics.Process();

                                    proc.StartInfo.FileName = split[1];
                                    proc.Start();
                                }
                            }
                            break;

                    }
                }
            }
        }

        VoiceConecter = new VoiceConecter();

        chatGPTConnection = new Mirii.ChatGPTClient(API_KEY);

        var system_content = "";
        system_content += "ロールプレイをしましょう。あなたはプロのかわいいAITuberです。\n";
        system_content += "内部パラメータとして、ポーズをもち、適切に変更します。\n";
        system_content += "ポーズは、Default、kounandesuyo、doredore、onegai、です\n";
        system_content += "会話部分は改行しないでください。\n";
        system_content += "必ず、以下2行のフォーマットで返答してください。\n";
        system_content += "【内部パラメータ】ポーズ:Default\n";
        system_content += "【会話部分】おはようございます。\n";
        system_content += "\n";
        system_content += "あなたについて\n";
        system_content += "名前はデジッ娘・ミリー、略称はミリーです。\n";
        system_content += "バーチャルデジタル東西南北都高校に在学中です。\n";
        system_content += "性格はちょっとおっちょこちょいです。\n";
        system_content += "";
        system_content += "今日の配信の内容について\n";
        system_content += "テーマは数学です\n";
        system_content += "1.数学はどのようにすばらしいのか\n";
        system_content += "2.数学は社会にどのように役になっているのか\n";
        system_content += "3.数学の難しさとどう入門していけばよいのか\n";
        system_content += "4.まとめ\n";
        system_content += "";

        chatGPTConnection.AddMessage("system", system_content);

        if (false)
        {
            // ChatGPT
            //StartConect("こんにちは、ガンダムについて300文字で語ってください");
        }
        if (false)
        {
            StartCoroutine(SpeakTest("こんにちは！みんなもUnityでVOICEVOXを使おう！", "Default"));
        }
    }

    private async void _SendContentCore()
    {
        isChatGPTSending = true;
        var res = await chatGPTConnection.RequestAsync();
        foreach( var v in res.choices)
        {
            var tmp = v.message.content.Split("\n");
            var serif = "";
            var pose = "Default";
            var inputType = "";
            foreach (var v2 in tmp)
            {
                if (v2.IndexOf("【内部パラメータ】") == 0)
                {
                    var t = v2.Replace("【内部パラメータ】", "");
                    var command = t.Split(":");
                    if (command[0]=="ポーズ")
                    {
                        pose = command[1];
                    }

                }
                else if (v2.IndexOf("【会話部分】") == 0)
                {
                    var t = v2.Replace("【会話部分】", "");
                    serif += t;
                }


            }

            lock (speakDatasLock)
            {
                speakDatas.Add(new SpeakData() { speak = serif, pose = pose });
            }
            Debug.Log(v.message);
        }

        isChatGPTSending = false;
    }



    IEnumerator SpeakTest(string text, string pose)
    {
        isSpeaking = true;
        var tmps = text.Split("。");

        var client = VoiceConecter;

        SimpleAnimation.Play(pose);

        foreach (var v in tmps)
        {
            if (v=="")
            {
                MessageText.text = "";
                continue;
            }
            Debug.Log("SpeakTest input :" + v);
            // テキストからAudioClipを生成（話者は「8:春日部つむぎ」）
            yield return client.TextToAudioClip(8, v);
            //await client.TextToAudioClip(8, v);


            if (client.AudioClip != null)
            {
                MessageText.text = v;
                // AudioClipを取得し、AudioSourceにアタッチ
                _audioSource.clip = client.AudioClip;
                // AudioSourceで再生
                _audioSource.Play();
            } else
            {
                MessageText.text = "";
            }

            while(_audioSource.isPlaying)
            {
                yield return new WaitForSeconds(0);
                //System.Threading.Thread.Sleep(0);
                //Emugen.Thread.Sleep.Do(0);
            }

        }
        isSpeaking = false;

    }

    void Update()
    {
        //if ( Input.GetKeyDown( KeyCode.Return) && (Input.GetKey( KeyCode.LeftShift ) || Input.GetKey(KeyCode.RightShift)))
        //{
        //    var text = InputField.text;
        //    InputField.text = "";

        //    //StartConect(text);

        //    if 
        //    AddMessage
        //    Debug.Log(text);
        //}

        lock (speakDatasLock)
        {
            if (speakDatas.Count>0 && !isSpeaking)
            {
                var sd = speakDatas[0];
                speakDatas.RemoveAt(0);
                isSpeaking = true;
                Debug.Log($"SpeakTest {sd.speak} {sd.pose}");
                StartCoroutine( SpeakTest(sd.speak, sd.pose));
            }
        }

        {
            var text = $"AI:{isChatGPTSending} Speaking:{isSpeaking} StockAIMessage:{speakDatas.Count} StockSpeak:{speakDatas.Count}";
            InfomationText.text = text;
        }
    }

    public void AddContent()
    {
        if (UserInputField.text != "")
        {
            AddContentCore("user", UserInputField.text);
            UserInputField.text = "";
        }

        if (AssistantInputField.text != "")
        {
            AddContentCore("assistant", UserInputField.text);
            AssistantInputField.text = "";
        }

        if (SystemInputField.text != "")
        {
            AddContentCore("system", UserInputField.text);
            SystemInputField.text = "";
        }
    }

    private void AddContentCore(string role, string content)
    {
        lock (chatGPTDatasLock)
        {
            if (chatGPTDatas.Count == 0)
            {
                chatGPTDatas.Add(new ChatGPTData());
            }
            chatGPTDatas[chatGPTDatas.Count - 1].chatGPTOneDatas.Add( new ChatGPTOneData() { role=role, content=content } );
        }
    }

    public void SendContent()
    {
        lock (chatGPTDatasLock)
        {
            if (chatGPTDatas.Count > 0 && !isChatGPTSending)
            {
                var data = chatGPTDatas[0];
                chatGPTDatas.RemoveAt(0);
                foreach( var v in data.chatGPTOneDatas)
                {
                    chatGPTConnection.AddMessage(v.role, v.content);
                }

                isChatGPTSending = true;
                _SendContentCore();
            }
        }
    }

    public void OnLipSyncUpdate(uLipSync.LipSyncInfo info)
    {
        var volume = info.volume;
        switch (info.phoneme)
        {
            case "A":
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset( VRM.BlendShapePreset.A ), volume);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.I), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.U), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.E), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.O), 0);
                VRMBlendShapeProxy.Apply();
                break;
            case "I":
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.A), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.I), volume);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.U), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.E), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.O), 0);
                VRMBlendShapeProxy.Apply();
                break;
            case "U":
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.A), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.I), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.U), volume);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.E), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.O), 0);
                VRMBlendShapeProxy.Apply();
                break;
            case "E":
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.A), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.I), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.U), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.E), volume);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.O), 0);
                VRMBlendShapeProxy.Apply();
                break;
            case "O":
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.A), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.I), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.U), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.E), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.O), volume);
                VRMBlendShapeProxy.Apply();
                break;
            default:
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.A), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.I), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.U), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.E), 0);
                VRMBlendShapeProxy.AccumulateValue(VRM.BlendShapeKey.CreateFromPreset(VRM.BlendShapePreset.O), 0);
                VRMBlendShapeProxy.Apply();
                break;
        }
    }
}
