#!/usr/bin/env python3
import sys
import subprocess

args = sys.argv[1::]
se_arg = [(i, arg) for i, arg in enumerate(args)
          if arg.endswith('SpaceEngineers.exe')]
if not se_arg:
    exit(1)

args[se_arg[0][0]] = "Z:" + se_arg[0][1]
args.insert(
    se_arg[0][0],
    "/path/to/SpaceEngineersLoader/dist/Loader.exe"
)

subprocess.call(args)
