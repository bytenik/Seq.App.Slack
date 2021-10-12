param($WebhookUrl)

echo "run: Publishing app binaries"

Push-Location 

& dotnet publish "$PSScriptRoot/src/Seq.App.Slack" -c Release -o "$PSScriptRoot/src/Seq.App.Slack/obj/publish" --version-suffix=local

if($LASTEXITCODE -ne 0) { exit 1 }    

echo "run: Piping live Seq logs to the app"

& seqcli tail --json | & seqcli app run -d "$PSScriptRoot/src/Seq.App.Slack/obj/publish" -p WebhookUrl="$WebhookUrl" 2>&1 | & seqcli print
