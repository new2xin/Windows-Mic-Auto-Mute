# Windows Mic Auto Mute

Windowsの録音エンドポイントをCore Audio APIで監視し、設定に一致するマイクが認識されたら `SetMute(TRUE)` を送ります。特定のマイク専用ではなく、Windowsが録音エンドポイントとして認識する機器を対象にできます。

## 初回セットアップ

1. このフォルダで `build.bat` を実行します。.NET 8 SDKだけを使い、外部NuGetパッケージは追加しません。
2. `devices.json` の `nameContains` を実際の録音デバイス名に合わせます。この環境の設定例は `AKG C44-USB Microphone` 用で、初期値は `C44-USB` です。
3. `auto_mute_on.bat` を一度実行します。ログオン時タスクを登録し、監視を直ちに開始します。

`auto_mute_on.bat` は現在の認識済みデバイスにも即時に適用します。USB抜き差し、ハブ再接続、スリープ復帰などでエンドポイントが再列挙された場合も、最大約2秒以内に再適用します。

## BAT

- `auto_mute_on.bat`: 自動ミュート監視を有効化（PowerShell標準のログオン時タスクを登録）
- `auto_mute_off.bat`: タスクと監視プロセスを停止。ミュート状態は変更しません
- `mute_now.bat`: 設定に一致するマイクを明示的にミュート
- `unmute_now.bat`: 設定に一致するマイクのミュートを解除
- `status.bat`: 対象マイクの状態を表示

`auto_mute_off.bat` は「自動化を止める」操作です。停止後に赤ランプを消したい場合は、`unmute_now.bat` を実行してください。
監視が有効なまま `unmute_now.bat` を実行すると、次の監視周期で再びミュートされます。

## 別のマイクに差し替える

まず `status.bat` でWindowsが表示する正確な録音デバイス名とIDを確認します。その後、ルートの `devices.json` の条件を編集してください。次回起動、または `auto_mute_on.bat` の再実行で読み込まれます。

例えばTM-250Uだけを対象にする場合:

```json
{
  "pollIntervalMs": 2000,
  "targets": [
    {
      "nameContains": "TM-250U",
      "idContains": "",
      "enabled": true
    }
  ]
}
```

表示名が似た機器にも一致してしまう場合は、`status.bat` で取得したIDの固有部分を `idContains` に設定します。`devices.example.json` にC44-USBとTM-250Uの例があります。

## 確認と制限

- これはUSBの5V通電開始そのものを制御するものではありません。Windowsが録音エンドポイントとして認識した後にミュートします。
- ミュート処理は対象の録音エンドポイントだけに限定します。再生デバイスや他のマイクには作用しません。
- 監視は常駐しますが、処理は約2秒ごとの列挙だけで、状態が既にMUTEDなら再設定しません。
- 実機で赤ランプが点灯するか、再起動・抜き差し・スリープ復帰後に `status.bat` とランプの両方を確認してください。
- ログは `dist\logs\automute.log` に保存します。
- `dist` は.NET 8のフレームワーク依存EXEです。実行先にも.NET 8ランタイムが必要です。
