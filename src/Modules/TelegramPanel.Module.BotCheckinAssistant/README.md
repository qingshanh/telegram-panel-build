# Bot Check-in Assistant Module

This is an installable Telegram Panel module.

## Features

- Select multiple accounts and target one bot.
- Send scripted messages line by line.
- Capture bot reply text / button summary after each send.
- Manually choose `Continue Next Line` or `Stop` after each round.

## Build TPM Package

From repository root:

```powershell
powershell tools/package-module.ps1 `
  -Project "src/Modules/TelegramPanel.Module.BotCheckinAssistant/TelegramPanel.Module.BotCheckinAssistant.csproj" `
  -Manifest "src/Modules/TelegramPanel.Module.BotCheckinAssistant/manifest.json"
```

Default output:

- `artifacts/modules/community.bot-checkin-assistant-1.0.0.tpm`

## Install in Panel

1. Open `Modules` page in Telegram Panel.
2. Upload the generated `.tpm`.
3. Enable module and restart panel service.
4. Open page: `Operations -> Bot Check-in Assistant`.

