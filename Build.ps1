# This script originally (c) 2016 Serilog Contributors - license Apache 2.0

echo "build: Build started"

Push-Location $PSScriptRoot

if (Test-Path ./artifacts) {
	echo "build: Cleaning ./artifacts"
	Remove-Item ./artifacts -Force -Recurse
}

echo "build: Restoring"
& dotnet restore --no-cache
if($LASTEXITCODE -ne 0) { exit 1 }

$ref = $env:GITHUB_REF ?? ""
$run = $env:GITHUB_RUN_NUMBER ?? "0"
$branch = @{ $true = $ref.Substring($ref.LastIndexOf("/") + 1); $false = $(git symbolic-ref --short -q HEAD) }[$ref -ne ""];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $run, 10); $false = "local" }[$run -ne "0"];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]

echo "build: Version suffix is $suffix"

echo "build: Testing"
& dotnet test -c Release ./test/Seq.App.Slack.Tests/Seq.App.Slack.Tests.csproj
if ($LASTEXITCODE -ne 0) { exit 3 }

echo "build: Publishing and packing"
$src = "./src/Seq.App.Slack"
if ($suffix) {
    & dotnet publish -c Release -o "$src/obj/publish" --version-suffix=$suffix "$src/Seq.App.Slack.csproj"
    & dotnet pack -c Release -o ./artifacts --no-build --version-suffix=$suffix "$src/Seq.App.Slack.csproj"
} else {
    & dotnet publish -c Release -o "$src/obj/publish" "$src/Seq.App.Slack.csproj"
    & dotnet pack -c Release -o ./artifacts --no-build "$src/Seq.App.Slack.csproj"
}
if ($LASTEXITCODE -ne 0) { exit 1 }    

Pop-Location
