using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net.Http;
using Cysharp.Threading.Tasks;

public class CommentConecter : MonoBehaviour
{
    [SerializeField]
    private TMPro.TMP_InputField ChannelIDInputField;

    string channelID;

    string continuation;
    string appInstallData = "";
    string apiKey = "";
    string innertubeApiKey = "";

    private enum Step
    {
        Start,
        ReadingOK
    }
    Step step = Step.Start;

    HttpClient client;

    public class CommentPack
    {
        public string message;
        public string authorName;
        public string authorPhotoURL;
    }

    public List<CommentPack> CommentPacks = new List<CommentPack>();
    public object CommentPacksLock = new object();

    void Start()
    {
    }

    void Update()
    {
        
    }

    public void RunByButton()
    {
        Run(ChannelIDInputField.text);
    }

    public void Run(string channelID)
    {
        this.channelID = channelID;
        StartCoroutine(_Run_Core());
    }

    IEnumerator _Run_Core()
    {
        Debug.Log("_Run_Core");

        yield return new WaitForSeconds(0);

        client = new HttpClient();

        {
            client.DefaultRequestHeaders.Add(
               "User-Agent",
               "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:103.0) Gecko/20100101 Firefox/103.0");
            client.DefaultRequestHeaders.Add("Accept-Language", "ja-JP");

            var htmlTask = client.GetStringAsync($"https://www.youtube.com/channel/{channelID}/live");

            while ( !htmlTask.IsCompleted )
            {
                yield return new WaitForSeconds(0.1f);
            }

            var htmlString = htmlTask.Result;
            var scriptCodes = GetScriptCode(htmlString);

            foreach (var v in scriptCodes)
            {
                if (v.IndexOf("appInstallData") >= 0)
                {
                    appInstallData = GetIDByKeySimple(v, "appInstallData");
                }

                if (v.IndexOf("apiKey") >= 0)
                {
                    apiKey = GetIDByKeySimple(v, "apiKey");
                }

                if (v.IndexOf("\"continuation\"") >= 0)
                {
                    continuation = GetIDByKeySimple(v, "\"continuation\"");
                }
                if (v.IndexOf("\"innertubeApiKey\"") >= 0)
                {
                    innertubeApiKey = GetIDByKeySimple(v, "\"innertubeApiKey\"");
                }
            }

            step = Step.ReadingOK;

            Debug.Log($"_Run_Core : innertubeApiKey : {innertubeApiKey}");
        }

        while (true)
        {

            var responseText = "";
            var commentPacks = new List<CommentPack>();

            // 通信して情報を取得
            {
                var url = ($"https://www.youtube.com/youtubei/v1/live_chat/get_live_chat?key={innertubeApiKey}");
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("ContentType", "application/json");

                var text = "";
                text += "{\n";
                text += "\tcontext: {\n";
                text += "\t\tclient: {\n";
                text += "\t\t\tclientName: 'WEB',\n";
                text += "\t\t\tclientVersion: '2.20210126.08.02',\n";
                text += "\t\t\ttimeZone: 'Asia/Tokyo',\n";
                text += "\t\t\tutcOffsetMinutes: 540,\n";
                text += "\t\t\tmainAppWebInfo: {\n";
                text += "\t\t\t\tgraftUrl: 'https://www.youtube.com/live_chat?continuation=',\n";
                text += "\t\t\t},\n";
                text += "\t\t},\n";
                text += "\t\trequest: {\n";
                text += "\t\t\tuseSsl: true,\n";
                text += "\t\t},\n";
                text += "\t},\n";
                text += $"\tcontinuation: \"{continuation}\",\n";
                text += "}\n";
                request.Content = new StringContent(text, System.Text.Encoding.UTF8);

                var responseTask = client.SendAsync(request);

                while (!responseTask.IsCompleted)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                var response = responseTask.Result;

                var responseTextTask = response.Content.ReadAsStringAsync();
                while (!responseTextTask.IsCompleted)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                responseText = responseTextTask.Result;

            }

            // JSONの解析
            {
                var json = new Json.JsonAnalyzer(responseText);

                //
                {
                    var continuations = json.body.GetNode("continuationContents", "liveChatContinuation", "continuations");

                    if (continuations != null)
                    {
                        var num = continuations.Count();
                        for (var i = 0; i < num; i++)
                        {
                            var v = continuations.GetNode(i);
                            var a = v.GetNode("invalidationContinuationData", "continuation");
                            var b = v.GetNode("timedContinuationData", "continuation");

                            if (a != null) continuation = (string)a.value;
                            if (b != null) continuation = (string)b.value;
                        }
                    }
                }

                //
                {
                    var actions = json.body.GetNode("continuationContents", "liveChatContinuation", "actions");
                    if (actions == null) continue;

                    var num = actions.Count();
                    for (var i = 0; i < num; i++)
                    {
                        var action = actions.GetNode(i).GetNode("addChatItemAction");
                        if (action == null) continue;
                        var item = action.GetNode("item");
                        if (item == null) continue;

                        var liveChatTextMessageRenderer = item.GetNode("liveChatTextMessageRenderer");
                        if (liveChatTextMessageRenderer != null)
                        {
                            var pack = new CommentPack();
                            pack.message = "";

                            var messageRuns = liveChatTextMessageRenderer.GetNode("message", "runs");
                            for (var j = 0; j < messageRuns.Count(); j++)
                            {
                                var tmp = messageRuns.GetNode(j).GetNode("text");
                                if (tmp != null)
                                {
                                    pack.message += (string)(tmp.value);
                                }
                            }

                            pack.authorName = (string)liveChatTextMessageRenderer.GetNode("authorName", "simpleText").value;

                            var thumbnails = liveChatTextMessageRenderer.GetNode("authorPhoto", "thumbnails");
                            var size = 0;
                            for (var j = 0; j < thumbnails.Count(); j++)
                            {
                                var tmp = thumbnails.GetNode(j);
                                var tmpSize = (System.Int64)tmp.GetNode("width").value;
                                var tmpURL = (string)tmp.GetNode("url").value;

                                if (size < tmpSize)
                                {
                                    pack.authorPhotoURL = tmpURL;
                                }
                            }

                            var timestampUsec = liveChatTextMessageRenderer.GetNode("timestampUsec"); // todo : 変換方法がわからん、時刻だと思われるが

                            commentPacks.Add(pack);
                        }


                    }
                }

                // 解析結果から、コメントを生成
                foreach (var v in commentPacks)
                {
                    Debug.Log($"_Run_Core : GetComment : {v.message} ");

                    lock (CommentPacksLock) {
                        CommentPacks.Add(new CommentPack() { message = v.message });
                    }
                }
            }

            yield return new WaitForSeconds(0.1f);
        }

    }


    static string[] GetScriptCode(string text)
    {
        List<string> items = new List<string>();

        var indexStart = 0;

        while (true)
        {
            var start = text.IndexOf("<script", indexStart);
            if (start < 0) break;
            var startB = text.IndexOf(">", start + 1);
            var end = text.IndexOf("</script>", startB);
            var scriptText = text.Substring(startB + 1, end - startB - 1);

            if (scriptText.Length > 0)
            {
                items.Add(scriptText);
            }
            indexStart = end;
        }

        return items.ToArray();
    }

    static string GetIDByKeySimple(string v, string key)
    {
        var start = v.IndexOf(key);
        var next = v.IndexOf(":", start);
        var p1 = next = v.IndexOf("\"", next + 1);
        var p2 = next = v.IndexOf("\"", next + 1);
        var tmp = v.Substring(p1 + 1, p2 - p1 - 1);
        return tmp;
    }

}
