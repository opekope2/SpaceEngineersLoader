#!/usr/bin/env python3
import sys
import subprocess

def replace_with_launcher(arg):
    if arg.endswith('SpaceEngineers.exe'):
        arg = arg[:-len('SpaceEngineers.exe')]
        return arg + 'Loader/SpaceEngineersLoader.exe'
    return arg

se_launcher_args = [replace_with_launcher(arg) for arg in sys.argv[1::]]

subprocess.call(se_launcher_args)
