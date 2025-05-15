#
#  util.py
#

import datetime
import io
import os
import signal
from time import sleep

debugLevel = 3


#  consistency checking a la assert()
class CheckException(Exception):

    def __init__(self, value):
        self.value = value

    def __str__(self):
        return str(self.value)


def check(condition, message):
    """
    Throw CheckException if condition is not fulfilled
    :param condition: The condition
    :param message: The message
    """
    if not condition:
        raise CheckException(message)


def fatal(msg):
    print()
    print(f"Fatal error: {msg}")
    finish(1)


def finish(status):
    print("", flush=True)
    print("", flush=True)
    sleep(0.2)
    exit(status)


def find_file_in_path(filename, search_path="$PATH"):
    """
    Searches for a file along a specified search path.

    Args:
        filename (str): The name of the file to search for.
        search_path (list of str): A list of directories to search within.
        search_path can be passed as single string with an environment variable
        example: "$PATH"

    Returns:
        str or None: The full path of the file if found, or None if not found.
    """
    if isinstance(search_path, str):
        if search_path.startswith("$"):
            s = os.getenv(search_path[1:])
            if s:
                search_path = s.split(os.pathsep)
            else:
                return None

    if not isinstance(search_path, list):
        RuntimeError(f"search_path '{search_path}' is no list")

    for directory in search_path:
        file_path = os.path.join(directory, filename)
        if os.path.isfile(file_path):
            return file_path

    return None


def handle_ctrl_break(_signal=None, frame=None):
    print()
    print("Ctrl+Break pressed!")
    print()
    finish(1)


def define_ctrl_break_handler(handler=handle_ctrl_break):
    signal.signal(signal.SIGBREAK, handler)


def path_add(new_directory, env_var='PATH'):
    current_path = os.environ.get(env_var, '')

    if current_path:
        new_path = current_path + os.pathsep + new_directory
    else:
        new_path = new_directory

    os.environ[env_var] = new_path


def pretty_num(i):
    if i < 0:
        return f"-{pretty_num(-1)}"
    if i < 1000:
        return str(i)
    return f"{pretty_num(i // 1000)},{str(i % 1000).rjust(3, '0')}"


outputFile = None
outputFileName = None


def open_output(name):
    global outputFile
    global outputFileName
    check(outputFile is None, "Output file already opened!")
    outputFile = io.open(name, "wt", encoding="ascii")
    outputFileName = outputFile.name


def close_output():
    if outputFile is None:
        check(False, "No output file opened!")
    else:
        outputFile.flush()
        outputFile.close()  # type: ignore[union-attr]  # Suppress warning
        void_output()


def output_file_name():
    check(outputFile is not None, "No output file opened!")
    return outputFileName


def void_output():
    global outputFile
    outputFile = None


def o(text=""):
    if outputFile is not None:
        outputFile.write(f"{text}\n")
        if debugLevel > 2:
            print(text, flush=True)
    else:
        print(text)


def set_debug_level(level):
    global debugLevel
    debugLevel = level


def show_environment():
    for key, value in os.environ.items():
        print(f'{key} = {value}')


def str2bool(v):
    if isinstance(v, bool):
        return v
    if v.lower() in ('yes', 'true', 't', 'y', '1', 'on'):
        return True
    elif v.lower() in ('no', 'false', 'f', 'n', '0', 'off'):
        return False
    else:
        raise ValueError(f'Boolean value expected: {v}')


def timestamp():
    now = datetime.datetime.now()
    mmm = [
        "Jan",
        "Feb",
        "Mar",
        "Apr",
        "May",
        "Jun",
        "Jul",
        "Aug",
        "Sep",
        "Oct",
        "Nov",
        "Dec",
    ][now.month - 1]
    return (
            f"{now.day:02}-{mmm}-{now.year} "
            + f"{now.hour:02}:{now.minute:02}:{now.second:02}"
    )


def trace(text):
    print(text)
    return True


class TimeReporter:

    # Initializing
    def __init__(self, purpose):
        self.watch = Watch()
        self.purpose = purpose
        print(f"{purpose} started {timestamp()}")

    # Deleting (Calling destructor)
    def __del__(self):
        print(f"{self.purpose} ended   {timestamp()}. {self.watch}")

    @property
    def elapsed(self):
        return str(self.watch)


#  for time measurement
class Watch:

    def __init__(self):
        self.millis = 0  # cf. restart()
        self.secondsStarted = 0.0
        self.restart()

    def __str__(self):
        elapsed = self.now - self.millis

        #  represent sub-second timespans in [ms]
        return (
            f"Elapsed {elapsed}ms" if elapsed < 1000 else f"Elapsed {round(elapsed / 1000.0, 1)}s"
        )

    @property
    def now(self):
        """milliseconds since object creation"""
        ts = datetime.datetime.now().timestamp()
        check(ts > 0, "Inconsistent timestamp")
        #  subtract start time to avoid int to become too big
        return int(round((ts - self.secondsStarted) * 1000))

    def restart(self):
        self.secondsStarted = datetime.datetime.now().timestamp()
        self.millis = self.now
