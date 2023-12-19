FROM ubuntu:latest

# Don't prompt for time zone
ENV DEBIAN_FRONTEND=noninteractive

# Set up Mono's APT repo
RUN apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates gnupg \
    && gpg --homedir /tmp --no-default-keyring --keyring /usr/share/keyrings/mono-official-archive-keyring.gpg --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF \
    && echo "deb [signed-by=/usr/share/keyrings/mono-official-archive-keyring.gpg] https://download.mono-project.com/repo/ubuntu stable-focal main" | tee /etc/apt/sources.list.d/mono-official-stable.list \
    && apt-get update

# Install the necessary pieces of Mono
RUN apt-get install -y --no-install-recommends \
    mono-runtime ca-certificates-mono libmono-microsoft-csharp4.0-cil libmono-system-data4.0-cil libmono-system-runtime-serialization4.0-cil libmono-system-transactions4.0-cil libmono-system-net-http-webrequest4.0-cil

ADD KSPMMCfgValidator.exe /usr/local/bin/.
ENTRYPOINT ["mono", "/usr/local/bin/KSPMMCfgValidator.exe"]
