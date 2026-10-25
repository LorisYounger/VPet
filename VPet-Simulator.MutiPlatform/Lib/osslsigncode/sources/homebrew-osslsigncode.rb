class Osslsigncode < Formula
  desc "OpenSSL based Authenticode signing for PE/MSI/Java CAB files"
  homepage "https://github.com/mtrojnar/osslsigncode"
  url "https://github.com/mtrojnar/osslsigncode/archive/refs/tags/2.14.tar.gz"
  sha256 "0f033fd6069387d2e489fbd2187e62f624764eb8c2758ee94e3e793e5150b5c5"
  license "GPL-3.0-or-later"

  depends_on "cmake" => :build
  depends_on "openssl@3"

  uses_from_macos "curl"
  uses_from_macos "python"

  on_linux do
    depends_on "zlib-ng-compat"
  end

  # Fix permission issue when installing bash completionn
  patch :DATA

  def install
    system "cmake", "-S", ".", "-B", "build", *std_cmake_args
    system "cmake", "--build", "build"
    system "cmake", "--install", "build"

    bash_completion.install "osslsigncode.bash" => "osslsigncode"
  end

  test do
    # Requires Windows PE executable as input, so we're just showing the version
    assert_match "osslsigncode", shell_output("#{bin}/osslsigncode --version")
  end
end

__END__
diff --git a/CMakeLists.txt b/CMakeLists.txt
index 2ffeb4e..7e2bc01 100644
--- a/CMakeLists.txt
+++ b/CMakeLists.txt
@@ -33,7 +33,6 @@ include(FindCURL)

 # load CMake project modules
 set(CMAKE_MODULE_PATH ${CMAKE_MODULE_PATH} "${PROJECT_SOURCE_DIR}/cmake")
-include(SetBashCompletion)
 include(FindHeaders)

 # define the target
