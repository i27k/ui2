param(
    [string]$Model = 'gpt-5.6-luna',
    [switch]$Machine
)

$ErrorActionPreference = 'Stop'

Write-Host 'c/Hub OpenAI setup' -ForegroundColor Red
Write-Host 'Create an API key at: https://platform.openai.com/api-keys'
Write-Host 'The key is stored as a Windows environment variable, never in the mod folder.'

$secureKey = Read-Host 'Paste the API key' -AsSecureString
$pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureKey)
try {
    $plainKey = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer)
    if ([string]::IsNullOrWhiteSpace($plainKey) -or -not $plainKey.StartsWith('sk-')) {
        throw 'The supplied value does not look like an OpenAI API key.'
    }

    $target = if ($Machine) { 'Machine' } else { 'User' }
    [Environment]::SetEnvironmentVariable('OPENAI_API_KEY', $plainKey, $target)
    [Environment]::SetEnvironmentVariable('CHUB_OPENAI_MODEL', $Model, $target)

    $headers = @{ Authorization = "Bearer $plainKey" }
    Invoke-RestMethod -Method Get -Uri 'https://api.openai.com/v1/models' -Headers $headers | Out-Null

    Write-Host "OpenAI authentication succeeded. Model: $Model" -ForegroundColor Green
    Write-Host 'Fully close and restart 7 Days to Die so it receives the new environment.' -ForegroundColor Yellow
}
finally {
    if ($pointer -ne [IntPtr]::Zero) {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer)
    }
    $plainKey = $null
}
