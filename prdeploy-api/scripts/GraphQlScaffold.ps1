param (
    [ValidateSet('all', 'update', 'build')]
    [string]
    $command =  'all'
)

. "$PSScriptRoot/_Supporting.ps1"

$repoRoot = [System.IO.Path]::GetFullPath("$PSScriptRoot/..")

if (($command -eq 'all') -or ($command -eq 'update')) {

    Write-Header "Starting API for GraphQL"

    $apiDirectory = "$repoRoot/src/PrDeploy.Api"

    $graphQlApi = Start-Job -Name "GraphQLScaffold" -ScriptBlock {
        param($directory)
        dotnet run --project "$directory/PrDeploy.Api.csproj"
    } -Arg $apiDirectory

    # Wait for GraphQL to start.
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    $isStarted = $false
    while (($stopwatch.TotalSeconds -lt 60) -and (-not $isStarted)) {
        Start-Sleep -Milliseconds 100
        $output = Receive-Job $graphQlApi
        $output
        $listening = $output | Select-String "Now listening on:"
        $isStarted = $listening -ne $null
    }

    $stopwatch.Stop()

    Pop-Location

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

    Write-Host "Stopping API."
    Receive-Job $graphQlApi | Out-Null  # Clean out buffer
    Stop-Job $graphQlApi
}

if (($command -eq 'all') -or ($command -eq 'build')) {

    Write-Header "Building Tests to generate classes"

    Push-Location "$repoRoot/test/PrDeploy.Api.Tests"

    dotnet build .\PrDeploy.Api.Tests.csproj

    Pop-Location
}

Write-Host "`nGraphQL scaffold complete." -ForegroundColor Green
