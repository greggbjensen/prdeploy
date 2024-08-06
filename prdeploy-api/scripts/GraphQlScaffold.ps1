param (
    [ValidateSet('all', 'update', 'build')]
    [string]
    $command =  'all'
)

. "$PSScriptRoot/_Supporting.ps1"

$repoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot/..")

if (($command -eq 'all') -or ($command -eq 'update')) {

    # SourceRef: https://chillicream.com/docs/strawberryshake/v12/get-started/console
    Write-Header "Updating GraphQL schema files"

    Push-Location "$repoRoot/test/PrDeploy.Api.Tests/Client"

    dotnet graphql update

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nMake sure the GraphQL server is started." -ForegroundColor Yellow
        Pop-Location

        Receive-Job $graphQlApi | Out-Null
        Stop-Job $graphQlApi
        exit -1
    }

    Pop-Location
}

if (($command -eq 'all') -or ($command -eq 'build')) {

    Write-Header "Building Tests to generate classes"

    Push-Location "$repoRoot/test/PrDeploy.Api.Tests"

    dotnet build .\PrDeploy.Api.Tests.csproj

    Pop-Location
}

Write-Host "`nGraphQL scaffold complete." -ForegroundColor Green
