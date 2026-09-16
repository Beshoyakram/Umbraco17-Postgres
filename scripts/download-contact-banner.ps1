$ErrorActionPreference = 'Continue'
$destDir = Join-Path $PSScriptRoot '..\wwwroot\media\contactus'
$destPath = Join-Path $destDir 'banner.png'
New-Item -ItemType Directory -Force -Path $destDir | Out-Null

$candidates = @(
    'https://centrocdx.com/wp-content/themes/centrotheme/img/Free-Consultation%202.png',
    'https://www.centrocdx.com/wp-content/themes/centrotheme/img/Free-Consultation%202.png',
    'https://centrocdx.com/wp-content/themes/centrotheme/img/Free-Consultation 2.png',
    'https://web.archive.org/web/2024/https://centrocdx.com/wp-content/themes/centrotheme/img/Free-Consultation%202.png',
    'https://web.archive.org/web/2022/https://centrocdx.com/wp-content/themes/centrotheme/img/Free-Consultation%202.png',
    'https://web.archive.org/web/2021/https://centrocdx.com/wp-content/themes/centrotheme/img/Free-Consultation%202.png'
)

foreach ($url in $candidates) {
    Write-Host "Trying: $url"
    try {
        $response = Invoke-WebRequest -Uri $url -UseBasicParsing -MaximumRedirection 10 -TimeoutSec 60
        $ct = $response.Headers['Content-Type']
        Write-Host "  Status: $($response.StatusCode) Content-Type: $ct Length: $($response.RawContentLength)"
        if ($response.StatusCode -eq 200 -and $ct -match 'image') {
            [System.IO.File]::WriteAllBytes($destPath, $response.Content)
            Write-Host "SUCCESS: $url -> $destPath"
            exit 0
        }
    }
    catch {
        Write-Host "  Failed: $($_.Exception.Message)"
    }
}

Write-Host 'All candidates failed'
exit 1
