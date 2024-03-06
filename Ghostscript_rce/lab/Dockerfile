FROM ubuntu:22.04

COPY flag /flag
COPY sources.list /etc/apt/sources.list
COPY ghostscript-10.01.0.tar.gz /opt/

EXPOSE 1337

RUN set -ex && \
    apt update && \
    apt -y install wget build-essential socat && \
    cd /opt/ && \
    tar xzvf ghostscript-10.01.0.tar.gz && \
    cd ./ghostscript-10.01.0 && \
    ./configure && \
    make && \
    make install && \
    chmod 400 /flag && \
    rm -rf /var/lib/apt/lists/*

CMD /usr/bin/socat TCP-LISTEN:1337,fork EXEC:'/usr/local/bin/gs -dSAFER'