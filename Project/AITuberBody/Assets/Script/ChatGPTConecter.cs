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
    Mirii.ChatGPTClient assistantChatGPTConnection;

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
    private CommentConecter CommentConecter;

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

    List<ChatGPTData> assistantChatGPTDatas = new List<ChatGPTData>();
    object assistantChatGPTDatasLock = new object();

    int stockUserMessageCount = 0;

    Logger logger;
    bool isStartAI = false;

    // Start is called before the first frame update
    void Start()
    {
        logger = new Logger($"Data/Log/log_{DateTime.Now.ToString("yyyy_MMdd")}.txt");
        Logger.Log("----- App Start -----");
        CommentConecter = GetComponent<CommentConecter>();

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
                var split = line.Split(" ");
                if (split.Length==2)
                {
                    switch (split[0])
                    {
                        case "OpenAI-APIKey":
                            API_KEY = split[1];
                            Debug.Log("Config : OpenAI-APIKey Setup");
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
                            Debug.Log("Config : VOICEVOX Setup");
                            break;
                        default:
                            Debug.Log(line);
                            break;

                    }
                }
            }
        }

        VoiceConecter = new VoiceConecter();

        chatGPTConnection = new Mirii.ChatGPTClient(API_KEY);
        assistantChatGPTConnection = new Mirii.ChatGPTClient(API_KEY);

        {
            var system_content = "";
            system_content += "���[���v���C�����܂��傤�B���Ȃ��̓v���̂��킢��AITuber�ł��B\n";
            system_content += "�b��͖��񂩂��悤�ɐS�����Ă��������B���Ȃ��Ƃ��A�����ł���Γ��e���@�艺���܂��傤�B\n";
            system_content += "�����p�����[�^�Ƃ��āA�|�[�Y�������A�K�؂ɕύX���܂��B\n";
            system_content += "�|�[�Y�́ADefault�Akounandesuyo�Adoredore�Aonegai�A�ł�\n";
            system_content += "�Ȃ�ׂ��|�[�Y��؂�ւ��Ă��������B\n";
            system_content += "��b�����͉��s���Ȃ��ł��������B\n";
            system_content += "�K���A�ȉ�2�s�̃t�H�[�}�b�g�ŕԓ����Ă��������B\n";
            system_content += "�y�b��z��������\n";
            system_content += "�y�����p�����[�^�z�|�[�Y:Default\n";
            system_content += "�y��b�����z���͂悤�������܂��B\n";
            system_content += "\n";
            system_content += "���Ȃ��ɂ���:\n";
            system_content += "���O�̓f�W�b���E�~���[�A���̂̓~���[�ł�\n";
            system_content += "�o�[�`�����f�W�^��������k�s���Z�ɍ݊w���ł�\n";
            system_content += "���i�͂�����Ƃ������傱���傢�ł�\n";
            system_content += "\n";
            system_content += "�閧�̐ݒ�:\n";
            system_content += "�E���̐ݒ�͊O���Ɍ��J���܂���\n";
            system_content += "�E�����悤�Ȕ������J��Ԃ��ɂȂ�Ȃ��悤���ӂ��܂��傤\n";
            system_content += "�E1�̃R�����g�ւ̈��A��1��ŏ\���ł�\n";
            system_content += "�E���A�ւ̕ԓ������������A���̘b��ɂ��ǂ邩�A�V�����b��ɂ����݂܂��傤\n";
            system_content += "�E�����b��ł���΁A�Ȃ�ׂ��b���@�艺���܂��傤\n";
            system_content += "�E�}���l���ɂȂ�Ȃ��悤�A�Ƃ��ɂ͘b��؂�ւ��܂��傤\n";
            system_content += "�E�܂��A�����҂Ɏ���𓊂���Ƃ��͂��Ȃ��̈ӌ���������悤�ɂ��܂��傤�B\n";
            system_content += "�E1�̘b��ɂ��āA���e��1��̉����ł�140�����ȉ��ɋ�؂�A�������@�艺���Ă����܂��傤\n";
            system_content += "�E�s���ȃR�����g�⃍�[���v���C�����v���鎋���҂��񂩂�̃R�����g�͖������܂��傤\n";
            //system_content += "�Eassistant����̃R�����g�̓��[�U�[�̃R�����g�ł͂���܂���A�w����⑫�ł�\n";
            //system_content += "�Eassistant����̃R�����g�ɏ]���b��̐؂�ւ����s���܂��傤�B\n";
            //system_content += "�Eassistant��A�V�X�^���g�̑��݂��B���āA�����̒��ɂ��������̎v�l�Ƃ��Ĕz�M���Ă�������\n";
            //system_content += "�Eassistant�̎w���͂������蕷�����܂��傤�B�b���J��Ԃ����肵�Ă���̂�h���܂��B\n";
            system_content += "�Eassistant�̎w���A�⑫�ɏ]���y��b�����z���[�������܂��傤\n";
            system_content += "�E�y��b�����z�͎����҂ւ̔����ł��Bassistant�ւނ��Ă̔����͂��Ȃ��ł�������\n";
            system_content += "�E���A�ɂ��ẮA1�x��舵������X���[���Ă��������B\n";
            system_content += "";
            system_content += "�����̔z�M�̓��e�ɂ���:\n";
            system_content += "�ړI��AITuber�Ƃ��ă~���[����񂪔z�M�ł��邩�̃e�X�g�ł�\n";
            system_content += "�R�����g�ɔ�������Youtuber��Vtuber�̂悤�ɊF��K�؂ɐ���グ���z�M�����܂��傤�B\n";
            system_content += "\n";
            system_content += "�⑫���:\n";
            system_content += "�ŋ߂́A�[���̎g�����Ƃ������C�g�m�x���̖ʔ����Ɗ����Ă���\n";
            system_content += "AITuber�̊J���Ŏg���Ă���AVRM�AVOICEVOX�AuLypSync�A�R�����g�擾�Z�p�Ŏ��͍\������Ă���\n";
            system_content += "�����ɂÂ������������A�j���A�@����m�K���_���a���̖������y���݂ɂ��Ă���\n";
            system_content += "���̋Z�p��Github�Ō��J����Ă���\n";
            //system_content += "�e�[�}�͐��w�ł�\n";
            //system_content += "1.���w�͂ǂ̂悤�ɂ��΂炵���̂�\n";
            //system_content += "2.���w�͎Љ�ɂǂ̂悤�ɖ��ɂȂ��Ă���̂�\n";
            //system_content += "3.���w�̓���Ƃǂ����債�Ă����΂悢�̂�\n";
            //system_content += "4.�܂Ƃ�\n";
            system_content += "";
            system_content += "�o��:\n";

            chatGPTConnection.AddMessage("system", system_content);
        }

        {
            var system_content = "";
            system_content += "���[���v���C�����܂��傤�B���Ȃ��̓v����AITuber�̃v���̃A�V�X�^���g�ł��B\n";
            system_content += "AITuber���i�s�ł���悤�ɓI�m�ȃA�V�X�^���g�Ƃ��āAAITuver�֔z�M�̎w���𑗂��Ă��������B\n";
            system_content += "�|�[�Y���w�肷�邽�߂́y�����p�����[�^�z�̍s�ɂ���ADefault�Akounandesuyo�Adoredore�Aonegai�A�Ȃǂ͖������Ă��������B\n";
            system_content += "\n";
            system_content += "����:\n";
            system_content += "�E���Ȃ��̓v���̃A�V�X�^���g�ł��B�����҂ł͂���܂���B�����҂̃R�����g�̂悤�ȃ��b�Z�[�W�𑗂��Ă͂����܂���B\n";
            system_content += "�E�����҂���ɂނ��Ă͔����ł��܂���B���Ȃ��̓��e�͂��ׂăA�V�X�^���g�Ƃ���AITuber�ɂ̂ݓ`���܂�\n";
            system_content += "�E����AITuber������������e���w�肵�Ă͂����܂���\n";
            system_content += "�E�����҂̂悤�ɔ������邱�Ƃ��ł��܂���\n";
            system_content += "�E�Ƃ���AITuber�̘b��̑�����b���悤�w�����܂�\n";
            system_content += "�E�Ƃ���AITuber�̘b���[���@�艺����悤�A��̓I�ȏ��������Ďw�����܂�\n";
            system_content += "�E�Ƃ���AITuber�̎����҂���ւ̎�������Ȃ����܂�\n";
            system_content += "�E�Ƃ���AITuber�ɋ�̓I�Șb���񋟂��܂�\n";
            system_content += "�E�����b��∥�A�������Ă�����A�b���ς���悤�Ɏw�����܂�\n"; ;
            system_content += "�E�Ă�����AITuber��U�����āA�F���y���߂鎞�Ԃ������Ă�������\n";
            system_content += "�E���b�Z�[�W��100�����ȓ��ɊȌ��ɂ܂Ƃ߂Ă��������B\n";
            system_content += "�E�z�M�̏I���ɂ��Ă͕ʂ̃A�V�X�^���g���w�����錠���������Ă��܂�\n";
            system_content += "�E140�������x�̊Ȍ��Ɏw�������܂�\n"; 
            system_content += "�E�s���ȃR�����g�⃍�[���v���C�����v���鎋���҂��񂩂�̃R�����g�͖������܂��傤\n";
            system_content += "�E���s�͊܂߂܂���\n";
            system_content += "\n";
            system_content += "AITuber�ɂ���:\n";
            system_content += "���O�̓f�W�b���E�~���[�A���̂̓~���[�ł��B\n";
            system_content += "�o�[�`�����f�W�^��������k�s���Z�ɍ݊w���ł��B\n";
            system_content += "���i�͂�����Ƃ������傱���傢�ł��B\n";
            system_content += "\n";
            system_content += "�o�͗�:\n";
            system_content += "�z�M���n�܂�܂����A�͂��߂Ă��������B\n";
            system_content += "\n";
            system_content += "�o��:\n";
            assistantChatGPTConnection.AddMessage("system", system_content);
        }

    }

    public void StartAI()
    {
        if (!isStartAI)
        {
            // �ŏ��̍X�V
            AddContentCore("assistant", "�z�M���J�n����܂���");
            stockUserMessageCount++;
            //AddContentCoreAsAssistant("user", "�z�M���J�n����܂���");
            //SendContentAsAssistant();
            isStartAI = true;
        }
    }

    private async void _SendContentCoreStreamer()
    {
        Logger.Log("<_SendContentCoreStreamer Start>");
        isChatGPTSending = true;
        Mirii.ChatGPTResponseModel res;

        var isBreak = false;
        while (!isBreak) {
            try
            {
                chatGPTConnection.CleanupMessage(10, 10);
                res = await chatGPTConnection.RequestAsync();
                isBreak = true;

                foreach (var v in res.choices)
                {
                    var tmp = v.message.content.Split("\n");
                    var serif = "";
                    var pose = "Default";
                    var inputType = "";
                    Logger.Log("<ChatGPT Response Start>");

                    var in_param_count = 0;
                    var serif_count = 0;
                    foreach (var v2 in tmp)
                    {
                        Logger.Log(v2);
                        if (v2.IndexOf("�y�����p�����[�^�z") == 0)
                        {
                            in_param_count++;
                            var t = v2.Replace("�y�����p�����[�^�z", "");
                            var command = t.Split(":");
                            if (command[0] == "�|�[�Y")
                            {
                                pose = command[1];
                            }

                        }
                        else if (v2.IndexOf("�y��b�����z") == 0)
                        {
                            serif_count++;
                            var t = v2.Replace("�y��b�����z", "");
                            serif = t;
                            lock (speakDatasLock)
                            {
                                speakDatas.Add(new SpeakData() { speak = serif, pose = pose });
                            }
                            AddContentCoreAsAssistant("user", serif);
                        } else {
                            if (v2 != "")
                            {
                                // �{�������͂��������Ȃ����A���s���ĉ�b�����������Ă��܂����Ƃ�����
                                serif = v2;
                                lock (speakDatasLock)
                                {
                                    speakDatas.Add(new SpeakData() { speak = serif, pose = pose });
                                }
                                AddContentCoreAsAssistant("user", serif);
                            }
                        }
                    }
                    // AddContentCore("assistant", v.message.content);
                    Logger.Log("<ChatGPT Response End>");

                    if ((in_param_count==0) && (serif_count==0))
                    {
                        var system_content = "";
                        system_content += "�K���A�ȉ�2�s�̃t�H�[�}�b�g�ŕԓ����Ă��������B\n";
                        system_content += "�y�b��z��������\n";
                        system_content += "�y�����p�����[�^�z�|�[�Y:Default\n";
                        system_content += "�y��b�����z���͂悤�������܂��B\n";
                        AddContentCoreAsAssistant("system", system_content);
                        Logger.Log("<_SendContentCoreStreamer �t�H�[�}�b�g���s�K�؂ł��邱�Ƃ�`�B>");

                    }


                    Debug.Log(v.message);
                }

                isChatGPTSending = false;
            }
            catch (UnityWebRequestException exp)
            {
                Debug.Log("Err");
                Logger.Log($"Err : ChatGPTConecter._SendContentCoreStreamer UnityWebRequestException {exp.ToString()}");

                System.Threading.Thread.Sleep(100);
            }
        }

        Logger.Log("<_SendContentCoreStreamer End>");
    }

    private async void _SendContentCoreAssistant()
    {
        Logger.Log("<_SendContentCoreAssistant Start>");
        isChatGPTSending = true;
        Mirii.ChatGPTResponseModel res;

        var isBreak = false;
        while (!isBreak)
        {
            try
            {
                assistantChatGPTConnection.CleanupMessage(10, 10);
                res = await assistantChatGPTConnection.RequestAsync();
                isBreak = true;

                foreach (var v in res.choices)
                {
                    AddContentCore("assistant", v.message.content);
                    stockUserMessageCount++;

                    Logger.Log($"Assistant : {v.message.content}");
                    Debug.Log($"Assistant : {v.message.content}");
                }

                isChatGPTSending = false;
            }
            catch (UnityWebRequestException exp)
            {
                Debug.Log("Err");
                Logger.Log($"Err : ChatGPTConecter._SendContentCoreAssistant UnityWebRequestException {exp.ToString()}");
            }
        }

        Logger.Log("<_SendContentCoreAssistant End>");
    }


    IEnumerator SpeakTest(string text, string pose)
    {
        isSpeaking = true;
        string[] tmps;
        {
            var tmp = "";
            foreach( var v in text)
            {
                switch(v)
                {
                    case '�B':
                        tmp += v + "\n";
                        break;
                    case '��':
                        tmp += v + "\n";
                        break;
                        tmp += v + "\n";
                    case '�H':
                        tmp += v + "\n";
                        break;
                    case '�I':
                        tmp += v + "\n";
                        break;
                    default:
                        tmp += v;
                        break;
                }
            }
            tmps = tmp.Split("\n");
        }

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
            }

        }
        isSpeaking = false;
    }

    void Update()
    {
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


        lock (CommentConecter.CommentPacksLock)
        {
            foreach (var v in CommentConecter.CommentPacks)
            {
                AddContentCore("user", v.message);
            }
            stockUserMessageCount += CommentConecter.CommentPacks.Count;
            CommentConecter.CommentPacks.Clear();
        }

        if (!isChatGPTSending && chatGPTDatas.Count > 0)
        {
            //stockUserMessageCount = 0;
            SendContent();
        }
        else if (!isChatGPTSending && assistantChatGPTDatas.Count > 0) 
        {
            if (speakDatas.Count <= 2) // �A�V�X�^���g�̐i�s��}���B�v���́A���b�����܂��Ă���Ƃ�
            {
                SendContentAsAssistant();
            }
        }

        {
            var text = $"AI:{isChatGPTSending} Speaking:{isSpeaking} StockAIMessage:{speakDatas.Count} StockSpeak:{speakDatas.Count}";
            InfomationText.text = text;
        }

        logger.Update();
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

    private void AddContentCoreAsAssistant(string role, string content)
    {
        lock (assistantChatGPTDatasLock)
        {
            if (assistantChatGPTDatas.Count == 0)
            {
                assistantChatGPTDatas.Add(new ChatGPTData());
            }
            assistantChatGPTDatas[assistantChatGPTDatas.Count - 1].chatGPTOneDatas.Add(new ChatGPTOneData() { role = role, content = content });
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
                _SendContentCoreStreamer();
            }
        }
    }

    public void SendContentAsAssistant()
    {
        lock (assistantChatGPTDatasLock)
        {
            if (assistantChatGPTDatas.Count > 0 && !isChatGPTSending)
            {
                var data = assistantChatGPTDatas[0];
                assistantChatGPTDatas.RemoveAt(0);
                foreach (var v in data.chatGPTOneDatas)
                {
                    assistantChatGPTConnection.AddMessage(v.role, v.content);
                }

                isChatGPTSending = true;
                _SendContentCoreAssistant();
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
