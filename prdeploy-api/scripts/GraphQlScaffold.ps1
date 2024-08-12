$repoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot/..")
$testDirectory = "$repoRoot/test/PrDeploy.Api.Tests"

Push-Location "$testDirectory/Client"

Write-Host "Updating schema in Test project."
dotnet graphql update
if ($LASTEXITCODE -ne 0) {
  Write-Host "Make sure you are running the API first." -ForegroundColor Yellow
  Pop-Location
  exit 1
}

Write-Host "Cleaning generated GraphQL classes."
Remove-Item "$testDirectory/obj/Debug/net6.0/berry" -Recurse -Force

Pop-Location

Write-Host "`nGraphQL scaffold complete." -ForegroundColor Green
