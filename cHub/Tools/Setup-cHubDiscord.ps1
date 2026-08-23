param(
    [switch]$Machine
)

$ErrorActionPreference = 'Stop'

Write-Host 'c/Hub Discord Bridge setup' -ForegroundColor Red
Write-Host 'Reset exposed tokens first: https://discord.com/developers/applications'
Write-Host 'The bot token is validated, then stored only as a Windows environment variable.'
Write-Host 'Use -Machine from an elevated PowerShell for a dedicated server service account.'

function Read-DiscordSnowflake {
    param([string]$Prompt)
    $value = (Read-Host $Prompt).Trim()

    # Discord Bot Maker fields or clipboard selections can accidentally contain
    # the same snowflake twice. Normalize only an exact duplicated pair; never
    # guess or truncate an otherwise invalid identifier.
    if ($value -match '^\d{34,40}$' -and ($value.Length % 2) -eq 0) {
        $halfLength = [int]($value.Length / 2)
        $firstHalf = $value.Substring(0, $halfLength)
        $secondHalf = $value.Substring($halfLength)
        if ($firstHalf -eq $secondHalf -and $firstHalf -match '^\d{17,20}$') {
            Write-Host "$Prompt was duplicated in the pasted value; using one copy: $firstHalf" -ForegroundColor Yellow
            $value = $firstHalf
        }
    }

    if ($value -notmatch '^\d{17,20}$') {
        throw "$Prompt must be one 17-20 digit Discord ID. Current length: $($value.Length)."
    }
    return $value
}

$secureToken = Read-Host 'Paste the NEW bot token' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
$plainToken = $null

try {
    $plainToken = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    if ([string]::IsNullOrWhiteSpace($plainToken) -or $plainToken.Length -lt 40) {
        throw 'The supplied value does not look like a Discord bot token.'
    }

    $channelId = Read-DiscordSnowflake 'Discord channel ID'
    $ownerId = Read-DiscordSnowflake 'Bot owner user ID'
    $clientId = Read-DiscordSnowflake 'Discord application/client ID'
    $headers = @{
        Authorization = "Bot $plainToken"
        'User-Agent' = 'DiscordBot (cHub, 0.0.3.0)'
    }

    Write-Host 'Validating bot authentication...' -ForegroundColor DarkGray
    $bot = Invoke-RestMethod -Method Get `
        -Uri 'https://discord.com/api/v10/users/@me' -Headers $headers

    if (-not $bot.bot) {
        throw 'Discord authenticated the token, but the account is not a bot.'
    }

    Write-Host 'Validating channel access...' -ForegroundColor DarkGray
    $channel = Invoke-RestMethod -Method Get `
        -Uri "https://discord.com/api/v10/channels/$channelId" -Headers $headers

    $target = if ($Machine) { 'Machine' } else { 'User' }
    [Environment]::SetEnvironmentVariable('CHUB_DISCORD_BOT_TOKEN', $plainToken, $target)
    [Environment]::SetEnvironmentVariable('CHUB_DISCORD_CHANNEL_ID', $channelId, $target)
    [Environment]::SetEnvironmentVariable('CHUB_DISCORD_OWNER_ID', $ownerId, $target)
    [Environment]::SetEnvironmentVariable('CHUB_DISCORD_CLIENT_ID', $clientId, $target)
    [Environment]::SetEnvironmentVariable('CHUB_DISCORD_ENABLED', 'true', $target)

    Write-Host "Discord authentication succeeded: $($bot.username)#$($bot.discriminator)" -ForegroundColor Green
    Write-Host "Channel access succeeded: $($channel.name) ($channelId)" -ForegroundColor Green
    Write-Host "Saved for Windows environment target: $target" -ForegroundColor Green
    Write-Host 'Fully restart the dedicated server so it receives the new environment.' -ForegroundColor Yellow
}
catch {
    Write-Host "Discord setup failed: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
finally {
    if ($pointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
    $plainToken = $null
    $secureToken = $null
}
