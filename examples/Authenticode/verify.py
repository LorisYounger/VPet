"""用 osslsigncode 验证 Authenticode；仅退出码 0 表示成功，不改变系统信任库。"""

import argparse
from pathlib import Path
import re
import shutil
import subprocess
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("file", type=Path, help="要验证的 DLL/EXE")
    parser.add_argument("--ca-file", required=True, type=Path, help="可信代码签名根证书 PEM")
    parser.add_argument("--tsa-ca-file", type=Path, help="可信时间戳根证书 PEM；默认与 --ca-file 相同")
    parser.add_argument("--osslsigncode", default="osslsigncode", help="工具名称或路径")
    parser.add_argument("--timeout", type=float, default=45, help="验证超时秒数，默认 45")
    args = parser.parse_args()
    if not 0 < args.timeout <= 300:
        parser.error("--timeout 必须大于 0 且不超过 300 秒")
    paths = [args.file, args.ca_file, args.tsa_ca_file or args.ca_file]
    for path in paths:
        if not path.is_file():
            parser.error(f"文件不存在：{path}")
    executable = shutil.which(args.osslsigncode)
    if executable is None:
        parser.error("找不到 osslsigncode；请安装 2.13 或更新版本，或指定 --osslsigncode")
    try:
        version = subprocess.run(
            [executable, "--version"], capture_output=True, text=True, timeout=10
        )
        match = re.search(r"^osslsigncode (\d+)\.(\d+)\b", version.stdout)
        if version.returncode != 0 or not match or tuple(map(int, match.groups())) < (2, 13):
            print("拒绝验证：需要 osslsigncode 2.13 或更新版本（包含 PE 验证安全修复）。", file=sys.stderr)
            return 2
        # 不调用 shell；不使用 -ignore-timestamp、-ignore-crl 或 -time 绕过验证。
        result = subprocess.run(
            [executable, "verify", "-CAfile", str(paths[1].resolve()),
             "-TSA-CAfile", str(paths[2].resolve()), "-in", str(paths[0].resolve())],
            timeout=args.timeout,
        )
        return 0 if result.returncode == 0 else 1
    except subprocess.TimeoutExpired:
        print("验证超时，未获得签名信任。", file=sys.stderr)
        return 2
    except OSError as error:
        print(f"无法运行验证工具：{error}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    sys.exit(main())
