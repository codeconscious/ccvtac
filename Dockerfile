FROM mcr.microsoft.com/dotnet/sdk:9.0

LABEL maintainer="your-email@example.com"

# Install Python 3 and curl.
RUN apt-get update && \
    apt-get install -y python3 curl && \
    rm -rf /var/lib/apt/lists/*

# Download the latest yt-dlp binary and set it as executable.
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
    -o /usr/local/bin/yt-dlp && \
    chmod a+rx /usr/local/bin/yt-dlp

# (Optional) Test installation by displaying versions.
CMD ["sh", "-c", "dotnet --version && python3 --version && yt-dlp --version"]
