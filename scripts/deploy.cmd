@echo off

echo Building project...
dotnet publish -c Release -o publish

echo Zipping package...
powershell Compress-Archive -Path publish\* -DestinationPath publish.zip -Force

echo Deploying to Azure...
az functionapp deployment source config-zip ^
 --resource-group tekni-rg ^
 --name tekni-api ^
 --src publish.zip

echo Done
pause