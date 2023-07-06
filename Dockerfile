FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY ./out .
ENTRYPOINT ["dotnet", "VintagestoryBugBot.dll"]
