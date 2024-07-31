function Write-Header {
  param (
    [string]$message
  )

  Write-Host "`n--------------------------------------------------------------------------------" -ForegroundColor Cyan
  Write-Host $message -ForegroundColor Cyan
  Write-Host "--------------------------------------------------------------------------------`n" -ForegroundColor Cyan
}

function Write-SubHeader {
  param (
    [string]$message
  )

  Write-Host ""
  Write-Host $message -ForegroundColor Cyan
}

function Sync-PathVariable {
  # SourceRef: https://stackoverflow.com/questions/17794507/reload-the-path-in-powershell
  $env:Path = [System.Environment]::GetEnvironmentVariable("Path","Machine") +
              ";" +
              [System.Environment]::GetEnvironmentVariable("Path","User")
}
