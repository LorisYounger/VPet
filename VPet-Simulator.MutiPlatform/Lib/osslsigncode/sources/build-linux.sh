#!/bin/sh
# Rebuild using the corresponding source archive and static development libs.
# Recorded build: Ubuntu 22.04, GCC 11.4, CMake 3.22.1;
# libssl-dev 3.0.2-0ubuntu1.30; zlib1g-dev 1:1.2.11.dfsg-2ubuntu9.2.
set -eu
tar -xzf osslsigncode-2.14.tar.gz
cmake -S osslsigncode-2.14 -B build -DCMAKE_BUILD_TYPE=Release \
  -DOPENSSL_USE_STATIC_LIBS=TRUE \
  -DZLIB_LIBRARY=/usr/lib/x86_64-linux-gnu/libz.a
cmake --build build --parallel
ldd build/osslsigncode
# The resulting binary must not depend on libssl.so, libcrypto.so or libz.so.
