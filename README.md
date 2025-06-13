# VRC-PhotoBridge

VRChatで写真を撮影した際に必要に応じてImagePadで表示出来るサイズ(2048)にリサイズしImgurにアップロード、Imgurのリンクをクリップボードにコピーするコンソールソフトウェアです。  
実行には.NET 9.0 Runtimeが必要です。[.NET 9.0.6 Windows Runtime](https://dotnet.microsoft.com/ja-jp/download/dotnet/thank-you/runtime-9.0.6-windows-x64-installer)
![image](https://github.com/user-attachments/assets/2ba2f01d-ad74-43a6-8437-5c9932815add)

仕様上解像度が2048x2048を超えるファイルはリサイズされた画像で上書きされます。  
また、デフォルトのスクショフォルダ(Pictures/VRChat)以外にも対応はしていますが実装が面倒だったので記憶はしません。  
画像の形式はpngのみ対応です。
