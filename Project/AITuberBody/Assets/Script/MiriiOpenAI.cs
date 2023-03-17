using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;

using Cysharp.Threading.Tasks;

namespace Mirii
{
    [Serializable]
    public class ChatGPTMessageModel
    {
        public string role;
        public string content;
    }

    //ChatGPT API��Request�𑗂邽�߂�JSON�p�N���X
    [Serializable]
    public class ChatGPTCompletionRequestModel
    {
        public string model;
        public List<ChatGPTMessageModel> messages;
    }

    //ChatGPT API�����Response���󂯎�邽�߂̃N���X
    [System.Serializable]
    public class ChatGPTResponseModel
    {
        public string id;
        public string @object;
        public int created;
        public Choice[] choices;
        public Usage usage;

        [System.Serializable]
        public class Choice
        {
            public int index;
            public ChatGPTMessageModel message;
            public string finish_reason;
        }

        [System.Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }
    }

    public class ChatGPTClient
    {
        private readonly string _apiKey;
        //��b������ێ����郊�X�g
        private readonly List<ChatGPTMessageModel> _messageList = new();

        public ChatGPTClient(string apiKey)
        {
            _apiKey = apiKey;
        }

        public void AddMessage( string role, string content )
        {
            _messageList.Add(new ChatGPTMessageModel { role = role, content = content });
        }

        // ���܂����R�����g�Ȃǂ̃f�[�^���ꕔ�폜���N�����i�b�v���鏈��
        public void CleanupMessage( int assistantLeaveCount, int userLeaveCount)
        {
            var system = new List<ChatGPTMessageModel>();
            var assystant = new List<ChatGPTMessageModel>();
            var usercomment = new List<ChatGPTMessageModel>();
            foreach( var v in _messageList)
            {
                if (v.role == "system")
                {
                    system.Add(v);
                } else if (v.role == "assistant") {
                    assystant.Add(v);
                } else if (v.role == "user") {
                    usercomment.Add(v);
                }
            }

            if (assystant.Count > 0)
            {
                while (assystant.Count >= assistantLeaveCount )
                {
                    if (assystant.Count == 0) break;
                    assystant.RemoveAt(0);
                }
            }

            if (usercomment.Count > 0)
            {
                while (usercomment.Count >= userLeaveCount)
                {
                    if (usercomment.Count == 0) break;
                    usercomment.RemoveAt(0);
                }
            }

            _messageList.Clear();
            Debug.Log($"CleanupMessage Stat {system.Count} {assystant.Count} {usercomment.Count}");
            foreach ( var v in system)
            {
                _messageList.Add(v);
                Debug.Log("system:"+v.content);
            }
            foreach (var v in assystant)
            {
                _messageList.Add(v);
                Debug.Log("assystant:" + v.content);
            }
            foreach (var v in usercomment)
            {
                _messageList.Add(v);
                Debug.Log("usercomment:" + v.content);
            }
            Debug.Log("CleanupMessage End");
        }

        public async UniTask<ChatGPTResponseModel> RequestAsync()
        {
            //���͐���AI��API�̃G���h�|�C���g��ݒ�
            var apiUrl = "https://api.openai.com/v1/chat/completions";

            //OpenAI��API���N�G�X�g�ɕK�v�ȃw�b�_�[����ݒ�
            var headers = new Dictionary<string, string>
            {
                {"Authorization", "Bearer " + _apiKey},
                {"Content-type", "application/json"},
                {"X-Slack-No-Retry", "1"}
            };

            //���͐����ŗ��p���郂�f����g�[�N������A�v�����v�g���I�v�V�����ɐݒ�
            var options = new ChatGPTCompletionRequestModel()
            {
                model = "gpt-3.5-turbo",
                messages = _messageList
            };
            var jsonOptions = JsonUtility.ToJson(options);

            //OpenAI�̕��͐���(Completion)��API���N�G�X�g�𑗂�A���ʂ�ϐ��Ɋi�[
            using var request = new UnityWebRequest(apiUrl, "POST")
            {
                uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonOptions)),
                downloadHandler = new DownloadHandlerBuffer()
            };

            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(request.error);
                throw new Exception();
            }
            else
            {
                var responseString = request.downloadHandler.text;
                var responseObject = JsonUtility.FromJson<ChatGPTResponseModel>(responseString);
                Debug.Log("ChatGPT:" + responseObject.choices[0].message.content);
                _messageList.Add(responseObject.choices[0].message);
                return responseObject;
            }
        }
    }
}
