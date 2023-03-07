# 概要
 
![main_screen_1](Readme_Resource/top_img.png)
 
AITuberの基礎となる部分を開発しています

# 必要なもの

* Windows OS (11)
* Unity 2021.3.10f1
* ChatGPT APIKey
* VOICEVOX
 
# 導入手順

## 設定ファイルの作成

Project/AITuberBody/Data/config.txt を作成してください。ひな型となるサンプル(config_sammple.txt)が入っています。

```
OpenAI-APIKey sk-XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
VOICEVOX C:\user\app\VOICEVOX\VOICEVOX\VOICEVOX.exe
```

こちらに、ChatGPT(OpenAI)のAPIKeyと、VOICEVOXの実行ファイルパスを設定します。

# 使い方

## 簡単な操作(1) ChatGPTへの通信と返答をもとに発話
+ アプリを起動し、VOICEVOXが起動するのを待ちます
+ Userの入力ボックスに、視聴者のコメントに相当するものを入力します
+ MessageAddボタンを押します
+ Sendボタンを押します
+ すると、Sendボタン下に表示されているAIの横がTrueになります、これがFalseになるのを待ちます
+ つづいて、Speakingの横がTrueになると、発話が開始されます

## あらかじめ設定したい
+ Project/AITuberBody/Assets/ChatGPTConecter.cs のchatGPTConnection.AddMessage("system", system_content) で初期設定をしています
+ そのため system_content の内容を編集してください

## User、Assistant、Systemとかの仕組みについて
ChatGPTのAPIは、メッセージに3つの役割があります。それが、User、Assistant、Systemの３つです。
メッセージごとに、どの役割かを設定できるので、ます、テキスト入力が3つあります。

また、MessageAddボタンは、上記のメッセージを蓄積します。そして、Sendボタンで一度にまとめてChatGPTに送信しています。
 
# 今後の展望

+ ライブ配信中のYoutubeのコメントの取得
+ ライブ配信で、何かしらお題を1つ、コメントとの相互のやりとりをしながら話ができるようにする

# キャラクターについて

デジッ娘・ミリーちゃんです。
設定:バーチャルデジタル東西南北都高校に在学中です、性格はちょっとおっちょこちょいです

AveterShopというソフトで作成しました。
テストなど、遊んでいただいても問題ありません。


# Note

私が制作した範囲については、MITライセンスです。
その他の各ライセンスについては、アップロード可能なものを利用しています。

+ 3Dアバターは、AvaterShop(https://booth.pm/ja/items/3787505)により作成したVRMファイルを利用しました。
+ フォントは、M+フォントです。
+ リップシンクは、uLipSyncを利用しています。

# Author

えむげん