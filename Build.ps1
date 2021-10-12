# This script originally (c) 2016 Serilog Contributors - license Apache 2.0

echo "build: Build started"

Push-Location $PSScriptRoot

if (Test-Path ./artifacts) {
	echo "build: Cleaning ./artifacts"
	Remove-Item ./artifacts -Force -Recurse
}

& dotnet restore --no-cache
if($LASTEXITCODE -ne 0) { exit 1 }

$ref = $env:GITHUB_REF ?? ""
$run = $env:GITHUB_RUN_NUMBER ?? "0"
$branch = @{ $true = $ref.Substring($ref.LastIndexOf("/") + 1); $false = $(git symbolic-ref --short -q HEAD) }[$ref -ne ""];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $run, 10); $false = "local" }[$run -ne "0"];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]

echo "build: Version suffix is $suffix"

foreach ($test in ls ./test) {
    Push-Location $test

    echo "build: Testing project in $test"

    & dotnet test -c Release
    if($LASTEXITCODE -ne 0) { exit 3 }

    Pop-Location
}

foreach ($src in ls ./src) {
    Push-Location $src

    echo "build: Packaging project in $src"

    if ($suffix) {
        & dotnet publish -c Release -o ./obj/publish --version-suffix=$suffix
        & dotnet pack -c Release -o ../../artifacts --no-build --version-suffix=$suffix
    } else {
        & dotnet publish -c Release -o ./obj/publish
        & dotnet pack -c Release -o ../../artifacts --no-build
    }
    if($LASTEXITCODE -ne 0) { exit 1 }    

    Pop-Location
}

Pop-Location
