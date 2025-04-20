FROM mcr.microsoft.com/dotnet/sdk:9.0

# LABEL maintainer="your-email@example.com"

# Install external dependencies.
RUN apt-get update && \
    apt-get install -y python3 curl ffmpeg imagemagick libmagickwand-dev && \
    rm -rf /var/lib/apt/lists/*

# Download the latest yt-dlp binary and set it as executable.
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp \
    -o /usr/local/bin/yt-dlp && \
    chmod a+rx /usr/local/bin/yt-dlp

# (Optional) Test installation by displaying versions.
# CMD ["sh", "-c", "dotnet --version && python3 --version && yt-dlp --version"]

COPY src ./src
RUN mkdir -p /home/user/documents/
RUN mkdir -p /home/user/downloads
RUN mkdir -p /tmp/ccvtac
COPY docker-settings.json /home/user/documents/

# RUN useradd app
# USER app

# CMD ["sh", "-c", "dotnet run --project ./src/CCVTAC.Console --settings src/CCVTAC.Console/settings.json"]
CMD ["sh", "-c", "dotnet run --project ./src/CCVTAC.Console --settings /home/user/documents/docker-settings.json"]
