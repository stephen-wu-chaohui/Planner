param(
    [ValidateSet("RabbitMQ", "ServiceBus")]
    [string]$Transport = "RabbitMQ",

    [switch]$NoBuild,
    [switch]$ForceRecreate,
    [switch]$SkipDocker,
    [switch]$SkipBlazor,
    [switch]$DetachedBlazor,
    [switch]$WaitForBlazor,

    [int]$BlazorTimeoutSeconds = 90,

    [string[]]$Services = @()
)

$ErrorActionPreference = "Stop"

function Test-CommandAvailable {
    param([Parameter(Mandatory = $true)][string]$Name)

    $null -ne (Get-Command $Name -ErrorAction SilentlyContinue)
}

function Get-DockerComposeCommand {
    if (Test-CommandAvailable "docker") {
        & docker compose version *> $null
        if ($LASTEXITCODE -eq 0) {
            return @{
                File = "docker"
                Prefix = @("compose")
            }
        }
    }

    if (Test-CommandAvailable "docker-compose") {
        return @{
            File = "docker-compose"
            Prefix = @()
        }
    }

    throw "Neither 'docker compose' nor 'docker-compose' is available on PATH."
}

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$ArgumentList
    )

    Write-Host "> $FilePath $($ArgumentList -join ' ')"
    & $FilePath @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE."
    }
}

function Start-BlazorDetached {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$ProjectPath
    )

    $dotnetArgs = @(
        "run",
        "--project",
        $ProjectPath,
        "--launch-profile",
        "Planner.BlazorApp"
    )

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = "dotnet"
    $startInfo.WorkingDirectory = $RepoRoot
    $startInfo.UseShellExecute = $false

    foreach ($arg in $dotnetArgs) {
        [void]$startInfo.ArgumentList.Add($arg)
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Failed to start Planner.BlazorApp."
    }

    Write-Host "Planner.BlazorApp started in the background. PID: $($process.Id)"
    return $process
}

function Wait-ForBlazor {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    Write-Host "Waiting for Planner.BlazorApp at $Url..."

    do {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Write-Host "Planner.BlazorApp responded with HTTP $($response.StatusCode)."
                return
            }
        } catch {
            Start-Sleep -Seconds 2
        }
    } while ((Get-Date) -lt $deadline)

    throw "Planner.BlazorApp did not respond at $Url within $TimeoutSeconds seconds."
}

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$composeFile = Join-Path $repoRoot "docker-compose.yml"
$blazorProject = Join-Path $repoRoot "src\Planner.BlazorApp\Planner.BlazorApp.csproj"

Push-Location $repoRoot
try {
    if (-not (Test-Path $composeFile)) {
        throw "Cannot find docker-compose.yml at $composeFile."
    }

    if (-not (Test-Path $blazorProject)) {
        throw "Cannot find Planner.BlazorApp project at $blazorProject."
    }

    if (-not $SkipDocker) {
        $compose = Get-DockerComposeCommand

        if ($Transport -eq "ServiceBus") {
            $env:OPTIMIZATION_DISPATCH_MODE = "AzureServiceBus"
            $env:OPTIMIZATION_MESSAGING_TRANSPORT = "ServiceBus"
        } else {
            $env:OPTIMIZATION_DISPATCH_MODE = "RabbitMq"
            $env:OPTIMIZATION_MESSAGING_TRANSPORT = "RabbitMQ"
            Remove-Item Env:\COMPOSE_PROFILES -ErrorAction SilentlyContinue
        }

        $composeArgs = @()
        $composeArgs += $compose.Prefix
        $composeArgs += @("-f", $composeFile)

        if ($Transport -eq "ServiceBus") {
            $composeArgs += @("--profile", "azure-emulators")
        }

        $composeArgs += @("up", "-d")

        if (-not $NoBuild) {
            $composeArgs += "--build"
        }

        if ($ForceRecreate) {
            $composeArgs += "--force-recreate"
        }

        if ($Services.Count -gt 0) {
            $composeArgs += $Services
        }

        Write-Host ""
        Write-Host "Updating Planner Docker stack..."
        Write-Host "Transport: $Transport"
        if ($Services.Count -gt 0) {
            Write-Host "Services: $($Services -join ', ')"
        } else {
            Write-Host "Services: all active services in docker-compose.yml"
        }
        Write-Host ""

        Invoke-NativeCommand -FilePath $compose.File -ArgumentList $composeArgs

        $psArgs = @()
        $psArgs += $compose.Prefix
        $psArgs += @("-f", $composeFile)
        if ($Transport -eq "ServiceBus") {
            $psArgs += @("--profile", "azure-emulators")
        }
        $psArgs += "ps"

        Write-Host ""
        Invoke-NativeCommand -FilePath $compose.File -ArgumentList $psArgs
    }

    if ($SkipBlazor) {
        Write-Host ""
        Write-Host "Docker update complete. Skipped Planner.BlazorApp startup."
        exit 0
    }

    if (-not (Test-CommandAvailable "dotnet")) {
        throw "'dotnet' is not available on PATH."
    }

    Write-Host ""
    Write-Host "Starting Planner.BlazorApp..."
    Write-Host "UI:  https://localhost:7014"
    Write-Host "API: http://localhost:7085"
    Write-Host ""

    if ($DetachedBlazor) {
        $blazorProcess = Start-BlazorDetached -RepoRoot $repoRoot -ProjectPath $blazorProject
        if ($WaitForBlazor) {
            Wait-ForBlazor -Url "http://localhost:5212/dispatch-center" -TimeoutSeconds $BlazorTimeoutSeconds
        }
    } else {
        & dotnet run --project $blazorProject --launch-profile Planner.BlazorApp
        exit $LASTEXITCODE
    }
}
finally {
    Pop-Location
}
