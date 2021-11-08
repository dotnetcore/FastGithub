set output=./publish
if exist "%output%" rd /S /Q "%output%"
dotnet publish -c Release -o "%output%/fastgithub_win-x64" ./FastGithub.UI/FastGithub.UI.csproj
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r win-x64 -o "%output%/fastgithub_win-x64" ./FastGithub/FastGithub.csproj
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r linux-x64 -o "%output%/fastgithub_linux-x64" ./FastGithub/FastGithub.csproj
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r linux-arm64 -o "%output%/fastgithub_linux-arm64" ./FastGithub/FastGithub.csproj
dotnet publish -c Release /p:PublishSingleFile=true /p:PublishTrimmed=true --self-contained -r osx-x64 -o "%output%/fastgithub_osx-x64" ./FastGithub/FastGithub.csproj