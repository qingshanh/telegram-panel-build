# 机器人签到助手模块

这是一个可安装到 Telegram Panel 的模块。

## 功能说明

- 选择多个账号，统一给同一个机器人发消息。
- 按脚本逐行发送消息。
- 每轮发送后抓取机器人回复文本和按钮摘要。
- 用户看完返回内容后，可手动决定“继续下一条”或“停止”。

## 打包 TPM

在仓库根目录执行：

```powershell
powershell tools/package-module.ps1 `
  -Project "src/Modules/TelegramPanel.Module.BotCheckinAssistant/TelegramPanel.Module.BotCheckinAssistant.csproj" `
  -Manifest "src/Modules/TelegramPanel.Module.BotCheckinAssistant/manifest.json"
```

默认输出文件：

- `artifacts/modules/community.bot-checkin-assistant-1.0.1.tpm`

## 在面板中安装

1. 打开 Telegram Panel 的“模块”页面。
2. 上传生成好的 `.tpm` 文件。
3. 启用模块，并重启面板服务。
4. 进入“运营工具 -> 机器人签到助手”页面使用。
