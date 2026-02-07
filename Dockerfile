FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /source

COPY src/Nadeko.Medusa/*.csproj src/Nadeko.Medusa/
COPY src/NadekoBot/*.csproj src/NadekoBot/
COPY src/NadekoBot.Coordinator/*.csproj src/NadekoBot.Coordinator/
COPY src/NadekoBot.Generators/*.csproj src/NadekoBot.Generators/
COPY src/NadekoBot.Voice/*.csproj src/NadekoBot.Voice/
COPY src/NadekoBot.GrpcApiBase/*.csproj src/NadekoBot.GrpcApiBase/

RUN DOTNET_RID="linux-musl-$([ "$TARGETARCH" = "arm64" ] && echo "arm64" || echo "x64")" \
    && echo "$DOTNET_RID" > /tmp/rid
RUN dotnet restore src/NadekoBot/ -r $(cat /tmp/rid)

COPY . .
WORKDIR /source/src/NadekoBot

RUN dotnet publish -c Release -o /app --self-contained -r $(cat /tmp/rid) --no-restore \
    && mv /app/data /app/data_init \
    && chmod +x /app/NadekoBot

FROM alpine:3.20
ARG TARGETARCH
WORKDIR /app

RUN apk add --no-cache ffmpeg libsodium opus yt-dlp

RUN apk add --no-cache libstdc++ libgcc icu-libs libc6-compat tzdata

COPY --from=build /app ./
COPY docker-entrypoint.sh /usr/local/sbin/

RUN rm -f /app/data_init/lib/libsodium.so /app/data_init/lib/opus.so \
    && ln -sf /usr/lib/libsodium.so.26 /app/data_init/lib/libsodium.so \
    && ln -sf /usr/lib/libopus.so.0 /app/data_init/lib/opus.so

VOLUME [ "/app/data" ]

ENTRYPOINT [ "/usr/local/sbin/docker-entrypoint.sh" ]
CMD [ "./NadekoBot" ]
