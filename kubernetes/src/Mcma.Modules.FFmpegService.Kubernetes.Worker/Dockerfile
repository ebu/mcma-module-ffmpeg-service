FROM mcr.microsoft.com/dotnet/runtime:3.1

COPY bin/Release/netcoreapp3.1/linux-x64/publish/ app/
WORKDIR /app
ENTRYPOINT ["dotnet", "Mcma.Modules.FFmpegService.Kubernetes.Worker.dll"]