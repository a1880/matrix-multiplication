#
#  util.py  -  helper functions and classes
#

import datetime
import io
import json
import numpy as np
import os
import re
import signal
from time import sleep

""" don't change this variable directly from outside """
debugLevel = 0

debugLevel_stack: list[int] = []


#  consistency checking a la assert()
class CheckException(Exception):

    def __init__(self, value):
        self.value = value

    def __str__(self):
        return str(self.value)


def check(condition: bool, message: str) -> None:
    """
    Throw CheckException if condition is not fulfilled
    :param condition: The condition
    :param message: The message
    """
    if not condition:
        raise CheckException(message)


def exists(file_name: str) -> bool:
    """Returns true iff fileName belongs to an existing file"""
    return os.path.exists(file_name) and os.path.isfile(file_name)


def exists_dir(dir_name: str) -> bool:
    """ Returns true iff dir_name belongs to an existing directory """
    return os.path.exists(dir_name) and os.path.isdir(dir_name)


def fatal(msg: str) -> None:
    """ grumble and die with status 1 """
    print()
    print(f"Fatal error: {msg}")
    finish(1)


def find(value_list: list, value) -> int:
    """Search list for value; return index iff found, -1 otherwise"""
    if value in value_list:
        return value_list.index(value)
    else:
        return -1


def finish(status: int) -> None:
    """ exit program and return status """
    print("", flush=True)
    print("", flush=True)
    sleep(0.2)
    """ using os._exit() to avoid stack trace for status 1 """
    os._exit(status)


def find_file_in_path(filename: str, search_path: str="$PATH") -> (str | None):
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


def get_seconds_since_midnight() -> int:
    now = datetime.now()
    midnight = now.replace(hour=0, minute=0, second=0, microsecond=0)
    seconds_since_midnight = (now - midnight).seconds
    return seconds_since_midnight


def handle_ctrl_break(_signal=None, frame=None) -> None:
    del _signal
    del frame

    print()
    print("Ctrl+Break pressed!")
    print()
    finish(1)


def define_ctrl_break_handler(handler=handle_ctrl_break):
    signal.signal(signal.SIGBREAK, handler)

def enum_to_int(value):
    """ Convert an IntEnum member to a numeric value.
    If it's not an IntEnum member return the value itself.
    """
    try:
        return int(value)
    except (ValueError, TypeError):
        return value

def load_json_with_trailing_commas(json_file_path: str) -> None:
    # https://github.com/CidQu/JSON-remove-trailing/blob/main/json_rtc.py    
    with open(json_file_path, 'r', encoding='ascii') as json_file:
        data = json_file.read()

    regex = r',(?!\s*?[\{\[\"\'\w])'
    data_fixed = re.sub(regex, '', data)

    data_parsed = json.loads(data_fixed)

    return data_parsed

def is_integer(s: str) -> bool:
    """ return True, iff string is a signed or unsigned integer """
    if s:
        if s[0] in {"+", "-"}:
            return s[1:].isdigit()     
        return s.isdigit()
    return False

def load_json_line(json_file_path: str, key: str) -> None:
    """ open json file and parse+return the first line with key """
    data_parsed = None
    with open(json_file_path, 'r', encoding='ascii') as json_file:
        for line in json_file:
            if line.find(key) >= 0:
                data_parsed = json.loads(line)
                break
    return data_parsed

def mkdir(directory_name: str) -> None:
    """ create the directory """
    try:
        os.mkdir(directory_name)
        o4(f"mkdir: Directory '{directory_name}' created successfully.")
    except FileExistsError:
        o4(f"mkdir: Directory '{directory_name}' already exists.")
    except PermissionError:
        fatal(f"Permission denied: Unable to create '{directory_name}'.")
    except Exception as e:
        fatal(f"mkdir: An error occurred: {e}")


def path_add(new_directory: str, env_var: str='PATH') -> None:
    """ add a directory to the env_var path """
    current_path = os.environ.get(env_var, '')

    if current_path:
        new_path = current_path + os.pathsep + new_directory
    else:
        new_path = new_directory

    os.environ[env_var] = new_path


def pretty_num(i: int) -> str:
    """ return number i as formatted string with thousands separator(s) """
    if i < 0:
        return f"-{pretty_num(-1)}"
    if i < 1000:
        return str(i)
    return f"{pretty_num(i // 1000)},{str(i % 1000).rjust(3, '0')}"


outputFile: io.TextIOWrapper = None
outputFileName: str = None


def open_output(name: str) -> None:
    """ open file 'name' for output """
    global outputFile
    global outputFileName
    check(outputFile is None, "Output file already opened!")
    outputFile = io.open(name, "wt", encoding="ascii")
    outputFileName = outputFile.name


def close_output() -> None:
    """ close output file previously opened by open_output() """
    if outputFile is None:
        raise RuntimeError("close_output(): No output file opened!")
    else:
        outputFile.flush()
        outputFile.close()  # type: ignore[union-attr]  # Suppress warning
        void_output()


def output_file_name() -> str:
    """  return name of curren t output file """
    check(outputFile is not None, "No output file opened!")
    return outputFileName


def void_output() -> None:
    """ set output file handle to None """
    global outputFile
    outputFile = None


def o4(text: str="") -> None:
    """ output text, if debugLevel is 4 or higher """
    if debugLevel > 3:
        o(text)


def o(text=""):
    """ output text; if outputFile is open, write text also to file """
    if outputFile is not None:
        outputFile.write(f"{text}\n")
        if debugLevel > 2:
            print(text, flush=True)
    else:
        print(text)


def print_array(arr) -> None:
    """  print array  """
    if arr is None:
        o("none")
        return
    if type(arr) is np.ndarray:
        print_array(arr.tolist())
        return
    if not (type(arr) is list):
        print(arr)
        return
    if len(arr) == 0:
        print("empty list")
        return
    if not (type(arr[0]) in {list, np.ndarray}):
        print_array([arr])
        return
    for row in arr:
        s = ""
        for element in row:
            if element == 0:
                s = s + ".".rjust(1)
            else:
                s = s + str(element).rjust(1)
        o(s)
            

def get_debug_level() -> int:
    """ return current debugLevel """
    return debugLevel

def set_debug_level(level):
    """ change current debugLevel  """
    global debugLevel
    o(f"Changing debugLevel from {debugLevel} to {level}")
    debugLevel = level

def push_debug_level(level: int) -> None:
    """ save current debugLevel to stack and set debugLevel to level """
    debugLevel_stack.append(debugLevel)
    set_debug_level(level)

def pop_debug_level() -> None:
    """ restore current debugLevel from stack """
    check(debugLevel_stack, "Cannot pop from empty debugLevel stack")
    level = debugLevel_stack.pop()
    set_debug_level(level)

def show_environment():
    """ list all environemt variables with name and value """
    for key, value in os.environ.items():
        print(f'{key} = {value}')

def str2bool(v):
    """ convert a string to a Boolean value """
    if isinstance(v, bool):
        return v
    if v.lower() in ('yes', 'true', 't', 'y', '1', 'on'):
        return True
    elif v.lower() in ('no', 'false', 'f', 'n', '0', 'off'):
        return False
    else:
        raise ValueError(f'Boolean value expected: {v}')

def datestamp() -> str:
    """ return current date as string (example: 24-Dec-2024) """
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
    return f"{now.day:02}-{mmm}-{now.year} "

def timestamp() -> str:
    """ return current date + time as string (example: 24-Dec-2024 19:34:01) """
    now = datetime.datetime.now()
    return f"{datestamp()} {now.hour:02}:{now.minute:02}:{now.second:02}"

def trace(text) -> None:
    """ print text and return true 
        function can be used to trace evaluations of Boolean expressions """
    print(text)
    return True

def wrap(text: str, width: int = 72, margin_width: int = 0, separator: str = "") -> str:
    """ reformat text to fit within width characters line width """
    if separator == "":
        words = text.split()
        sep = " "        
    else:
        words = text.split(separator)
        sep = separator
    sep_len = len(sep)
    lines = []
    current_line = ""
    margin = ' ' * margin_width
    for word in words:
        if len(current_line) + len(word) + (sep_len if current_line else 0) > width:
            lines.append(current_line)
            current_line = margin + word
        else:
            current_line += (sep if current_line else "") + word
    if current_line:
        lines.append(current_line)
    if separator.strip() == "":
        ret = "\n".join(lines)
    else:
        ret = f"{separator}\n".join(lines)        
    if text.endswith("\n") and not ret.endswith("\n"):
        """ final \n is removed in split() as whitespace """
        return f"{ret}\n"
    else:
        return ret


class TimeReporter:
    """
    Class to measure runtime as wall-time
    Create TimeReporter object at the beginning of the code to be measured.
    Once the code scope is left behind, TimeReport will be deconstructed.
    Before its death, it will report the elapsed time.
    The object disposal can be made explicit by using "del" as object delete.
    """
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

"""  end of file util.py  """
