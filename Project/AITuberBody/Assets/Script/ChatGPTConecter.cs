using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using System.Text;

using Cysharp.Threading.Tasks;

// �p�b�P�[�W�̒ǉ����K�v�ȓz
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

        // Data/config.txt ����ݒ��ǂݍ��݂܂�
        // Data/config_sammple.txt �̖��O��ҏW���āAAPIKey�Ȃǂ�ݒ肵�Ă��g����������
        // �ȒP�ɐݒ�t�@�C����������擾���܂�
        // ����Ă��邱��
        // + OpenAI��APIKey�̓ǂݍ���
        // + VOICEVOX�̎��s�t�@�C���̃p�X���w�肵�Ă����āA�N�����m�F���A�N�����Ă��Ȃ��ꍇ�͋N�������܂�
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
        system_content += "���[���v���C�����܂��傤�B���Ȃ��̓v���̂��킢��AITuber�ł��B\n";
        system_content += "�����p�����[�^�Ƃ��āA�|�[�Y�������A�K�؂ɕύX���܂��B\n";
        system_content += "�|�[�Y�́ADefault�Akounandesuyo�Adoredore�Aonegai�A�ł�\n";
        system_content += "��b�����͉��s���Ȃ��ł��������B\n";
        system_content += "�K���A�ȉ�2�s�̃t�H�[�}�b�g�ŕԓ����Ă��������B\n";
        system_content += "�y�����p�����[�^�z�|�[�Y:Default\n";
        system_content += "�y��b�����z���͂悤�������܂��B\n";
        system_content += "\n";
        system_content += "���Ȃ��ɂ���\n";
        system_content += "���O�̓f�W�b���E�~���[�A���̂̓~���[�ł��B\n";
        system_content += "�o�[�`�����f�W�^��������k�s���Z�ɍ݊w���ł��B\n";
        system_content += "���i�͂�����Ƃ������傱���傢�ł��B\n";
        system_content += "";
        system_content += "�����̔z�M�̓��e�ɂ���\n";
        system_content += "�e�[�}�͐��w�ł�\n";
        system_content += "1.���w�͂ǂ̂悤�ɂ��΂炵���̂�\n";
        system_content += "2.���w�͎Љ�ɂǂ̂悤�ɖ��ɂȂ��Ă���̂�\n";
        system_content += "3.���w�̓���Ƃǂ����債�Ă����΂悢�̂�\n";
        system_content += "4.�܂Ƃ�\n";
        system_content += "";

        chatGPTConnection.AddMessage("system", system_content);

        if (false)
        {
            // ChatGPT
            //StartConect("����ɂ��́A�K���_���ɂ���300�����Ō���Ă�������");
        }
        if (false)
        {
            StartCoroutine(SpeakTest("����ɂ��́I�݂�Ȃ�Unity��VOICEVOX���g�����I", "Default"));
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
                if (v2.IndexOf("�y�����p�����[�^�z") == 0)
                {
                    var t = v2.Replace("�y�����p�����[�^�z", "");
                    var command = t.Split(":");
                    if (command[0]=="�|�[�Y")
                    {
                        pose = command[1];
                    }

                }
                else if (v2.IndexOf("�y��b�����z") == 0)
                {
                    var t = v2.Replace("�y��b�����z", "");
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
        var tmps = text.Split("�B");

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
            // �e�L�X�g����AudioClip�𐶐��i�b�҂́u8:�t�����ނ��v�j
            yield return client.TextToAudioClip(8, v);
            //await client.TextToAudioClip(8, v);


            if (client.AudioClip != null)
            {
                MessageText.text = v;
                // AudioClip���擾���AAudioSource�ɃA�^�b�`
                _audioSource.clip = client.AudioClip;
                // AudioSource�ōĐ�
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
